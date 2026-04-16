namespace Modelos.DTO.Suscripciones
{
    public class SuscripcionTipoTarifa
    {
        public long Id { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public string StripePriceId { get; set; }

        public bool Activo { get; set; }
    }
}
