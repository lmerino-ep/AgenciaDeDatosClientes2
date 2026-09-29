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

            Event stripeEvent;
            try
            {
                // throwOnApiVersionMismatch: false porque la cuenta de Stripe usa una API version antigua (2020-08-27) que no coincide con la del SDK.
                stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _stripeConfig.WebhookSecret, 300, throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex) when (EsFalloDeVerificacionDeFirma(ex))
            {
                throw new FirmaWebhookInvalidaException("Firma de webhook de Stripe inválida.", ex);
            }

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

        // Stripe.net no distingue firma inválida de otros StripeException con un tipo propio; se detecta por mensaje.
        private static bool EsFalloDeVerificacionDeFirma(StripeException ex)
        {
            var mensaje = ex.Message ?? string.Empty;
            return mensaje.Contains("expected signature", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("signature header format", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("outside of the allowed tolerance", StringComparison.OrdinalIgnoreCase)
                || mensaje.Contains("signature cannot be safely calculated", StringComparison.OrdinalIgnoreCase);
        }
    }
}
