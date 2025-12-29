namespace Utils.Middlewares.CdnHeaders
{
    /// <summary>
    /// Builder para configurar el middleware de cabeceras de CDN
    /// </summary>
    public class CdnHeadersBuilder
    {
        private readonly CdnHeadersPolicy _policy = new CdnHeadersPolicy();

        /// <summary>
        /// Habilita las cabeceras de caché optimizadas para CDN
        /// </summary>
        public CdnHeadersBuilder HabilitarCabecerasCache(bool habilitar = true)
        {
            _policy.HabilitarCabecerasCache = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita las cabeceras de seguridad específicas para CDN
        /// </summary>
        public CdnHeadersBuilder HabilitarCabecerasSeguridad(bool habilitar = true)
        {
            _policy.HabilitarCabecerasSeguridad = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita las cabeceras de rendimiento
        /// </summary>
        public CdnHeadersBuilder HabilitarCabecerasRendimiento(bool habilitar = true)
        {
            _policy.HabilitarCabecerasRendimiento = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita las cabeceras de monitoreo
        /// </summary>
        public CdnHeadersBuilder HabilitarCabecerasMonitoreo(bool habilitar = true)
        {
            _policy.HabilitarCabecerasMonitoreo = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita las cabeceras de CORS optimizadas para CDN
        /// </summary>
        public CdnHeadersBuilder HabilitarCabecerasCors(bool habilitar = true)
        {
            _policy.HabilitarCabecerasCors = habilitar;
            return this;
        }

        /// <summary>
        /// Incluye cabeceras específicas de Transparent CDN
        /// </summary>
        public CdnHeadersBuilder IncluirCabecerasTransparent(bool incluir = true)
        {
            _policy.IncluirCabecerasTransparent = incluir;
            return this;
        }

        /// <summary>
        /// Incluye cabeceras específicas de CloudFront (para futuro uso)
        /// </summary>
        public CdnHeadersBuilder IncluirCabecerasCloudFront(bool incluir = false)
        {
            _policy.IncluirCabecerasCloudFront = incluir;
            return this;
        }

        /// <summary>
        /// Habilita la generación automática de ETags
        /// </summary>
        public CdnHeadersBuilder GenerarETags(bool habilitar = true)
        {
            _policy.GenerarETags = habilitar;
            return this;
        }

        /// <summary>
        /// Habilita la generación automática de Last-Modified
        /// </summary>
        public CdnHeadersBuilder GenerarLastModified(bool habilitar = true)
        {
            _policy.GenerarLastModified = habilitar;
            return this;
        }

        /// <summary>
        /// Configura el tiempo de caché para contenido dinámico
        /// </summary>
        public CdnHeadersBuilder ConCacheDinamico(int minutos)
        {
            _policy.CacheDinamicoMinutos = minutos;
            return this;
        }

        /// <summary>
        /// Configura el tiempo de caché para contenido estático
        /// </summary>
        public CdnHeadersBuilder ConCacheEstatico(int dias)
        {
            _policy.CacheEstaticoDias = dias;
            return this;
        }

        /// <summary>
        /// Configura el tiempo de caché para contenido moderado
        /// </summary>
        public CdnHeadersBuilder ConCacheModerado(int minutos)
        {
            _policy.CacheModeradoMinutos = minutos;
            return this;
        }

        /// <summary>
        /// Habilita las cabeceras de geolocalización
        /// </summary>
        public CdnHeadersBuilder ConGeolocalizacion(bool habilitar = true)
        {
            _policy.IncluirGeolocalizacion = habilitar;
            return this;
        }

        /// <summary>
        /// Configuración de caché inteligente con tiempos personalizados
        /// </summary>
        public CdnHeadersBuilder ConCacheInteligente(int cacheDinamicoMinutos, int cacheEstaticoDias, int cacheModeradoMinutos)
        {
            _policy.CacheDinamicoMinutos = cacheDinamicoMinutos;
            _policy.CacheEstaticoDias = cacheEstaticoDias;
            _policy.CacheModeradoMinutos = cacheModeradoMinutos;
            return this;
        }

        /// <summary>
        /// Configuración optimizada para Transparent CDN
        /// </summary>
        public CdnHeadersBuilder ParaTransparentCdn()
        {
            HabilitarCabecerasCache(true);
            HabilitarCabecerasSeguridad(true);
            HabilitarCabecerasRendimiento(true);
            HabilitarCabecerasMonitoreo(true);
            HabilitarCabecerasCors(true);
            IncluirCabecerasTransparent(true);
            IncluirCabecerasCloudFront(false);
            GenerarETags(true);
            GenerarLastModified(true);
            ConCacheDinamico(5); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConCacheEstatico(30); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConCacheModerado(15); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConGeolocalizacion(true);
            return this;
        }

        /// <summary>
        /// Configuración optimizada para CloudFront CDN (para futuro uso)
        /// </summary>
        public CdnHeadersBuilder ParaCloudFrontCdn()
        {
            HabilitarCabecerasCache(true);
            HabilitarCabecerasSeguridad(true);
            HabilitarCabecerasRendimiento(true);
            HabilitarCabecerasMonitoreo(true);
            HabilitarCabecerasCors(true);
            IncluirCabecerasTransparent(false);
            IncluirCabecerasCloudFront(true);
            GenerarETags(true);
            GenerarLastModified(true);
            ConCacheDinamico(5); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConCacheEstatico(30); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConCacheModerado(15); //Esto es solo una salvaguarda, pues aplica solo si no se ha definido política de caché a la response.
            ConGeolocalizacion(true);
            return this;
        }

        /// <summary>
        /// Construye la política de configuración
        /// </summary>
        public CdnHeadersPolicy Build()
        {
            return _policy;
        }
    }
} 
