using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Modelos.Autenticacion;

namespace Utils.Middlewares.GlobalExceptionHandler
{
    /// <summary>
    /// Middleware para capturar excepciones no controladas y registrarlas en los logs
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<Usuario> _userManager;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment environment,
            UserManager<Usuario> userManager)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Generar ExceptionId para todos los casos
                var exceptionId = Guid.NewGuid().ToString();

                // Logear la excepción siempre
                try
                {
                    await HandleExceptionAsync(context, ex, exceptionId);
                }
                catch (Exception loggingEx)
                {
                    // No pasar la excepción como primer parámetro para evitar conflictos de propiedades duplicadas
                    _logger.LogError("Error al procesar la excepción original: {OriginalException}. Logging Exception: {LoggingException}", ex.Message, loggingEx.Message);
                }

                // Solo intentar redirigir si la respuesta no se ha iniciado
                if (!context.Response.HasStarted)
                {
                    try
                    {
                        await RedirectToErrorPage(context, exceptionId);
                    }
                    catch (Exception redirectEx)
                    {
                        // No pasar la excepción como primer parámetro para evitar conflictos de propiedades duplicadas
                        _logger.LogError("Error al redirigir a la página de error. ExceptionId: {ExceptionId}. Redirect Exception: {RedirectException}", exceptionId, redirectEx.Message);
                    }
                }
                else
                {
                    _logger.LogWarning("No se puede redirigir porque la respuesta ya se inició. ExceptionId: {ExceptionId}. Request: {Method} {Path}",
                        exceptionId, context.Request.Method, context.Request.Path);
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string exceptionId)
        {
            // Obtener UserId de forma segura (sin exponer datos sensibles)
            string userId = "Anonymous";
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    userId = _userManager.GetUserId(context.User) ?? "Unknown";
                }
                catch
                {
                    userId = "Unknown";
                }
            }

            // Obtener información del request (sin datos sensibles)
            var requestInfo = new
            {
                Method = context.Request.Method,
                Path = context.Request.Path
            };

            // Construir el mensaje completo con stack trace (limpiando saltos de línea para CloudWatch)
            var stackTrace = (exception.StackTrace ?? "No stack trace disponible")
                .Replace("\r\n", " >> ")
                .Replace("\n", " >> ")
                .Replace("\r", " >> ");
            
            var exceptionMessage = exception.Message
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");
            
            var logMessage = $"Excepción no controlada capturada. ExceptionId: {exceptionId}. Request: {{ Method = {requestInfo.Method}, Path = {requestInfo.Path}, UserId = {userId} }}. " +
                             $"Exception Type: {exception.GetType().FullName}. " +
                             $"Message: {exceptionMessage}. " +
                             $"Stack Trace: {stackTrace}";

            // Agregar excepciones internas si existen
            var innerException = exception.InnerException;
            var innerExceptionCount = 1;
            while (innerException != null)
            {
                var innerStackTrace = (innerException.StackTrace ?? "No stack trace disponible")
                    .Replace("\r\n", " >> ")
                    .Replace("\n", " >> ")
                    .Replace("\r", " >> ");
                
                var innerMessage = innerException.Message
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");
                
                logMessage += $" | Inner Exception #{innerExceptionCount}: Type = {innerException.GetType().FullName}, Message = {innerMessage}, Stack Trace = {innerStackTrace}";
                innerException = innerException.InnerException;
                innerExceptionCount++;
            }

            // Log único con toda la información en una sola línea
            // No pasamos la excepción como primer parámetro para evitar conflictos de propiedades duplicadas
            _logger.LogError(logMessage);

            // El ExceptionId se pasará como parámetro en la URL de redirección
            // No es necesario almacenarlo en el contexto
        }

        private async Task RedirectToErrorPage(HttpContext context, string exceptionId)
        {
            try
            {
                // Verificar que la respuesta no se haya iniciado
                if (!context.Response.HasStarted)
                {
                    // Limpiar cualquier contenido previo
                    context.Response.Clear();

                    // Almacenar el ExceptionId en Items para que la página de error pueda accederlo
                    context.Items["ExceptionId"] = exceptionId;

                    // Agregar cabeceras para evitar el cacheo
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";

                    // SOLUCIÓN: Simplemente establecer el status code a 500
                    // UseStatusCodePagesWithReExecute (configurado en ApplicationBuilderExtensions.cs línea 109)
                    // interceptará automáticamente este código y re-ejecutará /error/500
                    // La página Error.cshtml puede acceder al ExceptionId desde context.Items
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    // Log para confirmar que se estableció el status code
                    _logger.LogInformation("GlobalExceptionHandler: Status code 500 establecido. UseStatusCodePagesWithReExecute debería interceptar. ExceptionId: {ExceptionId}", exceptionId);
                }
                else
                {
                    _logger.LogWarning("No se puede redirigir porque la respuesta ya se inició. ExceptionId: {ExceptionId}", exceptionId);
                }
            }
            catch (Exception ex)
            {
                // No pasar la excepción como primer parámetro para evitar conflictos de propiedades duplicadas
                _logger.LogError("Error al procesar la redirección a la página de error. ExceptionId: {ExceptionId}. Exception: {Exception}", exceptionId, ex.Message);
            }
        }
    }
}
