namespace Datos
{
    // Deduplicación de eventos de Stripe (StripeWebhookEvento, UNIQUE sobre EventId), compartida
    // entre suscripciones y compras de Resultados Empresariales.
    public interface IRepositorioWebhooksStripe
    {
        Task<bool> EventoProcesadoAsync(string eventId);

        Task RegistrarAsync(string eventId, string eventType);
    }
}
