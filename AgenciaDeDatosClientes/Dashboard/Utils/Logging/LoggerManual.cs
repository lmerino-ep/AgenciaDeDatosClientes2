using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Utils.Logging
{
    /// <summary>
    /// Logger específico para búsquededas de usuarios y otros logs específicos
    /// Envía directamente a Kinesis Data Firehose → S3
    /// </summary>
    public class LoggerManual
    {
        private readonly IAmazonKinesisFirehose _firehoseClient;
        private readonly string _deliveryStreamName;
        private readonly ILogger<LoggerManual> _logger;

        public LoggerManual(IAmazonKinesisFirehose firehoseClient, string deliveryStreamName, ILogger<LoggerManual> logger)
        {
            _firehoseClient = firehoseClient;
            _deliveryStreamName = deliveryStreamName;
            _logger = logger;
        }

        /// <summary>
        /// Registra una búsqueda de usuario
        /// </summary>
        public virtual async Task LogAsync<T>(T mensaje, string tipo, string urlOrigen, int? fk_Usuario = null)
        {
            try
            {
                var logEntry = new
                {
                    fecha = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    tipo = tipo,
                    mensaje = mensaje,
                    fk_usuario = fk_Usuario,
                    url_origen = urlOrigen
                };                
                await EnviarAFirehoseAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando búsqueda de usuario a Firehose");
            }
        }
        
        private async Task EnviarAFirehoseAsync(object logEntry)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            var jsonLog = JsonSerializer.Serialize(logEntry, options) + "\n";
            var bytes = Encoding.UTF8.GetBytes(jsonLog);

            var request = new PutRecordRequest
            {
                DeliveryStreamName = _deliveryStreamName,
                Record = new Record
                {
                    Data = new MemoryStream(bytes)
                }
            };

            await _firehoseClient.PutRecordAsync(request);
        }
    }
} 
