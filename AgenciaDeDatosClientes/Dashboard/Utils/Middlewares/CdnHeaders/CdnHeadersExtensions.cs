namespace Utils.Middlewares.CdnHeaders
{
    /// <summary>
    /// Extensiones para registrar el middleware de cabeceras de CDN
    /// </summary>
    public static class CdnHeadersExtensions
    {
        /// <summary>
        /// Registra el middleware de cabeceras de CDN con la configuración especificada
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeCdn(this IApplicationBuilder app, CdnHeadersBuilder builder)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return app.UseMiddleware<CdnHeadersMiddleware>(builder.Build());
        }

        /// <summary>
        /// Registra el middleware de cabeceras de CDN con configuración por defecto para Transparent CDN
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeCdn(this IApplicationBuilder app)
        {
            return app.UsarCabecerasDeCdn(new CdnHeadersBuilder().ParaTransparentCdn());
        }

        /// <summary>
        /// Registra el middleware de cabeceras de CDN optimizado para Transparent CDN
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeTransparentCdn(this IApplicationBuilder app)
        {
            return app.UsarCabecerasDeCdn(new CdnHeadersBuilder().ParaTransparentCdn());
        }

        /// <summary>
        /// Registra el middleware de cabeceras de CDN optimizado para CloudFront CDN (para futuro uso)
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeCloudFrontCdn(this IApplicationBuilder app)
        {
            return app.UsarCabecerasDeCdn(new CdnHeadersBuilder().ParaCloudFrontCdn());
        }
    }
} 
