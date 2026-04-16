using Dapper;
using AgenciaDeDatosBD;
using AgenciaDeDatosBD.Herramientas;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Suscripciones;
using Modelos.InyeccionDeDependencias;

namespace Datos
{
    public class RepositorioSuscripciones : BaseRepositorio, IRepositorioSuscripciones
    {
        private readonly ILogger<RepositorioSuscripciones> _logger;

        public RepositorioSuscripciones(Aurora aurora, ILogger<RepositorioSuscripciones> logger) : base(aurora)
        {
            _logger = logger;
        }

        public async Task<bool> TieneSuscripcionActivaAsync(int fkUsuario)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Suscripcion
                WHERE FK_Usuario = @FkUsuario
                    AND Estado IN ('active', 'trialing')
                    AND (FechaFin IS NULL OR FechaFin >= UTC_TIMESTAMP())
            ;";

            using var connection = ObtenerConexionLectura();
            var total = await connection.ExecuteScalarAsync<int>(sql, new { FkUsuario = fkUsuario });
            return total > 0;
        }

        public async Task<SuscripcionActivaUsuario?> ObtenerSuscripcionActivaDetalleAsync(int fkUsuario)
        {
            const string sql = @"
                SELECT
                    S.StripeCustomerId,
                    S.StripeSubscriptionId,
                    S.Estado,
                    S.FechaInicio,
                    S.FechaFin,
                    ST.Id AS SuscripcionTipoId,
                    ST.Nombre AS PlanNombre
                FROM Suscripcion S
                INNER JOIN SuscripcionTipo ST ON ST.Id = S.FK_SuscripcionTipo
                WHERE S.FK_Usuario = @FkUsuario
                    AND S.Estado IN ('active', 'trialing')
                    AND (S.FechaFin IS NULL OR S.FechaFin >= UTC_TIMESTAMP())
                ORDER BY S.Id DESC
                LIMIT 1
            ;";

            using var connection = ObtenerConexionLectura();
            return await connection.QueryFirstOrDefaultAsync<SuscripcionActivaUsuario>(sql, new { FkUsuario = fkUsuario });
        }

        public async Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorIdAsync(long suscripcionTipoId)
        {
            const string sql = @"
                SELECT
                    Id,
                    Nombre,
                    Descripcion,
                    StripePriceId,
                    Activo
                FROM SuscripcionTipo
                WHERE Id = @SuscripcionTipoId
                    AND Activo = 1
                LIMIT 1
            ;";

            using var connection = ObtenerConexionLectura();
            return await connection.QueryFirstOrDefaultAsync<SuscripcionTipoTarifa>(sql, new { SuscripcionTipoId = suscripcionTipoId });
        }

        public async Task<SuscripcionTipoTarifa?> ObtenerSuscripcionTipoPorStripePriceIdAsync(string stripePriceId)
        {
            const string sql = @"
                SELECT
                    Id,
                    Nombre,
                    Descripcion,
                    StripePriceId,
                    Activo
                FROM SuscripcionTipo
                WHERE StripePriceId = @StripePriceId
                    AND Activo = 1
                LIMIT 1
            ;";

            using var connection = ObtenerConexionLectura();
            return await connection.QueryFirstOrDefaultAsync<SuscripcionTipoTarifa>(sql, new { StripePriceId = stripePriceId });
        }

        public async Task<IReadOnlyList<SuscripcionTipoTarifa>> ObtenerSuscripcionesTipoActivasAsync()
        {
            const string sql = @"
                SELECT
                    Id,
                    Nombre,
                    Descripcion,
                    StripePriceId,
                    Activo
                FROM SuscripcionTipo
                WHERE Activo = 1
                    AND StripePriceId IS NOT NULL
                    AND StripePriceId <> ''
                ORDER BY Id
            ;";

            using var connection = ObtenerConexionLectura();
            var planes = await connection.QueryAsync<SuscripcionTipoTarifa>(sql);
            return planes.ToList();
        }

