namespace Utils.Middlewares.ProxyHeaders
{
    /// <summary>
    /// Builder para configurar el middleware de cabeceras de proxy
    /// </summary>
    public class ProxyHeadersBuilder
    {
        private readonly ProxyHeadersPolicy _policy = new ProxyHeadersPolicy();

        /// <summary>
        /// Habilita el procesamiento de cabeceras ForwardedHeaders
        /// </summary>
        public ProxyHeadersBuilder HabilitarForwardedHeaders(bool habilitar = true)
        {
            _policy.HabilitarForwardedHeaders = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita el logging de cabeceras para debugging
        /// </summary>
        public ProxyHeadersBuilder ConLogging(bool habilitar = true)
        {
            _policy.LogHeaders = habilitar;
            return this;
        }

        /// <summary>
        /// Configura las IPs confiables para ForwardedHeaders
        /// </summary>
        public ProxyHeadersBuilder ConProxiesConfiables(params string[] proxies)
        {
            if (proxies != null && proxies.Length > 0)
            {
                _policy.TrustedProxies = proxies;
            }
            return this;
        }

        /// <summary>
        /// Habilita la detección de ALB
        /// </summary>
        public ProxyHeadersBuilder DetectarALB(bool habilitar = true)
        {
            _policy.DetectarALB = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita la detección de Transparent CDN
        /// </summary>
        public ProxyHeadersBuilder DetectarTransparentCdn(bool habilitar = true)
        {
            _policy.DetectarTransparentCdn = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita la validación de IPs confiables
        /// </summary>
        public ProxyHeadersBuilder ValidarIPsConfiables(bool habilitar = true)
        {
            _policy.ValidarIPsConfiables = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita el logging de IPs no confiables
        /// </summary>
        public ProxyHeadersBuilder LogIPsNoConfiables(bool habilitar = true)
        {
            _policy.LogIPsNoConfiables = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita la detección de geolocalización
        /// </summary>
        public ProxyHeadersBuilder DetectarGeolocalizacion(bool habilitar = true)
        {
            _policy.DetectarGeolocalizacion = habilitar;
            return this;
        }

        /// <summary>
        /// Configuración específica para Transparent CDN
        /// </summary>
        public ProxyHeadersBuilder ParaTransparentCdn()
        {
            HabilitarForwardedHeaders(true);
            DetectarALB(true);
            DetectarTransparentCdn(true);
            ValidarIPsConfiables(false);
            LogIPsNoConfiables(false);
            DetectarGeolocalizacion(false);
            ConProxiesConfiables(
                "10.0.0.0/8",      // VPC privada
                "172.16.0.0/12",   // VPC privada
                "192.168.0.0/16",  // VPC privada
                "169.254.169.254", // AWS metadata service
                "127.0.0.1",       // Localhost
                "::1"              // IPv6 localhost
            );
            return this;
        }

        /// <summary>
        /// Configuración específica para AWS ECS con CDN Transparent + ALB + WAF
        /// Arquitectura: Internet → Transparent CDN → ALB + WAF → ECS Cluster → Kestrel
        /// </summary>
        public ProxyHeadersBuilder ParaAwsEcs()
        {
            //************************** ¡¡¡Importante!!! **************************//
            //Aquí habilitamos el código necesario para la comunicación entre las distintas capas de nuestra arquitectura (CDN + ALB + ECS + Kestrel)
            //************************** ¡¡¡Importante!!! **************************//

            HabilitarForwardedHeaders(true); //Procesa X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host
            DetectarALB(true); //Identifica si la request viene del ALB o del CDN y Añade flags en HttpContext.Items para que otros middlewares sepan el origen
            DetectarTransparentCdn(true); //Identifica si la request viene del ALB o del CDN y Añade flags en HttpContext.Items para que otros middlewares sepan el origen
            ValidarIPsConfiables(false); //Verifica que las IPs sean de tus servicios confiables
            LogIPsNoConfiables(false); //Registra logs si llegan requests de IPs desconocidas
            DetectarGeolocalizacion(false); //Extrae datos de geolocalización de las cabeceras de Transparent CDN y Los hace disponibles para personalización y analytics
            ConLogging(false);
            
            // Configurar IPs confiables para la arquitectura completa
            ConProxiesConfiables(
                // IPs del ALB (AWS Application Load Balancer)
                "10.0.0.0/8",      // VPC privada donde está el ALB
                "172.16.0.0/12",   // VPC privada
                "192.168.0.0/16",  // VPC privada
                
                // IPs de Transparent CDN (rango de IPs de Transparent)
                "185.199.108.0/22", // Transparent CDN IPs
                "185.199.109.0/22", // Transparent CDN IPs
                "185.199.110.0/22", // Transparent CDN IPs
                "185.199.111.0/22", // Transparent CDN IPs
                
                // IPs de AWS WAF y servicios internos
                "169.254.169.254", // AWS metadata service
                "127.0.0.1",       // Localhost
                "::1"              // IPv6 localhost
            );
            /*
                1. Cliente hace request
                   ↓
                2. Transparent CDN añade cabeceras de geolocalización
                   ↓
                3. ALB añade X-Forwarded-For, X-Forwarded-Proto
                   ↓
                4. ProxyHeadersMiddleware procesa TODO:
                   - ✅ Detecta que viene de CDN + ALB
                   - ✅ Valida IPs confiables
                   - ✅ Extrae geolocalización
                   - ✅ Añade flags en HttpContext.Items
                   ↓
                5. CdnHeadersMiddleware usa esa información:
                   - ✅ Añade cabeceras optimizadas para CDN
                   - ✅ Incluye datos de geolocalización en respuesta
                   - ✅ Configura caché inteligente
            */
            return this;
        }

        /// <summary>
        /// Construye la política de configuración
        /// </summary>
        public ProxyHeadersPolicy Build()
        {
            return _policy;
        }
    }
} 
