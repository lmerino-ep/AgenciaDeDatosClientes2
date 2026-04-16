using Modelos.DTO.Suscripciones;

namespace Negocio
{
    public interface IServicioSuscripciones
    {
        Task<ContextoTarifaUsuario> ObtenerContextoTarifaAsync(int fkUsuario, string usuarioTipo);

        Task<IReadOnlyList<SuscripcionTipoTarifa>> ObtenerSuscripcionesDisponiblesAsync();

        Task<IReadOnlyList<HistorialDescargaItem>> ObtenerHistorialDescargasAsync(int fkUsuario, int limit = 100);

        Task<string> CrearSesionCheckoutAsync(int fkUsuario, string email, long suscripcionTipoId, string scheme, string host);

        Task<SuscripcionActivaUsuario?> ObtenerSuscripcionActivaDetalleAsync(int fkUsuario);

        Task<IReadOnlyList<FacturaStripeResumen>> ObtenerFacturasRecientesAsync(string stripeCustomerId, int limit = 24);

        Task<string> CrearUrlPortalGestionSuscripcionAsync(string stripeCustomerId, string returnUrl);

        Task ProcesarWebhookAsync(string payload, string signatureHeader);
    }
}
