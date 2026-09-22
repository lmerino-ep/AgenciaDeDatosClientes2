namespace Modelos.DTO.Suscripciones
{
    public class SuscripcionTipoTarifa
    {
        public long Id { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public string StripePriceId { get; set; }

        public bool Activo { get; set; }

        /// <summary>
        /// true si el StripePriceId pertenece al modo Test de Stripe (no al modo Live).
        /// </summary>
        public bool EsTest { get; set; }
    }
}
