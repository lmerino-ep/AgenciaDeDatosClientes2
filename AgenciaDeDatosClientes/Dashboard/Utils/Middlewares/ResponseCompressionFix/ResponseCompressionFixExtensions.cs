using Microsoft.AspNetCore.Builder;

namespace Dashboard.Utils.Middlewares.ResponseCompressionFix
{
    /// <summary>
    /// Extensiones para configurar el middleware de corrección de compresión
    /// </summary>
    public static class ResponseCompressionFixExtensions
    {
        /// <summary>
        /// Añade el middleware de corrección de compresión para respuestas 304
        /// </summary>
        /// <param name="app">El ApplicationBuilder</param>
        /// <returns>El ApplicationBuilder para chaining</returns>
        public static IApplicationBuilder UseResponseCompressionFix(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<ResponseCompressionFixMiddleware>();
        }
    }
}
