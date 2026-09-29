using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Modelos.DTO.ResultadosEmpresariales;
using Modelos.InyeccionDeDependencias;

namespace Negocio.PagosResultados
{
    public class PasarelaPagoLemonSqueezy : IPasarelaPagoResultados
    {
        private readonly LemonSqueezy _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PasarelaPagoLemonSqueezy> _logger;

        public PasarelaPagoLemonSqueezy(
            LemonSqueezy config,
            IHttpClientFactory httpClientFactory,
            ILogger<PasarelaPagoLemonSqueezy> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string RutaFormularioCheckout => "/lemonsqueezy/checkout";

        public async Task<(string CheckoutId, string CheckoutUrl)> CrearCheckoutAsync(
            ResultadoParaCompra resultado,
            int fkUsuario,
            string email,
            string scheme,
            string host)
        {
            var baseUrl = $"{scheme}://{host}";
            var successPath = string.IsNullOrWhiteSpace(_config.CheckoutSuccessPath) ? "/clientes/resultados-empresariales/gracias" : _config.CheckoutSuccessPath;

            var body = new JsonObject
            {
                ["data"] = new JsonObject
                {
                    ["type"] = "checkouts",
                    ["attributes"] = new JsonObject
                    {
                        ["custom_price"] = resultado.PrecioCentimos,
                        ["product_options"] = new JsonObject
                        {
                            ["name"] = resultado.Titulo,
                            ["description"] = "Descarga de resultados empresariales",
                            ["redirect_url"] = $"{baseUrl}{successPath}?resultadoId={resultado.ResultadoId}",
                            ["enabled_variants"] = new JsonArray(long.Parse(_config.VariantId))
                        },
                        ["checkout_data"] = new JsonObject
                        {
                            ["email"] = email,
                            ["custom"] = new JsonObject
                            {
                                ["fk_usuario"] = fkUsuario.ToString(),
                                ["resultado_id"] = resultado.ResultadoId.ToString()
                            }
                        },
                        ["expires_at"] = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
                    },
                    ["relationships"] = new JsonObject
                    {
                        ["store"] = new JsonObject
                        {
                            ["data"] = new JsonObject { ["type"] = "stores", ["id"] = _config.StoreId }
                        },
                        ["variant"] = new JsonObject
                        {
                            ["data"] = new JsonObject { ["type"] = "variants", ["id"] = _config.VariantId }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.lemonsqueezy.com/v1/checkouts")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/vnd.api+json")
            };

            var client = _httpClientFactory.CreateClient("LemonSqueezy");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error creando checkout de LemonSqueezy para resultado {ResultadoId}: {StatusCode} {Body}", resultado.ResultadoId, response.StatusCode, responseBody);
                throw new InvalidOperationException("No se pudo crear el checkout de pago.");
            }

            var responseJson = JsonNode.Parse(responseBody);
            var checkoutId = responseJson?["data"]?["id"]?.GetValue<string>();
            var checkoutUrl = responseJson?["data"]?["attributes"]?["url"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new InvalidOperationException("LemonSqueezy no devolvió URL de checkout.");
            }

            return (checkoutId, checkoutUrl);
        }

        // HMAC-SHA256 manual: LemonSqueezy no tiene SDK .NET con verificación de firma integrada.
        public static bool FirmaValida(string signingSecret, string rawBody, string firmaHexRecibida)
        {
            if (string.IsNullOrEmpty(firmaHexRecibida))
            {
                return false;
            }

            var clave = Encoding.UTF8.GetBytes(signingSecret ?? string.Empty);
            var cuerpo = Encoding.UTF8.GetBytes(rawBody);

            using var hmac = new HMACSHA256(clave);
            var hash = hmac.ComputeHash(cuerpo);
            var esperadoHex = Convert.ToHexStringLower(hash);

            var a = Encoding.UTF8.GetBytes(esperadoHex);
            var b = Encoding.UTF8.GetBytes(firmaHexRecibida.Trim());

            if (a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
