using System;

namespace Modelos.DTO.Suscripciones
{
    public class SuscripcionActivaUsuario
    {
        public string StripeCustomerId { get; set; } = string.Empty;

        public string StripeSubscriptionId { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public long SuscripcionTipoId { get; set; }

        public string PlanNombre { get; set; } = string.Empty;

        /// <summary>
        /// La suscripción sigue en estado activo/trialing en Stripe pero el usuario ya ha programado la baja
        /// (p. ej. <c>cancel_at</c> o cancelación al final del periodo). El acceso sigue hasta <see cref="FechaFin"/>.
        /// </summary>
        public bool TieneCancelacionProgramada =>
            (string.Equals(Estado, "active", StringComparison.OrdinalIgnoreCase)
             || string.Equals(Estado, "trialing", StringComparison.OrdinalIgnoreCase))
            && FechaFin.HasValue
            && FechaFin.Value > DateTime.UtcNow;
    }
}
