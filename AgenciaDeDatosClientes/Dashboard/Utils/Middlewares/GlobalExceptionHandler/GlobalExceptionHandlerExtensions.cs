namespace Utils.Middlewares.GlobalExceptionHandler
{
    /// <summary>
    /// Extensiones para configurar el middleware de manejo global de excepciones
    /// </summary>
    public static class GlobalExceptionHandlerExtensions
    {
        /// <summary>
        /// Agrega el middleware de manejo global de excepciones al pipeline de la aplicación
        /// </summary>
        /// <param name="app">La instancia de la aplicación web</param>
        /// <returns>La instancia de la aplicación web para encadenamiento</returns>
        public static IApplicationBuilder UsarManejoGlobalDeExcepciones(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
} 
