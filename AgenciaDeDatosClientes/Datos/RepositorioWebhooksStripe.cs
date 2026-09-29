using Dapper;
using AgenciaDeDatosBD;
using Modelos.InyeccionDeDependencias;

namespace Datos
{
    public class RepositorioWebhooksStripe : BaseRepositorio, IRepositorioWebhooksStripe
    {
        public RepositorioWebhooksStripe(Aurora aurora) : base(aurora)
        {
        }

        public async Task<bool> EventoProcesadoAsync(string eventId)
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

        public async Task RegistrarAsync(string eventId, string eventType)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.StripeWebhookEvento().Crear(eventId, eventType);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }
    }
}
