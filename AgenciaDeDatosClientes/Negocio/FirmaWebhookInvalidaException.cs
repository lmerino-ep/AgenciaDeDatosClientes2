using System;

namespace Negocio
{
    /// <summary>
    /// Señala que la firma de un webhook entrante (Stripe, LemonSqueezy) no se pudo verificar,
    /// es decir, la petición no es confiable. Se distingue de cualquier otro fallo al procesar
    /// el evento, que representa un error interno y no debe tratarse como petición inválida.
    /// </summary>
    public class FirmaWebhookInvalidaException : Exception
    {
        public FirmaWebhookInvalidaException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
