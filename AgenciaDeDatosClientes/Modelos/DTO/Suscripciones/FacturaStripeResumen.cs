using System;

namespace Modelos.DTO.Suscripciones
{
    public class FacturaStripeResumen
    {
        public string Id { get; set; } = string.Empty;

        public string? Numero { get; set; }

        public DateTime Fecha { get; set; }

        public string ImporteTexto { get; set; } = string.Empty;

        public string? UrlHostedInvoice { get; set; }

        public string? UrlPdf { get; set; }
    }
}
