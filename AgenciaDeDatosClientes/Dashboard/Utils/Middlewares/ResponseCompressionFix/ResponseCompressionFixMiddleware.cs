using Microsoft.Extensions.Logging;

namespace Utils.Middlewares.ResponseCompressionFix
{
    /// <summary>
    /// Middleware para solucionar problemas de compresión con respuestas 304
    /// Basado en las mejores prácticas de Microsoft para manejo de respuestas 304
    /// </summary>
    public class ResponseCompressionFixMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseCompressionFixMiddleware> _logger;

        public ResponseCompressionFixMiddleware(RequestDelegate next, ILogger<ResponseCompressionFixMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Interceptar la respuesta ANTES de que se procese para prevenir problemas con 304
            var originalBodyStream = context.Response.Body;
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                // Procesar la request
                await _next(context);

                // Verificar si la respuesta es 304
                if (context.Response.StatusCode == 304)
                {
                    // Para respuestas 304, no escribir al cuerpo según el estándar HTTP
                    _logger.LogDebug("Respuesta 304 detectada, evitando escritura al cuerpo. Path: {Path}", context.Request.Path);
                    
                    // Restaurar el stream original y no escribir nada
                    context.Response.Body = originalBodyStream;
                    return;
                }

                // Para respuestas normales, escribir el contenido al stream original
                if (responseBodyStream.Length > 0)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex) when (ex.Message.Contains("Writing to the response body is invalid for responses with status code 304"))
            {
                // Capturar específicamente la excepción de 304 y manejarla silenciosamente
                _logger.LogWarning("Excepción de compresión con respuesta 304 capturada y manejada. Path: {Path}, Exception: {Exception}", 
                    context.Request.Path, ex.Message);
                
                // Para respuestas 304, no hacer nada más
                context.Response.Body = originalBodyStream;
                return;
            }
            catch (Exception ex)
            {
                // Re-lanzar otras excepciones
                _logger.LogError(ex, "Error inesperado en ResponseCompressionFixMiddleware. Path: {Path}", context.Request.Path);
                throw;
            }
            finally
            {
                // Asegurar que el stream original se restaure
                context.Response.Body = originalBodyStream;
                responseBodyStream?.Dispose();
            }
        }
    }
}
