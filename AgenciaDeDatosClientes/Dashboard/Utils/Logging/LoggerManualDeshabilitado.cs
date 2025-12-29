using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Utils.Logging
{
    public class LoggerManualDeshabilitado : LoggerManual
    {
        private readonly ILogger<LoggerManual> _logger;

        public LoggerManualDeshabilitado(ILogger<LoggerManual> logger)
            : base(null, null, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// No hace nada - solo registra en CloudWatch
        /// </summary>
        public override async Task LogAsync<T>(T mensaje, string tipo, string urlOrigen, int? fk_Usuario = null)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
            var json = JsonSerializer.Serialize(mensaje, options);
            _logger.LogDebug("LoggerManual deshabilitado - registro no enviado a S3: {json}", json);
            await Task.CompletedTask;
        }
    }
}
