using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace Utils.Middlewares.ProxyHeaders
{
    /// <summary>
    /// Middleware para manejar cabeceras de proxy de AWS ALB y Transparent CDN
    /// </summary>
    public class ProxyHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ProxyHeadersPolicy _policy;

        public ProxyHeadersMiddleware(RequestDelegate next, ProxyHeadersPolicy policy)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Procesar cabeceras de proxy antes de continuar
            ProcessProxyHeaders(context);

            await _next(context);
        }

        private void ProcessProxyHeaders(HttpContext context)
        {
            var request = context.Request;
            var headers = request.Headers;

            // Configurar ForwardedHeaders para AWS ALB con validación de IPs confiables
            if (_policy.HabilitarForwardedHeaders)
            {
                ProcessForwardedHeaders(context, headers);
            }

            // Detectar si la request viene de Transparent CDN
            if (_policy.DetectarTransparentCdn && DetectarTransparentCdn(headers))
            {
                ProcessTransparentCdnHeaders(context, headers);
            }

            // Detectar si la request viene de AWS ALB
            if (_policy.DetectarALB && DetectarALBRequest(headers))
            {
                context.Items["IsALBRequest"] = true;
            }

            // Log de cabeceras para debugging (solo en desarrollo)
            if (_policy.LogHeaders && context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                var logger = context.RequestServices.GetService<ILogger<ProxyHeadersMiddleware>>();
                if (logger != null)
                {
                    logger.LogInformation("Proxy Headers: {Headers}", 
                        string.Join(", ", headers.Select(h => $"{h.Key}={h.Value}")));
                }
            }
        }

        private void ProcessForwardedHeaders(HttpContext context, IHeaderDictionary headers)
        {
            // Procesar X-Forwarded-For con validación de IPs confiables
            if (headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = headers["X-Forwarded-For"].First();
                var clientIP = GetClientIPFromForwardedFor(forwardedFor);
                
                if (_policy.ValidarIPsConfiables && IsIPConfiable(context.Connection.RemoteIpAddress))
                {
                    if (IPAddress.TryParse(clientIP, out var ipAddress))
                    {
                        context.Connection.RemoteIpAddress = ipAddress;
                    }
                }
                else if (_policy.LogIPsNoConfiables)
                {
                    var logger = context.RequestServices.GetService<ILogger<ProxyHeadersMiddleware>>();
                    // No loguear IPs por seguridad - solo registrar que hubo un intento no confiable
                    logger?.LogWarning("IP no confiable detectada en X-Forwarded-For");
                }
            }

            // Procesar X-Forwarded-Proto
            if (headers.ContainsKey("X-Forwarded-Proto"))
            {
                var proto = headers["X-Forwarded-Proto"].First();
                if (IsProtocoloValido(proto))
                {
                    context.Request.Scheme = proto;
                }
            }

            // Procesar X-Forwarded-Host
            if (headers.ContainsKey("X-Forwarded-Host"))
            {
                var host = headers["X-Forwarded-Host"].First();
                if (IsHostValido(host))
                {
                    context.Request.Host = new HostString(host);
                }
            }

            // Procesar X-Forwarded-Port
            if (headers.ContainsKey("X-Forwarded-Port"))
            {
                if (int.TryParse(headers["X-Forwarded-Port"].First(), out int port) && port > 0 && port <= 65535)
                {
                    context.Request.Host = new HostString(context.Request.Host.Host, port);
                }
            }
        }

        private void ProcessTransparentCdnHeaders(HttpContext context, IHeaderDictionary headers)
        {
            context.Items["IsTransparentCdnRequest"] = true;
            context.Items["CdnProvider"] = "Transparent";
            
            // Cabeceras específicas de Transparent CDN
            var transparentHeaders = new[]
            {
                ("X-Transparent-Country", "TransparentViewerCountry"),
                ("X-Transparent-City", "TransparentViewerCity"),
                ("X-Transparent-IP", "TransparentViewerIP"),
                ("X-Transparent-User-Agent", "TransparentUserAgent"),
                ("X-Transparent-Cache-Status", "TransparentCacheStatus"),
                ("X-Transparent-Request-ID", "TransparentRequestID"),
                ("X-Transparent-Continent", "TransparentViewerContinent"),
                ("X-Transparent-Region", "TransparentViewerRegion"),
                ("X-Transparent-ISP", "TransparentViewerISP"),
                ("X-Transparent-ASN", "TransparentViewerASN"),
                ("X-Transparent-Timezone", "TransparentViewerTimezone"),
                ("X-Transparent-Device-Type", "TransparentDeviceType"),
                ("X-Transparent-Browser", "TransparentBrowser"),
                ("X-Transparent-OS", "TransparentOS")
            };

            foreach (var (headerName, contextKey) in transparentHeaders)
            {
                if (headers.ContainsKey(headerName))
                {
                    context.Items[contextKey] = headers[headerName].First();
                }
            }

            // Detectar geolocalización si está habilitado
            if (_policy.DetectarGeolocalizacion)
            {
                DetectGeolocation(context, headers);
            }
        }

        private string GetClientIPFromForwardedFor(string forwardedFor)
        {
            // Extraer la primera IP de la cadena X-Forwarded-For
            return forwardedFor.Split(',')[0].Trim();
        }

        private bool IsIPConfiable(IPAddress? remoteIP)
        {
            if (remoteIP == null) return false;

            foreach (var trustedProxy in _policy.TrustedProxies)
            {
                if (IsIPInRange(remoteIP, trustedProxy))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsIPInRange(IPAddress ip, string cidr)
        {
            try
            {
                if (cidr.Contains("/"))
                {
                    var parts = cidr.Split('/');
                    var networkIP = IPAddress.Parse(parts[0]);
                    var prefixLength = int.Parse(parts[1]);
                    
                    var networkBytes = networkIP.GetAddressBytes();
                    var ipBytes = ip.GetAddressBytes();
                    
                    if (networkBytes.Length != ipBytes.Length) return false;
                    
                    var mask = ~((1 << (32 - prefixLength)) - 1);
                    var network = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0) & mask;
                    var ipAddress = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0) & mask;
                    
                    return network == ipAddress;
                }
                else
                {
                    return ip.Equals(IPAddress.Parse(cidr));
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsProtocoloValido(string protocolo)
        {
            return protocolo.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                   protocolo.Equals("https", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsHostValido(string host)
        {
            return !string.IsNullOrEmpty(host) && host.Length <= 255;
        }

        private void DetectGeolocation(HttpContext context, IHeaderDictionary headers)
        {
            // Crear objeto de geolocalización si hay información disponible
            var geoInfo = new Dictionary<string, string>();

            var geoHeaders = new[]
            {
                "X-Transparent-Country",
                "X-Transparent-City", 
                "X-Transparent-Continent",
                "X-Transparent-Region",
                "X-Transparent-Timezone"
            };

            foreach (var header in geoHeaders)
            {
                if (headers.ContainsKey(header))
                {
                    geoInfo[header] = headers[header].First();
                }
            }

            if (geoInfo.Any())
            {
                context.Items["GeolocationInfo"] = geoInfo;
            }
        }

        private bool DetectarTransparentCdn(IHeaderDictionary headers)
        {
            // Transparent CDN puede enviar estas cabeceras específicas
            return headers.ContainsKey("X-Transparent-Country") ||
                   headers.ContainsKey("X-Transparent-City") ||
                   headers.ContainsKey("X-Transparent-IP") ||
                   headers.ContainsKey("X-Transparent-Cache-Status") ||
                   headers.ContainsKey("X-Transparent-Request-ID") ||
                   headers.ContainsKey("X-Transparent-Continent") ||
                   headers.ContainsKey("X-CDN") && headers["X-CDN"].ToString().Contains("transparent", StringComparison.OrdinalIgnoreCase);
        }

        private bool DetectarALBRequest(IHeaderDictionary headers)
        {
            return headers.ContainsKey("X-Aws-Ec2-Instance-Id") || 
                   headers.ContainsKey("X-Aws-Ec2-Instance-Type") ||
                   headers.ContainsKey("X-Aws-Ec2-Availability-Zone");
        }
    }
} 
