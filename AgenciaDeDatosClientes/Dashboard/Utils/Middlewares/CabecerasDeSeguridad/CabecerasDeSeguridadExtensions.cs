namespace Dashboard.Utils.Middlewares.CabecerasDeSeguridad
{
    public static class CabecerasDeSeguridadExtensions
    {
        public static IApplicationBuilder UsarCabecerasDeSeguridad(this IApplicationBuilder app, CabecerasDeSeguridadBuilder builder)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return app.UseMiddleware<CabecerasDeSeguridad>(builder.Build());
        }
    }
}
