using System;
using System.Collections.Generic;
using Modelos.DTO.ResultadosEmpresariales;
using Stripe;
using Stripe.Checkout;

namespace Negocio.PagosResultados
{
    public class PasarelaPagoStripe : IPasarelaPagoResultados
    {
        private readonly Modelos.InyeccionDeDependencias.Stripe _config;

        public PasarelaPagoStripe(Modelos.InyeccionDeDependencias.Stripe config)
        {
            _config = config;
        }

        public string RutaFormularioCheckout => "/stripe/checkout-resultados";

        public async Task<(string CheckoutId, string CheckoutUrl)> CrearCheckoutAsync(
            ResultadoParaCompra resultado,
            int fkUsuario,
            string email,
            string scheme,
            string host)
        {
            StripeConfiguration.ApiKey = _config.SecretKey;
            var client = new StripeClient(_config.SecretKey);
            var service = new SessionService(client);

            var baseUrl = $"{scheme}://{host}";
            var successPath = string.IsNullOrWhiteSpace(_config.CheckoutSuccessPathResultados) ? "/clientes/resultados-empresariales/gracias" : _config.CheckoutSuccessPathResultados;
            var cancelPath = string.IsNullOrWhiteSpace(_config.CheckoutCancelPathResultados) ? "/clientes/resultados-empresariales" : _config.CheckoutCancelPathResultados;

            // Se replica en PaymentIntentData para poder leerla desde payment_intent.payment_failed.
            var metadata = new Dictionary<string, string>
            {
                ["fk_usuario"] = fkUsuario.ToString(),
                ["resultado_id"] = resultado.ResultadoId.ToString()
            };

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{baseUrl}{successPath}?resultadoId={resultado.ResultadoId}",
                CancelUrl = $"{baseUrl}{cancelPath}",
                CustomerEmail = email,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                Metadata = metadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = metadata
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            UnitAmount = resultado.PrecioCentimos,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = resultado.Titulo,
                                Description = "Descarga de resultados empresariales"
                            }
                        }
                    }
                ]
            };

            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.Url))
            {
                throw new InvalidOperationException("Stripe no devolvió URL de checkout.");
            }

            return (session.Id, session.Url);
        }
    }
}
