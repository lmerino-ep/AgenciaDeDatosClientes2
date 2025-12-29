namespace Utils.Middlewares.RequestTiming
{
    /// <summary>
    /// Extensiones para registrar el middleware de timing de requests
    /// </summary>
    public static class RequestTimingExtensions
    {
        /// <summary>
        /// Registra el middleware de timing de requests
        /// </summary>
        public static IApplicationBuilder UsarTimingDeRequests(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<RequestTimingMiddleware>();
        }
    }
} 
