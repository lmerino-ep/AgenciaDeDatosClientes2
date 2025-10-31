namespace Dashboard.Utils.Middlewares.ProxyHeaders
{
    /// <summary>
    /// Política de configuración para el middleware de cabeceras de proxy
    /// </summary>
    public class ProxyHeadersPolicy
    {
        /// <summary>
        /// Habilita el procesamiento de cabeceras ForwardedHeaders (X-Forwarded-For, X-Forwarded-Proto, etc.)
        /// </summary>
        public bool HabilitarForwardedHeaders { get; set; } = true;

        /// <summary>
        /// Habilita el logging de cabeceras para debugging (solo en desarrollo)
        /// </summary>
        public bool LogHeaders { get; set; } = false;

        /// <summary>
        /// Lista de IPs confiables para ForwardedHeaders (ALB, VPC)
        /// </summary>
        public string[] TrustedProxies { get; set; } = new[]
        {
            "10.0.0.0/8",      // VPC privada
            "172.16.0.0/12",   // VPC privada
            "192.168.0.0/16",  // VPC privada
            "169.254.169.254", // AWS metadata service
            "127.0.0.1",       // Localhost
            "::1"              // IPv6 localhost
        };

        /// <summary>
        /// Habilita la detección automática de Transparent CDN
        /// </summary>
        public bool DetectarTransparentCdn { get; set; } = true;

        /// <summary>
        /// Habilita la detección automática de ALB
        /// </summary>
        public bool DetectarALB { get; set; } = true;

        /// <summary>
        /// Habilita la validación de IPs confiables
        /// </summary>
        public bool ValidarIPsConfiables { get; set; } = false;

        /// <summary>
        /// Habilita el logging de IPs no confiables
        /// </summary>
        public bool LogIPsNoConfiables { get; set; } = false;

        /// <summary>
        /// Habilita la detección de geolocalización
        /// </summary>
        public bool DetectarGeolocalizacion { get; set; } = false;
    }
} 