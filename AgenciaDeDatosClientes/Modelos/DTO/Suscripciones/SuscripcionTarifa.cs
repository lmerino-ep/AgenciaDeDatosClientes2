namespace Modelos.DTO.Suscripciones
{
    public class SuscripcionTarifa
    {
        public int FK_Usuario { get; set; }

        public long FK_SuscripcionTipo { get; set; }

        /// <summary>
        /// Identificador del cliente en el sistema de facturación externo (aún no integrado). Hasta entonces debe quedar vacío.
        /// </summary>
        public string ClienteIdFacturacion { get; set; }

        public string StripeSubscriptionId { get; set; }

        public string StripeCustomerId { get; set; }

        public string Estado { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }
    }
}
