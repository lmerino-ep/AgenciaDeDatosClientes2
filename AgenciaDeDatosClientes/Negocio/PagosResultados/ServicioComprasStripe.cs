using System;
using System.Collections.Generic;
using Datos;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Negocio.PagosResultados
{
    public class ServicioComprasStripe : IServicioComprasStripe
    {
        private const string EstadoPagoCompletado = "paid";

        private readonly IRepositorioComprasResultados _repositorio;
        private readonly ILogger<ServicioComprasStripe> _logger;

        public ServicioComprasStripe(IRepositorioComprasResultados repositorio, ILogger<ServicioComprasStripe> logger)
        {
            _repositorio = repositorio;
            _logger = logger;
        }

        public async Task ProcesarEventoAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await ProcesarCheckoutCompletadoAsync(stripeEvent);
                    break;
                case "checkout.session.expired":
                    await ProcesarCheckoutExpiradoAsync(stripeEvent);
                    break;
                case "payment_intent.payment_failed":
                    await ProcesarPagoFallidoAsync(stripeEvent);
                    break;
                case "charge.refunded":
                    await ProcesarReembolsoAsync(stripeEvent);
                    break;
            }
        }

        private async Task ProcesarCheckoutCompletadoAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null || !string.Equals(session.PaymentStatus, EstadoPagoCompletado, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryResolverMetadata(session.Metadata, out var fkUsuario, out var resultadoId))
            {
                _logger.LogWarning("No se pudo resolver fk_usuario/resultado_id en la sesión de Stripe {SessionId}", session.Id);
                return;
            }

            await _repositorio.ConfirmarCompraAsync(
                fkUsuario,
                resultadoId,
                session.PaymentIntentId,
                numeroPedido: null,
                importeCentimos: (int?)session.AmountTotal,
                moneda: session.Currency,
                emailComprador: session.CustomerDetails?.Email ?? session.CustomerEmail,
                esTest: !stripeEvent.Livemode,
                fechaCompra: stripeEvent.Created);
        }

        private async Task ProcesarCheckoutExpiradoAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null || !TryResolverMetadata(session.Metadata, out var fkUsuario, out var resultadoId))
            {
                return;
            }

            await _repositorio.MarcarFallidaAsync(fkUsuario, resultadoId, session.Id);
        }

        private async Task ProcesarPagoFallidoAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            // Los PaymentIntent de facturas de suscripción no llevan fk_usuario/resultado_id: se ignoran solos.
            if (paymentIntent == null || !TryResolverMetadata(paymentIntent.Metadata, out var fkUsuario, out var resultadoId))
            {
                return;
            }

            await _repositorio.MarcarFallidaAsync(fkUsuario, resultadoId, checkoutId: null);
        }

        private async Task ProcesarReembolsoAsync(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null || string.IsNullOrWhiteSpace(charge.PaymentIntentId))
            {
                return;
            }

            await _repositorio.MarcarReembolsadaAsync(charge.PaymentIntentId);
        }

        private static bool TryResolverMetadata(Dictionary<string, string> metadata, out int fkUsuario, out int resultadoId)
        {
            fkUsuario = 0;
            resultadoId = 0;

            return metadata != null
                && metadata.TryGetValue("fk_usuario", out var fkUsuarioTexto) && int.TryParse(fkUsuarioTexto, out fkUsuario)
                && metadata.TryGetValue("resultado_id", out var resultadoIdTexto) && int.TryParse(resultadoIdTexto, out resultadoId);
        }
    }
}
