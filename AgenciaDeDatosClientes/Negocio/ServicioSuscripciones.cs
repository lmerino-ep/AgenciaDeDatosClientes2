using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Datos;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Suscripciones;
using Modelos.InyeccionDeDependencias;
using Stripe;
using Stripe.Checkout;

namespace Negocio
{
    public class ServicioSuscripciones : IServicioSuscripciones
    {
        /// <summary>
        /// Formato numérico tipo es-ES sin llamar a <see cref="CultureInfo.GetCultureInfo(string)"/>,
        /// para que funcione con <c>System.Globalization.InvariantGlobalization</c> (Docker/Alpine).
        /// </summary>
        private static readonly NumberFormatInfo FormatoNumeroMostrarEs = CrearFormatoNumeroMostrarEs();

        private static NumberFormatInfo CrearFormatoNumeroMostrarEs()
        {
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberDecimalSeparator = ",";
            nfi.NumberGroupSeparator = ".";
            return nfi;
        }

        private static readonly HashSet<string> EstadosActivos = ["active", "trialing"];
        private static readonly HashSet<string> TiposTrabajadorEuropaPress = ["2", "3", "4", "5", "6"];
        private readonly IRepositorioSuscripciones _repositorioSuscripciones;
        private readonly Modelos.InyeccionDeDependencias.Stripe _stripeConfig;
        private readonly ILogger<ServicioSuscripciones> _logger;

        public ServicioSuscripciones(
            IRepositorioSuscripciones repositorioSuscripciones,
            Modelos.InyeccionDeDependencias.Stripe stripeConfig,
            ILogger<ServicioSuscripciones> logger)
        {
            _repositorioSuscripciones = repositorioSuscripciones;
            _stripeConfig = stripeConfig;
            _logger = logger;
        }

        /// <summary>
        /// true si la SecretKey configurada es de modo Test de Stripe (sk_test_...), false si es de modo Live.
        /// Determina qué filas de SuscripcionTipo (EsTest) se ofrecen y se pueden contratar.
        /// </summary>
        private bool EsModoTest => _stripeConfig.SecretKey?.StartsWith("sk_test_", StringComparison.Ordinal) ?? true;

        public async Task<ContextoTarifaUsuario> ObtenerContextoTarifaAsync(int fkUsuario, string usuarioTipo)
        {
            if (TiposTrabajadorEuropaPress.Contains(usuarioTipo))
            {
                return new ContextoTarifaUsuario
                {
                    EsTrabajadorEuropaPress = true,
                    TieneSuscripcionActiva = true
                };
            }

            var tieneSuscripcionActiva = await _repositorioSuscripciones.TieneSuscripcionActivaAsync(fkUsuario);
            return new ContextoTarifaUsuario
            {
                EsTrabajadorEuropaPress = false,
                TieneSuscripcionActiva = tieneSuscripcionActiva
            };
        }

        public async Task<IReadOnlyList<SuscripcionTipoTarifa>> ObtenerSuscripcionesDisponiblesAsync()
        {
            return await _repositorioSuscripciones.ObtenerSuscripcionesTipoActivasAsync(EsModoTest);
        }

        public async Task<IReadOnlyList<HistorialDescargaItem>> ObtenerHistorialDescargasAsync(int fkUsuario, int limit = 100)
        {
            return await _repositorioSuscripciones.ObtenerHistorialDescargasAsync(fkUsuario, limit);
        }

        public async Task<SuscripcionActivaUsuario?> ObtenerSuscripcionActivaDetalleAsync(int fkUsuario)
        {
            return await _repositorioSuscripciones.ObtenerSuscripcionActivaDetalleAsync(fkUsuario);
        }

