using Utils.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Pages
{
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public ETag ETag { get; set; } //Ojo: esta propiedad debe llamarse así. No cambiar, porque lo casteamos cmomo dynamic con este nombre (ETag) desde el ETagFilterControllers para evitar usar reflection

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string? code = null)
        {
            // ===== PATRÓN OFICIAL DE MICROSOFT =====
            // Referencia: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling

            // 1. Intentar obtener la excepción desde UseExceptionHandler (Patrón Microsoft)
            var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                // Generar GUID único para tracking en CloudWatch
                var exceptionId = Guid.NewGuid().ToString();
                RequestId = exceptionId;

                // Establecer status code 500 para excepciones
                Response.StatusCode = 500;

                // Loggear con GUID para búsqueda posterior en CloudWatch
                _logger.LogError(exceptionFeature.Error,
                    "Excepción no controlada. ExceptionId: {ExceptionId}, Path: {Path}",
                    exceptionId, exceptionFeature.Path);

                return;
            }

            // 2. Si no es una excepción, es un código de estado HTTP (ej: 404)
            // Establecer el status code desde el parámetro de ruta
            if (!string.IsNullOrEmpty(code) && int.TryParse(code, out int statusCode))
            {
                Response.StatusCode = statusCode;
            }
            else
            {
                Response.StatusCode = 500; // Por defecto
            }

            // 3. Usar TraceIdentifier para códigos de estado (no excepciones)
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            _logger.LogWarning("Código de estado HTTP {StatusCode}. TraceId: {TraceId}",
                Response.StatusCode, RequestId);
        }
    }

}
