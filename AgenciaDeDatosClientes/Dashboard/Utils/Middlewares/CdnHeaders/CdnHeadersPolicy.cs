namespace Dashboard.Utils.Middlewares.CdnHeaders
{
    /// <summary>
    /// Política de configuración para el middleware de cabeceras de CDN
    /// </summary>
    public class CdnHeadersPolicy
    {
        /// <summary>
        /// Habilita las cabeceras de caché optimizadas para CDN
        /// </summary>
        public bool HabilitarCabecerasCache { get; set; } = true;

        /// <summary>
        /// Habilita las cabeceras de seguridad específicas para CDN
        /// </summary>
        public bool HabilitarCabecerasSeguridad { get; set; } = true;

        /// <summary>
        /// Habilita las cabeceras de rendimiento
        /// </summary>
        public bool HabilitarCabecerasRendimiento { get; set; } = true;

        /// <summary>
        /// Habilita las cabeceras de monitoreo
        /// </summary>
        public bool HabilitarCabecerasMonitoreo { get; set; } = true;

        /// <summary>
        /// Habilita las cabeceras de CORS optimizadas para CDN
        /// </summary>
        public bool HabilitarCabecerasCors { get; set; } = true;

        /// <summary>
        /// Incluye cabeceras específicas de Transparent CDN
        /// </summary>
        public bool IncluirCabecerasTransparent { get; set; } = true;

        /// <summary>
        /// Incluye cabeceras específicas de CloudFront (para futuro uso)
        /// </summary>
        public bool IncluirCabecerasCloudFront { get; set; } = false;

        /// <summary>
        /// Habilita la generación automática de ETags
        /// </summary>
        public bool GenerarETags { get; set; } = true;

        /// <summary>
        /// Habilita la generación automática de Last-Modified
        /// </summary>
        public bool GenerarLastModified { get; set; } = true;

        /// <summary>
        /// Configuración de caché por defecto para contenido dinámico (en minutos)
        /// </summary>
        public int CacheDinamicoMinutos { get; set; } = 5;

        /// <summary>
        /// Configuración de caché por defecto para contenido estático (en días)
        /// </summary>
        public int CacheEstaticoDias { get; set; } = 30;

        /// <summary>
        /// Configuración de caché por defecto para contenido moderado (en minutos)
        /// </summary>
        public int CacheModeradoMinutos { get; set; } = 15;

        /// <summary>
        /// Habilita las cabeceras de geolocalización
        /// </summary>
        public bool IncluirGeolocalizacion { get; set; } = true;
    }
} 