        public async Task<IReadOnlyList<FacturaStripeResumen>> ObtenerFacturasRecientesAsync(string stripeCustomerId, int limit = 24)
        {
            if (string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                return [];
            }

            StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
            var invoiceService = new InvoiceService();
            var listOptions = new InvoiceListOptions
            {
                Customer = stripeCustomerId,
                Limit = limit
            };
            var invoices = await invoiceService.ListAsync(listOptions);
            var resultado = new List<FacturaStripeResumen>(invoices.Data.Count);
            foreach (var inv in invoices.Data)
            {
                resultado.Add(new FacturaStripeResumen
                {
                    Id = inv.Id,
                    Numero = inv.Number,
                    Fecha = inv.Created,
                    ImporteTexto = FormatearImporteFactura(inv.AmountPaid, inv.Currency),
                    UrlHostedInvoice = inv.HostedInvoiceUrl,
                    UrlPdf = inv.InvoicePdf
                });
            }

            return resultado;
        }

        public async Task<string> CrearUrlPortalGestionSuscripcionAsync(string stripeCustomerId, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                throw new ArgumentException("Se requiere el cliente de Stripe.", nameof(stripeCustomerId));
            }

            StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = returnUrl
            };
            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.Url))
            {
                throw new InvalidOperationException("Stripe no devolvió URL del portal de facturación.");
            }

            return session.Url;
        }

        private static string FormatearImporteFactura(long cantidad, string monedaIso)
        {
            var moneda = string.IsNullOrWhiteSpace(monedaIso) ? "eur" : monedaIso;
            var valor = cantidad / 100m;
            return valor.ToString("N2", FormatoNumeroMostrarEs) + " " + moneda.ToUpperInvariant();
        }

        public async Task<string> CrearSesionCheckoutAsync(int fkUsuario, string email, long suscripcionTipoId, string scheme, string host)
        {
            var suscripcionTipo = await _repositorioSuscripciones.ObtenerSuscripcionTipoPorIdAsync(suscripcionTipoId, EsModoTest);
            if (suscripcionTipo == null || string.IsNullOrWhiteSpace(suscripcionTipo.StripePriceId))
            {
                throw new InvalidOperationException($"No existe configuración activa de Stripe para la suscripción {suscripcionTipoId}.");
            }

            StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
            var client = new StripeClient(_stripeConfig.SecretKey);
            var service = new Stripe.Checkout.SessionService(client);

            var baseUrl = $"{scheme}://{host}";
            var successPath = string.IsNullOrWhiteSpace(_stripeConfig.CheckoutSuccessPath) ? "/suscripcion/gracias" : _stripeConfig.CheckoutSuccessPath;
            var cancelPath = string.IsNullOrWhiteSpace(_stripeConfig.CheckoutCancelPath) ? "/suscripcion/cancelada" : _stripeConfig.CheckoutCancelPath;
            var successUrl = $"{baseUrl}{successPath}?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}{cancelPath}";

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = email,
                Metadata = new Dictionary<string, string>
                {
                    ["FkUsuario"] = fkUsuario.ToString()
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["FkUsuario"] = fkUsuario.ToString()
                    }
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = suscripcionTipo.StripePriceId,
                        Quantity = 1
                    }
                ]
            };

            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.Url))
            {
                throw new InvalidOperationException("Stripe no devolvió URL de checkout.");
            }

            return session.Url;
        }

        public async Task ProcesarEventoAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (!string.IsNullOrWhiteSpace(session?.SubscriptionId))
                    {
                        await SincronizarSuscripcionDesdeStripeAsync(session.SubscriptionId, session.CustomerId, session.Metadata);
                    }
                    break;
                }
                case "customer.subscription.deleted":
                {
                    var subscriptionId = (stripeEvent.Data.Object as Subscription)?.Id;
                    if (!string.IsNullOrWhiteSpace(subscriptionId))
                    {
                        var subscription = await ObtenerSuscripcionFrescaAsync(subscriptionId);
                        await ActualizarSuscripcionTrasEliminacionEnStripeAsync(subscription);
                    }

                    break;
                }
                case "customer.subscription.created":
                case "customer.subscription.updated":
                {
                    // Se refresca desde la API en vez de usar el objeto embebido en el webhook:
                    // con esta cuenta, fijada en api_version 2020-08-27, subscription.items.data[].current_period_end
                    // (estructura introducida después) llega vacío en el payload del webhook.
                    var subscriptionId = (stripeEvent.Data.Object as Subscription)?.Id;
                    if (!string.IsNullOrWhiteSpace(subscriptionId))
                    {
                        await SincronizarSuscripcionDesdeStripeAsync(subscriptionId, null, null);
                    }

                    break;
                }
                case "invoice.paid":
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    var subscriptionId = ObtenerSubscriptionIdDeInvoice(invoice);
                    if (!string.IsNullOrWhiteSpace(subscriptionId))
                    {
                        _logger.LogInformation(
                            "Pago de factura correcto. StripeSubscriptionId: {StripeSubscriptionId}, InvoiceId: {InvoiceId}",
                            subscriptionId, invoice.Id);
                        await SincronizarSuscripcionDesdeStripeAsync(subscriptionId, invoice.CustomerId, null);
                    }

                    break;
                }
                case "invoice.payment_failed":
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    var subscriptionId = ObtenerSubscriptionIdDeInvoice(invoice);
                    if (!string.IsNullOrWhiteSpace(subscriptionId))
                    {
                        _logger.LogWarning(
                            "Fallo de pago de factura. StripeSubscriptionId: {StripeSubscriptionId}, InvoiceId: {InvoiceId}, ProximoIntento: {ProximoIntento}",
                            subscriptionId, invoice.Id, invoice.NextPaymentAttempt);
                        await SincronizarSuscripcionDesdeStripeAsync(subscriptionId, invoice.CustomerId, null);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Esta cuenta de Stripe tiene fijada api_version 2020-08-27 (no se puede subir: la comparte
        /// otra integración) y Stripe.net 51 ya no modela el campo plano <c>invoice.subscription</c>
        /// de esa versión, solo <see cref="InvoiceParent.SubscriptionDetails"/> (introducido después).
        /// Si el modelo tipado no lo resuelve, se recurre al JSON crudo que Stripe.net conserva.
        /// </summary>
        private static string ObtenerSubscriptionIdDeInvoice(Invoice invoice)
        {
            var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                return subscriptionId;
            }

            if (invoice?.RawJsonElement is { } json
                && json.TryGetProperty("subscription", out var valor)
                && valor.ValueKind == JsonValueKind.String)
            {
                return valor.GetString();
            }

            return null;
        }

        private async Task ActualizarSuscripcionTrasEliminacionEnStripeAsync(Subscription subscription)
        {
            var estado = string.IsNullOrWhiteSpace(subscription.Status) ? "canceled" : subscription.Status;
            var fechaFin = ResolverFechaFinAccesoStripe(subscription);

            await _repositorioSuscripciones.ActualizarEstadoSuscripcionAsync(subscription.Id, estado, fechaFin);
        }

        private async Task SincronizarSuscripcionDesdeStripeAsync(string subscriptionId, string customerId, Dictionary<string, string> metadata)
        {
            var subscription = await ObtenerSuscripcionFrescaAsync(subscriptionId);
            await SincronizarSuscripcionAsync(subscription, metadata, customerId);
        }

        private async Task<Subscription> ObtenerSuscripcionFrescaAsync(string subscriptionId)
        {
            var client = new StripeClient(_stripeConfig.SecretKey);
            var subscriptionService = new SubscriptionService(client);
            return await subscriptionService.GetAsync(subscriptionId);
        }

        private async Task SincronizarSuscripcionAsync(Subscription subscription, Dictionary<string, string> metadata, string customerIdFromEvent = null)
        {
            var stripePriceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
            if (string.IsNullOrWhiteSpace(stripePriceId))
            {
                _logger.LogWarning("No se pudo resolver StripePriceId para la suscripción {StripeSubscriptionId}", subscription.Id);
                return;
            }

            var suscripcionTipo = await _repositorioSuscripciones.ObtenerSuscripcionTipoPorStripePriceIdAsync(stripePriceId);
            if (suscripcionTipo == null)
            {
                _logger.LogWarning("No existe SuscripcionTipo para StripePriceId {StripePriceId}", stripePriceId);
                return;
            }

            var metadataFromSubscription = subscription.Metadata ?? [];
            var fkUsuarioString = metadataFromSubscription.TryGetValue("FkUsuario", out var valueFromSubscription)
                ? valueFromSubscription
                : metadata?.TryGetValue("FkUsuario", out var valueFromEvent) == true
                    ? valueFromEvent
                    : null;

            if (!int.TryParse(fkUsuarioString, out var fkUsuario))
            {
                _logger.LogWarning("No se pudo obtener FkUsuario en la suscripción Stripe {StripeSubscriptionId}", subscription.Id);
                return;
            }

            var fechaFin = ResolverFechaFinAccesoStripe(subscription);
            if (fechaFin.HasValue && EstadosActivos.Contains(subscription.Status))
            {
                _logger.LogInformation(
                    "Suscripción Stripe {StripeSubscriptionId}: cancelación programada o fin de acceso conocido; FechaFinUtc={FechaFinUtc}, CancelAtPeriodEnd={CancelAtPeriodEnd}",
                    subscription.Id,
                    fechaFin,
                    subscription.CancelAtPeriodEnd);
            }

            var suscripcion = new SuscripcionTarifa
            {
                FK_Usuario = fkUsuario,
                FK_SuscripcionTipo = suscripcionTipo.Id,
                ClienteIdFacturacion = string.Empty,
                StripeSubscriptionId = subscription.Id,
                StripeCustomerId = customerIdFromEvent ?? subscription.CustomerId,
                Estado = subscription.Status,
                FechaInicio = subscription.StartDate,
                FechaFin = fechaFin
            };

            await _repositorioSuscripciones.UpsertSuscripcionAsync(suscripcion);

            if (!EstadosActivos.Contains(subscription.Status))
            {
                await _repositorioSuscripciones.ActualizarEstadoSuscripcionAsync(subscription.Id, subscription.Status, fechaFin);
            }
        }

        /// <summary>
        /// Fecha en la que termina el acceso según Stripe: <c>cancel_at</c>, o fin de periodo actual si
        /// <c>cancel_at_period_end</c>, o <c>canceled_at</c> si ya se canceló.
        /// </summary>
        private static DateTime? ResolverFechaFinAccesoStripe(Subscription subscription)
        {
            if (subscription == null)
            {
                return null;
            }

            if (subscription.CancelAt.HasValue && subscription.CancelAt.Value != default)
            {
                return NormalizarUtc(subscription.CancelAt.Value);
            }

            if (subscription.CancelAtPeriodEnd)
            {
                var finPeriodoItem = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                if (finPeriodoItem.HasValue && finPeriodoItem.Value != default)
                {
                    return NormalizarUtc(finPeriodoItem.Value);
                }
            }

            if (subscription.CanceledAt.HasValue && subscription.CanceledAt.Value != default)
            {
                return NormalizarUtc(subscription.CanceledAt.Value);
            }

            // Respaldo: Stripe envía cancellation_details (p. ej. reason=cancellation_requested) aunque falle el mapeo de cancel_at
            if (!string.IsNullOrWhiteSpace(subscription.CancellationDetails?.Reason)
                && subscription.CancellationDetails.Reason.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            {
                var finRespaldo = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                if (finRespaldo.HasValue && finRespaldo.Value != default)
                {
                    return NormalizarUtc(finRespaldo.Value);
                }
            }

            return null;
        }

        private static DateTime NormalizarUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
        }
    }
}
