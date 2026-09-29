using Modelos.DTO.Suscripciones;

namespace Datos
{
    public interface IRepositorioSuscripciones
    {
        Task<bool> TieneSuscripcionActivaAsync(int fkUsuario);

        Task<SuscripcionActivaUsuario?> ObtenerSuscripcionActivaDetalleAsync(int fkUsuario);

        Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorIdAsync(long suscripcionTipoId, bool esTest);

        Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorStripePriceIdAsync(string stripePriceId);

        Task<IReadOnlyList<SuscripcionTipoTarifa>> ObtenerSuscripcionesTipoActivasAsync(bool esTest);

        Task<IReadOnlyList<HistorialDescargaItem>> ObtenerHistorialDescargasAsync(int fkUsuario, int limit = 100);

        Task UpsertSuscripcionAsync(SuscripcionTarifa suscripcion);

        Task ActualizarEstadoSuscripcionAsync(string stripeSubscriptionId, string estado, DateTime? fechaFin);
    }
}
