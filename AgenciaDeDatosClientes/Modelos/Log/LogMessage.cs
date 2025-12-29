using Microsoft.Extensions.Logging;
using System;

namespace Modelos.Log
{
    /// <summary>
    /// Representa un mensaje de log para envío a CloudWatch
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// Timestamp UTC del mensaje de log
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Nivel de log (Information, Warning, Error, etc.)
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Categoría del logger (típicamente el nombre de la clase)
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Mensaje del log
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Excepción asociada (si existe)
        /// </summary>
        public Exception Exception { get; set; }
    }
}
