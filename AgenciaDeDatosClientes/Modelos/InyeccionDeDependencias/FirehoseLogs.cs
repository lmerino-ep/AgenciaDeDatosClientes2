using Microsoft.Extensions.Logging;
using System;

namespace Modelos.InyeccionDeDependencias
{
    public class FirehoseLogs
    {
        public bool Enabled { get; set; } = true;
        public string DeliveryStreamName { get; set; }
    }
} 