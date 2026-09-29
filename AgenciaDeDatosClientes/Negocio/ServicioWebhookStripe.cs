using System;
using Datos;
using Microsoft.Extensions.Logging;
using Negocio.PagosResultados;
using Stripe;
using Stripe.Checkout;

namespace Negocio
{
    public class ServicioWebhookStripe : IServicioWebhookStripe
    {
        private readonly Modelos.InyeccionDeDependencias.Stripe _stripeConfig;
        private readonly IServicioSuscripciones _servicioSuscripciones;
        private readonly IServicioComprasStripe _servicioComprasStripe;
        private readonly IRepositorioWebhooksStripe _repositorioWebhooks;
        private readonly ILogger<ServicioWebhookStripe> _logger;

        public ServicioWebhookStripe(
            Modelos.InyeccionDeDependencias.Stripe stripeConfig,
            IServicioSuscripciones servicioSuscripciones,
            IServicioComprasStripe servicioComprasStripe,
            IRepositorioWebhooksStripe repositorioWebhooks,
            ILogger<ServicioWebhookStripe> logger)
        {
            _stripeConfig = stripeConfig;
            _servicioSuscripciones = servicioSuscripciones;
            _servicioComprasStripe = servicioComprasStripe;
            _repositorioWebhooks = repositorioWebhooks;
            _logger = logger;
        }

        public async Task ProcesarAsync(string payload, string signatureHeader)
        {
            StripeConfiguration.ApiKey = _stripeConfig.SecretKey;

            try
            {
                EventUtility.ValidateSignature(payload, signatureHeader, _stripeConfig.WebhookSecret);
            }
            catch (StripeException ex)
            {
                throw new FirmaWebhookInvalidaException("Firma de webhook de Stripe inválida.", ex);
            }

            // La cuenta de Stripe tiene fijada api_version 2020-08-27 a nivel de cuenta (no de
            // endpoint: crear un WebhookEndpoint con --api-version no lo cambia, comprobado en
            // producción) y no se puede subir sin afectar a otra integración que comparte la
            // cuenta. Se desactiva la comprobación de versión del SDK; los campos que cambiaron de
            // forma entre esa versión y la del SDK se resuelven con reserva desde el JSON crudo
            // (ver ServicioSuscripciones.ObtenerSubscriptionIdDeInvoice) o refrescando el recurso
            // vía la API en vez de fiarse del payload del webhook.
            var stripeEvent = EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);

            if (await _repositorioWebhooks.EventoProcesadoAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Evento Stripe ya procesado: {EventId}", stripeEvent.Id);
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.expired":
                    await DespacharPorModoDeSesionAsync(stripeEvent);
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                case "invoice.paid":
                case "invoice.payment_failed":
                    await _servicioSuscripciones.ProcesarEventoAsync(stripeEvent);
                    break;
                case "payment_intent.payment_failed":
                case "charge.refunded":
                    await _servicioComprasStripe.ProcesarEventoAsync(stripeEvent);
                    break;
            }

            await _repositorioWebhooks.RegistrarAsync(stripeEvent.Id, stripeEvent.Type);
        }

        // checkout.session.completed/expired llega en Mode=subscription y en Mode=payment: se reparte por Session.Mode.
        private async Task DespacharPorModoDeSesionAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (string.Equals(session?.Mode, "payment", StringComparison.OrdinalIgnoreCase))
            {
                await _servicioComprasStripe.ProcesarEventoAsync(stripeEvent);
            }
            else
            {
                await _servicioSuscripciones.ProcesarEventoAsync(stripeEvent);
            }
        }
    }
}
