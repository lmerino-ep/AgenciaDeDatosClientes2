namespace Negocio
{
    // Punto único de entrada de /stripe/webhook: verifica firma, deduplica y reparte entre suscripciones y compras.
    public interface IServicioWebhookStripe
    {
        Task ProcesarAsync(string payload, string signatureHeader);
    }
}