        public async Task<IReadOnlyList<HistorialDescargaItem>> ObtenerHistorialDescargasAsync(int fkUsuario, int limit = 100)
        {
            const string sql = @"
                SELECT
                    R.Id AS FkRepresentacion,
                    R.GUID AS RepresentacionGuid,
                    COALESCE(NULLIF(TRIM(MAX(RD.Titulo)), ''), R.TituloUrl) AS RepresentacionTitulo,
                    R.TituloUrl AS RepresentacionTituloUrl,
                    MAX(CASE WHEN UA.FK_Actividad = 1 THEN UA.LastUpdate END) AS FechaUltimaDescargaCsv,
                    MAX(CASE WHEN UA.FK_Actividad = 2 THEN UA.LastUpdate END) AS FechaUltimaDescargaJson,
                    MAX(CASE WHEN UA.FK_Actividad = 3 THEN UA.LastUpdate END) AS FechaUltimaDescargaImagen,
                    MAX(UA.LastUpdate) AS FechaUltimaActividad
                FROM UsuarioActividad UA
                INNER JOIN Representacion R ON R.Id = UA.FK_Representacion
                LEFT JOIN RepresentacionDisegno RD ON RD.FK_Representacion = R.Id
                WHERE UA.FK_Usuario = @FkUsuario
                    AND UA.FK_Actividad IN (1, 2, 3)
                    AND UA.FK_Representacion IS NOT NULL
                GROUP BY R.Id, R.GUID, R.TituloUrl
                ORDER BY FechaUltimaActividad DESC
                LIMIT @Limit
            ;";

            using var connection = ObtenerConexionLectura();
            var historial = await connection.QueryAsync<HistorialDescargaItem>(sql, new
            {
                FkUsuario = fkUsuario,
                Limit = limit
            });
            return historial.ToList();
        }

        public async Task RegistrarEventoWebhookAsync(string eventId, string eventType)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            Retorno retorno = await bd.StripeWebhookEvento().Crear(eventId, eventType);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task<bool> EventoWebhookProcesadoAsync(string eventId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM StripeWebhookEvento
                WHERE EventId = @EventId
            ;";

            using var connection = ObtenerConexionLectura();
            var total = await connection.ExecuteScalarAsync<int>(sql, new { EventId = eventId });
            return total > 0;
        }

        public async Task UpsertSuscripcionAsync(SuscripcionTarifa suscripcion)
        {
            const string selectSql = @"
                SELECT Id
                FROM Suscripcion
                WHERE StripeSubscriptionId = @StripeSubscriptionId
                LIMIT 1
            ;";

            using var connection = ObtenerConexionEscritura();
            var existingId = await connection.QueryFirstOrDefaultAsync<long?>(selectSql, new { suscripcion.StripeSubscriptionId });
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var fkSuscripcionTipo = Convert.ToInt32(suscripcion.FK_SuscripcionTipo);

            if (existingId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(suscripcion.StripeCustomerId))
                {
                    const string sqlCustomerActual = @"
                        SELECT StripeCustomerId
                        FROM Suscripcion
                        WHERE StripeSubscriptionId = @StripeSubscriptionId
                        LIMIT 1
                    ;";
                    var customerGuardado = await connection.QueryFirstOrDefaultAsync<string>(sqlCustomerActual, new { suscripcion.StripeSubscriptionId });
                    if (!string.IsNullOrWhiteSpace(customerGuardado))
                    {
                        suscripcion.StripeCustomerId = customerGuardado;
                    }
                }

                Retorno retornoActualizar = await bd.Suscripcion().ActualizarPorStripeSubscriptionId(
                    suscripcion.StripeSubscriptionId,
                    fkSuscripcionTipo,
                    suscripcion.ClienteIdFacturacion,
                    suscripcion.StripeCustomerId,
                    suscripcion.Estado,
                    suscripcion.FechaInicio,
                    suscripcion.FechaFin);

                if (!retornoActualizar.Correcto)
                {
                    throw new Exception(retornoActualizar.Mensaje);
                }

                return;
            }

            Retorno retornoCrear = await bd.Suscripcion().Crear(
                suscripcion.FK_Usuario,
                fkSuscripcionTipo,
                suscripcion.ClienteIdFacturacion,
                suscripcion.StripeSubscriptionId,
                suscripcion.StripeCustomerId,
                suscripcion.Estado,
                suscripcion.FechaInicio,
                suscripcion.FechaFin);

            if (!retornoCrear.Correcto)
            {
                throw new Exception(retornoCrear.Mensaje);
            }
        }

        public async Task ActualizarEstadoSuscripcionAsync(string stripeSubscriptionId, string estado, DateTime? fechaFin)
        {
            using var connection = ObtenerConexionEscritura();
            const string sqlValidacion = @"
                SELECT COUNT(1)
                FROM Suscripcion
                WHERE StripeSubscriptionId = @StripeSubscriptionId
            ;";
            var rows = await connection.ExecuteScalarAsync<int>(sqlValidacion, new
            {
                StripeSubscriptionId = stripeSubscriptionId
            });

            if (rows == 0)
            {
                _logger.LogWarning("No se encontró suscripción para actualizar estado. StripeSubscriptionId: {StripeSubscriptionId}", stripeSubscriptionId);
                return;
            }

            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            Retorno retorno = await bd.Suscripcion().ActualizarEstadoPorStripeSubscriptionId(
                stripeSubscriptionId,
                estado,
                fechaFin);

            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }
    }
}
