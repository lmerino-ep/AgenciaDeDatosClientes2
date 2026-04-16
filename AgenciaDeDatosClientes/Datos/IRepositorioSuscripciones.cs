using Modelos.DTO.Suscripciones;

namespace Datos
{
    public interface IRepositorioSuscripciones
    {
        Task<bool> TieneSuscripcionActivaAsync(int fkUsuario);

        Task<SuscripcionActivaUsuario?> ObtenerSuscripcionActivaDetalleAsync(int fkUsuario);

        Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorIdAsync(long suscripcionTipoId);

        Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorStripePriceIdAsync(string stripePriceId);

        Task<IReadOnlyList<SuscripcionTipoTarifa>> ObtenerSuscripcionesTipoActivasAsync();

        Task<IReadOnlyList<HistorialDescargaItem>> ObtenerHistorialDescargasAsync(int fkUsuario, int limit = 100);

        Task RegistrarEventoWebhookAsync(string eventId, string eventType);

        Task<bool> EventoWebhookProcesadoAsync(string eventId);

        Task UpsertSuscripcionAsync(SuscripcionTarifa suscripcion);

        Task ActualizarEstadoSuscripcionAsync(string stripeSubscriptionId, string estado, DateTime? fechaFin);
    }
}
