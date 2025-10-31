namespace Dashboard.Utils.Middlewares.ProxyHeaders
{
    /// <summary>
    /// Extensiones para registrar el middleware de cabeceras de proxy
    /// </summary>
    public static class ProxyHeadersExtensions
    {
        /// <summary>
        /// Registra el middleware de cabeceras de proxy con la configuración especificada
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeProxy(this IApplicationBuilder app, ProxyHeadersBuilder builder)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return app.UseMiddleware<ProxyHeadersMiddleware>(builder.Build());
        }

        /// <summary>
        /// Registra el middleware de cabeceras de proxy con configuración por defecto para AWS ECS
        /// </summary>
        public static IApplicationBuilder UsarCabecerasDeProxy(this IApplicationBuilder app)
        {
            return app.UsarCabecerasDeProxy(new ProxyHeadersBuilder().ParaAwsEcs());
        }
    }
} 