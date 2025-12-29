namespace Utils.Middlewares.RequestTiming
{
    /// <summary>
    /// Middleware para medir el tiempo de respuesta de las requests
    /// </summary>
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestTimingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Registrar el tiempo de inicio
            var startTime = DateTime.UtcNow;
            context.Items["RequestStartTime"] = startTime;

            // Procesar la request
            await _next(context);

            // Calcular el tiempo de respuesta
            var endTime = DateTime.UtcNow;
            var responseTime = endTime - startTime;

            // Almacenar el tiempo de respuesta en el contexto para que otros middleware puedan usarlo
            context.Items["RequestResponseTime"] = responseTime;

            // Log del tiempo de respuesta (opcional)
            var logger = context.RequestServices.GetService<ILogger<RequestTimingMiddleware>>();
            if (logger != null)
            {
                logger.LogDebug("Request {Method} {Path} completed in {ResponseTime}ms", 
                    context.Request.Method, 
                    context.Request.Path, 
                    responseTime.TotalMilliseconds);
            }
        }
    }
} 
