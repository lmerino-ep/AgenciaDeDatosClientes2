using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Dashboard.Utils.Middlewares.CdnHeaders
{
    /// <summary>
    /// Middleware para manejar cabeceras de respuesta optimizadas para CDN (Transparent, CloudFront)
    /// </summary>
    public class CdnHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CdnHeadersPolicy _policy;

        public CdnHeadersMiddleware(RequestDelegate next, CdnHeadersPolicy policy)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Añadir cabeceras de respuesta optimizadas para CDN ANTES de procesar la request
            AddCdnHeaders(context);

            // Procesar la request
            await _next(context);
        }

        private void AddCdnHeaders(HttpContext context)
        {
            var response = context.Response;
            var request = context.Request;
            var headers = response.Headers;

            // Verificar si la respuesta ya ha comenzado (no se pueden modificar cabeceras)
            if (response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    request.Path, request.Method, response.StatusCode);
                return;
            }

            // Verificar si la request viene de alguna CDN
            bool isTransparentCdnRequest = context.Items.ContainsKey("IsTransparentCdnRequest") && 
                                         context.Items["IsTransparentCdnRequest"] is bool transparentCdn && transparentCdn;
            bool isCloudFrontRequest = context.Items.ContainsKey("IsCloudFrontRequest") && 
                                     context.Items["IsCloudFrontRequest"] is bool cloudFront && cloudFront;
            bool isCdnRequest = isTransparentCdnRequest || isCloudFrontRequest;

            // Cabeceras de caché optimizadas para CDN
            if (_policy.HabilitarCabecerasCache)
            {
                AddCacheHeaders(context, headers, isCdnRequest);
            }

            // Cabeceras de seguridad específicas para CDN
            if (_policy.HabilitarCabecerasSeguridad)
            {
                AddSecurityHeaders(context, headers, isCdnRequest);
            }

            // Cabeceras de rendimiento
            if (_policy.HabilitarCabecerasRendimiento)
            {
                AddPerformanceHeaders(context, headers);
            }

            // Cabeceras de monitoreo
            if (_policy.HabilitarCabecerasMonitoreo)
            {
                AddMonitoringHeaders(context, headers, isTransparentCdnRequest, isCloudFrontRequest);
            }

            // Cabeceras de CORS optimizadas para CDN
            if (_policy.HabilitarCabecerasCors)
            {
                AddCorsHeaders(context, headers, isCdnRequest);
            }
        }

        private void AddCacheHeaders(HttpContext context, IHeaderDictionary headers, bool isCdnRequest)
        {
            var request = context.Request;
            var response = context.Response;

            // Verificar si la respuesta ya ha comenzado
            if (response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras de caché CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    request.Path, request.Method, response.StatusCode);
                return;
            }

            // Determinar la estrategia de caché basada en la ruta y el tipo de contenido
            var cacheStrategy = GetCacheStrategy(request.Path, response.ContentType);

            if (cacheStrategy != null)
            {
                var cacheControl = new CacheControlHeaderValue
                {
                    Public = cacheStrategy.IsPublic,
                    Private = cacheStrategy.IsPrivate,
                    MaxAge = cacheStrategy.MaxAge,
                    SharedMaxAge = cacheStrategy.SharedMaxAge,
                    MustRevalidate = cacheStrategy.MustRevalidate,
                    NoCache = cacheStrategy.NoCache,
                    NoStore = cacheStrategy.NoStore
                };

                // Añadir Vary headers para CDN
                response.GetTypedHeaders().CacheControl = cacheControl;

                if (isCdnRequest && cacheStrategy.VaryHeaders?.Any() == true)
                {
                    headers[HeaderNames.Vary] = string.Join(", ", cacheStrategy.VaryHeaders);
                }

                // ETag para validación de caché
                if (cacheStrategy.IncludeETag && !headers.ContainsKey(HeaderNames.ETag))
                {
                    var etag = GenerateETag(context);
                    headers[HeaderNames.ETag] = $"\"{etag}\"";
                }

                // Last-Modified para validación de caché
                if (cacheStrategy.IncludeLastModified && !headers.ContainsKey(HeaderNames.LastModified))
                {
                    headers[HeaderNames.LastModified] = DateTime.UtcNow.ToString("R");
                }
            }
        }

        private void AddSecurityHeaders(HttpContext context, IHeaderDictionary headers, bool isCdnRequest)
        {
            // Verificar si la respuesta ya ha comenzado
            if (context.Response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras de seguridad CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    context.Request.Path, context.Request.Method, context.Response.StatusCode);
                return;
            }

            // Cabeceras de seguridad específicas para CDN
            if (isCdnRequest)
            {
                // Asegurar que las cabeceras de seguridad lleguen al cliente final
                if (!headers.ContainsKey("X-Content-Type-Options"))
                {
                    headers["X-Content-Type-Options"] = "nosniff";
                }

                if (!headers.ContainsKey("X-Frame-Options"))
                {
                    headers["X-Frame-Options"] = "SAMEORIGIN";
                }

                if (!headers.ContainsKey("X-XSS-Protection"))
                {
                    headers["X-XSS-Protection"] = "1; mode=block";
                }

                // Cabeceras adicionales de seguridad
                if (!headers.ContainsKey("Referrer-Policy"))
                {
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                }

                if (!headers.ContainsKey("Permissions-Policy"))
                {
                    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                }

                // Cabeceras específicas para Transparent CDN
                if (_policy.IncluirCabecerasTransparent && context.Items.ContainsKey("IsTransparentCdnRequest") && 
                    context.Items["IsTransparentCdnRequest"] is bool transparentCdn && transparentCdn)
                {
                    headers["X-CDN-Provider"] = "Transparent";
                    headers["X-CDN-Cache-Status"] = context.Items.ContainsKey("TransparentCacheStatus") 
                        ? context.Items["TransparentCacheStatus"]?.ToString() ?? "MISS"
                        : "MISS";
                    
                    // Cabeceras específicas de Transparent CDN
                    if (context.Items.ContainsKey("TransparentRequestID"))
                    {
                        headers["X-Request-ID"] = context.Items["TransparentRequestID"]?.ToString() ?? "";
                    }
                }
                // Cabeceras específicas para CloudFront
                else if (_policy.IncluirCabecerasCloudFront && context.Items.ContainsKey("IsCloudFrontRequest") && 
                         context.Items["IsCloudFrontRequest"] is bool cloudFront && cloudFront)
                {
                    headers["X-CDN-Provider"] = "CloudFront";
                    headers["X-CDN-Cache-Status"] = context.Items.ContainsKey("CloudFrontCacheStatus") 
                        ? context.Items["CloudFrontCacheStatus"]?.ToString() ?? "MISS"
                        : "MISS";
                }
            }
        }

        private void AddPerformanceHeaders(HttpContext context, IHeaderDictionary headers)
        {
            // Verificar si la respuesta ya ha comenzado
            if (context.Response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras de rendimiento CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    context.Request.Path, context.Request.Method, context.Response.StatusCode);
                return;
            }

            // Cabeceras de rendimiento
            if (!headers.ContainsKey("X-Response-Time"))
            {
                var startTime = context.Items["RequestStartTime"] as DateTime?;
                var responseTime = startTime.HasValue 
                    ? DateTime.UtcNow - startTime.Value 
                    : TimeSpan.Zero;
                headers["X-Response-Time"] = $"{responseTime.TotalMilliseconds:F2}ms";
            }

            // Cabeceras de compresión
            if (context.Response.Headers.ContainsKey("Content-Encoding"))
            {
                headers["X-Compression"] = context.Response.Headers["Content-Encoding"];
            }

            // Cabeceras de optimización
            if (!headers.ContainsKey("X-DNS-Prefetch-Control"))
            {
                headers["X-DNS-Prefetch-Control"] = "on";
            }

            if (!headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
            {
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
            }
        }

        private void AddMonitoringHeaders(HttpContext context, IHeaderDictionary headers, bool isTransparentCdnRequest, bool isCloudFrontRequest)
        {
            // Verificar si la respuesta ya ha comenzado
            if (context.Response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras de monitoreo CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    context.Request.Path, context.Request.Method, context.Response.StatusCode);
                return;
            }

            // Cabeceras de monitoreo
            headers["X-Served-By"] = "EpData-ECS";
            headers["X-Environment"] = context.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName ?? "Unknown";
            headers["X-Build-Date"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (isTransparentCdnRequest)
            {
                headers["X-CDN-Request"] = "true";
                headers["X-CDN-Provider"] = "Transparent";
                
                // Información adicional de Transparent CDN si está disponible
                var transparentInfo = new[]
                {
                    ("TransparentViewerCountry", "X-Viewer-Country"),
                    ("TransparentViewerCity", "X-Viewer-City"),
                    ("TransparentViewerContinent", "X-Viewer-Continent"),
                    ("TransparentViewerRegion", "X-Viewer-Region"),
                    ("TransparentViewerISP", "X-Viewer-ISP"),
                    ("TransparentViewerASN", "X-Viewer-ASN"),
                    ("TransparentViewerTimezone", "X-Viewer-Timezone"),
                    ("TransparentDeviceType", "X-Device-Type"),
                    ("TransparentBrowser", "X-Browser"),
                    ("TransparentOS", "X-OS"),
                    ("TransparentRequestID", "X-Request-ID")
                };

                foreach (var (contextKey, headerName) in transparentInfo)
                {
                    if (context.Items.ContainsKey(contextKey))
                    {
                        headers[headerName] = context.Items[contextKey]?.ToString() ?? "";
                    }
                }
            }
            else if (isCloudFrontRequest)
            {
                headers["X-CDN-Request"] = "true";
                headers["X-CDN-Provider"] = "CloudFront";
                
                // Información adicional de CloudFront si está disponible
                if (context.Items.ContainsKey("CloudFrontViewerCountry"))
                {
                    headers["X-Viewer-Country"] = context.Items["CloudFrontViewerCountry"]?.ToString() ?? "";
                }
            }
        }

        private void AddCorsHeaders(HttpContext context, IHeaderDictionary headers, bool isCdnRequest)
        {
            // Verificar si la respuesta ya ha comenzado
            if (context.Response.HasStarted)
            {
                var logger = context.RequestServices.GetService<ILogger<CdnHeadersMiddleware>>();
                logger?.LogWarning(
                    "No se pudieron añadir cabeceras CORS CDN porque la respuesta ya había comenzado. " +
                    "Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                    context.Request.Path, context.Request.Method, context.Response.StatusCode);
                return;
            }

            // Cabeceras de CORS optimizadas para CDN
            if (isCdnRequest)
            {
                // Asegurar que las cabeceras CORS lleguen al cliente final
                if (!headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    headers["Access-Control-Allow-Origin"] = "*";
                }

                if (!headers.ContainsKey("Access-Control-Allow-Methods"))
                {
                    headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                }

                if (!headers.ContainsKey("Access-Control-Allow-Headers"))
                {
                    headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
                }

                if (!headers.ContainsKey("Access-Control-Max-Age"))
                {
                    headers["Access-Control-Max-Age"] = "86400";
                }
            }
        }

        private CacheStrategy? GetCacheStrategy(PathString path, string? contentType)
        {
            var pathString = path.Value?.ToLowerInvariant() ?? "";

            // Estrategias de caché por tipo de contenido
            if (contentType?.StartsWith("text/html") == true)
            {
                // Páginas HTML - caché corto para contenido dinámico
                return new CacheStrategy
                {
                    IsPublic = true,
                    MaxAge = TimeSpan.FromMinutes(_policy.CacheDinamicoMinutos),
                    SharedMaxAge = TimeSpan.FromMinutes(_policy.CacheDinamicoMinutos),
                    MustRevalidate = true,
                    VaryHeaders = new[] { "Accept-Encoding", "Accept-Language" }
                };
            }

            if (contentType?.StartsWith("application/json") == true)
            {
                // APIs JSON - sin caché por defecto
                return new CacheStrategy
                {
                    NoCache = true,
                    NoStore = true
                };
            }

            if (contentType?.StartsWith("text/css") == true || 
                contentType?.StartsWith("image/") == true)
            {
                // Recursos estáticos (CSS e imágenes) - caché largo
                return new CacheStrategy
                {
                    IsPublic = true,
                    MaxAge = TimeSpan.FromDays(_policy.CacheEstaticoDias),
                    SharedMaxAge = TimeSpan.FromDays(_policy.CacheEstaticoDias),
                    IncludeETag = true,
                    IncludeLastModified = true,
                    VaryHeaders = new[] { "Accept-Encoding" }
                };
            }

            if (contentType?.StartsWith("application/javascript") == true)
            {
                // JavaScript - verificar si es crítico
                if (pathString.StartsWith("/js/critical/"))
                {
                    // JavaScript crítico - sin caché para evitar 304
                    return new CacheStrategy
                    {
                        NoCache = true,
                        NoStore = true
                    };
                }
                else
                {
                    // JavaScript normal - caché largo
                    return new CacheStrategy
                    {
                        IsPublic = true,
                        MaxAge = TimeSpan.FromDays(_policy.CacheEstaticoDias),
                        SharedMaxAge = TimeSpan.FromDays(_policy.CacheEstaticoDias),
                        IncludeETag = true,
                        IncludeLastModified = true,
                        VaryHeaders = new[] { "Accept-Encoding" }
                    };
                }
            }

            // Rutas específicas
            if (pathString.StartsWith("/api/"))
            {
                return new CacheStrategy
                {
                    NoCache = true,
                    NoStore = true
                };
            }

            if (pathString.StartsWith("/admin/") || pathString.StartsWith("/mi-cuenta/"))
            {
                return new CacheStrategy
                {
                    IsPrivate = true,
                    MaxAge = TimeSpan.FromMinutes(0),
                    NoCache = true
                };
            }

            // Por defecto, caché moderado
            return new CacheStrategy
            {
                IsPublic = true,
                MaxAge = TimeSpan.FromMinutes(_policy.CacheModeradoMinutos),
                SharedMaxAge = TimeSpan.FromMinutes(_policy.CacheModeradoMinutos),
                MustRevalidate = true,
                VaryHeaders = new[] { "Accept-Encoding" }
            };
        }

        private string GenerateETag(HttpContext context)
        {
            try
            {
                // Generar ETag basado en la URL y timestamp
                var url = context.Request.Path + context.Request.QueryString;
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
                var content = url + timestamp;
                
                // Usar SHA256 para generar un hash más seguro y consistente
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
                var etag = Convert.ToBase64String(hashBytes);
                
                // Limpiar caracteres problemáticos para ETags
                return etag.Replace("+", "-").Replace("/", "_").Replace("=", "").Substring(0, Math.Min(etag.Length, 32));
            }
            catch
            {
                // Fallback en caso de error
                return Guid.NewGuid().ToString("N");
            }
        }
    }

    /// <summary>
    /// Estrategia de caché para diferentes tipos de contenido
    /// </summary>
    public class CacheStrategy
    {
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public TimeSpan? SharedMaxAge { get; set; }
        public bool MustRevalidate { get; set; }
        public bool NoCache { get; set; }
        public bool NoStore { get; set; }
        public bool IncludeETag { get; set; }
        public bool IncludeLastModified { get; set; }
        public string[]? VaryHeaders { get; set; }
    }
} 