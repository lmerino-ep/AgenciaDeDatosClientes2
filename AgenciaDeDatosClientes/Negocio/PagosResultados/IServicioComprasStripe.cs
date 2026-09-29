using Stripe;

namespace Negocio.PagosResultados
{
    // Eventos de Stripe (Mode="payment") de Resultados Empresariales, ya verificados y deduplicados.
    public interface IServicioComprasStripe
    {
        Task ProcesarEventoAsync(Event stripeEvent);
    }
}
