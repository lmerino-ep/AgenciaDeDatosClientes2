using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Modelos.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard.Utils.Logging
{
    /// <summary>
    /// Provider personalizado para enviar logs de ILogger a AWS CloudWatch Logs
    /// </summary>
    public class CloudWatchLogsProvider : ILoggerProvider
    {
        private readonly string _logGroupName;
        private readonly LogLevel _logLevel;
        private readonly IAmazonCloudWatchLogs _cloudWatchLogsClient;
        private readonly ConcurrentDictionary<string, CloudWatchLogger> _loggers;
        private readonly Timer _flushTimer;
        private readonly ConcurrentQueue<LogMessage> _logQueue;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public CloudWatchLogsProvider(string logGroupName, Amazon.RegionEndpoint region, LogLevel logLevel = LogLevel.Information)
        {
            _logGroupName = logGroupName;
            _logLevel = logLevel;
            _cloudWatchLogsClient = new AmazonCloudWatchLogsClient(region);
            _loggers = new ConcurrentDictionary<string, CloudWatchLogger>();
            _logQueue = new ConcurrentQueue<LogMessage>();
            
            // Timer para enviar logs en lotes cada 5 segundos
            _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CloudWatchLogger(name, this));
        }

        internal void EnqueueLog(LogLevel logLevel, string categoryName, string message, Exception exception = null)
        {
            if (logLevel >= _logLevel)
            {
                var logMessage = new LogMessage
                {
                    Timestamp = DateTime.UtcNow,
                    LogLevel = logLevel,
                    CategoryName = categoryName,
                    Message = message,
                    Exception = exception
                };

                _logQueue.Enqueue(logMessage);
            }
        }

        private async void FlushLogs(object state)
        {
            if (_disposed) return;

            var logsToSend = new List<LogMessage>();
            
            // Extraer todos los logs de la cola
            while (_logQueue.TryDequeue(out var logMessage))
            {
                logsToSend.Add(logMessage);
            }

            if (logsToSend.Count > 0)
            {
                await SendLogsToCloudWatch(logsToSend);
            }
        }

        private async Task SendLogsToCloudWatch(List<LogMessage> logs)
        {
            try
            {
                // Asegurar que el log group existe
                await EnsureLogGroupExists();

                // Crear stream name basado en la fecha
                var streamName = $"epdata-{DateTime.UtcNow:yyyy-MM-dd-HH}";

                // Crear el log stream si no existe
                await EnsureLogStreamExists(streamName);

                // Preparar los eventos de log
                var logEvents = new List<InputLogEvent>();
                foreach (var log in logs)
                {
                    var logEntry = FormatLogEntry(log);
                    logEvents.Add(new InputLogEvent
                    {
                        Message = logEntry,
                        Timestamp = log.Timestamp
                    });
                }

                // Enviar logs en lotes de 100 (límite de AWS)
                for (int i = 0; i < logEvents.Count; i += 100)
                {
                    var batch = logEvents.Skip(i).Take(100).ToList();
                    if (batch.Count > 0)
                    {
                        var request = new PutLogEventsRequest
                        {
                            LogGroupName = _logGroupName,
                            LogStreamName = streamName,
                            LogEvents = batch
                        };

                        await _cloudWatchLogsClient.PutLogEventsAsync(request);
                    }
                }
            }
            catch (Exception ex)
            {
                // En caso de error, escribir en consola para debugging
                Console.WriteLine($"Error sending logs to CloudWatch: {ex.Message}");
            }
        }

        private async Task EnsureLogGroupExists()
        {
            try
            {
                var describeRequest = new DescribeLogGroupsRequest
                {
                    LogGroupNamePrefix = _logGroupName
                };

                var response = await _cloudWatchLogsClient.DescribeLogGroupsAsync(describeRequest);
                
                if (!response.LogGroups.Any(lg => lg.LogGroupName == _logGroupName))
                {
                    var createRequest = new CreateLogGroupRequest
                    {
                        LogGroupName = _logGroupName
                    };

                    await _cloudWatchLogsClient.CreateLogGroupAsync(createRequest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring log group exists: {ex.Message}");
            }
        }

        private async Task EnsureLogStreamExists(string streamName)
        {
            try
            {
                var describeRequest = new DescribeLogStreamsRequest
                {
                    LogGroupName = _logGroupName,
                    LogStreamNamePrefix = streamName
                };

                var response = await _cloudWatchLogsClient.DescribeLogStreamsAsync(describeRequest);
                
                if (!response.LogStreams.Any(ls => ls.LogStreamName == streamName))
                {
                    var createRequest = new CreateLogStreamRequest
                    {
                        LogGroupName = _logGroupName,
                        LogStreamName = streamName
                    };

                    await _cloudWatchLogsClient.CreateLogStreamAsync(createRequest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring log stream exists: {ex.Message}");
            }
        }

        private string FormatLogEntry(LogMessage logMessage)
        {
            var sb = new StringBuilder();
            
            // Timestamp
            sb.Append($"[{logMessage.Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC] ");
            
            // Log Level
            sb.Append($"[{logMessage.LogLevel}] ");
            
            // Category
            sb.Append($"[{logMessage.CategoryName}] ");
            
            // Message
            sb.Append(logMessage.Message);
            
            // Exception
            if (logMessage.Exception != null)
            {
                sb.Append($" | Exception: {logMessage.Exception.Message}");
                if (logMessage.Exception.StackTrace != null)
                {
                    sb.Append($" | StackTrace: {logMessage.Exception.StackTrace}");
                }
            }
            
            return sb.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _flushTimer?.Dispose();
                _cloudWatchLogsClient?.Dispose();
            }
        }
    }
} 