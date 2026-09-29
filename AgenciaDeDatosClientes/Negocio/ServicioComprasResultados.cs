using System;
using System.Text.Json.Nodes;
using Datos;
using Microsoft.Extensions.Logging;
using Modelos.DTO.ResultadosEmpresariales;
using Modelos.InyeccionDeDependencias;
using Negocio.PagosResultados;

namespace Negocio
{
    public class ServicioComprasResultados : IServicioComprasResultados
    {
        private const string EstadoPedidoPagado = "paid";
        private const string EventoPedidoCreado = "order_created";
        private const string EventoPedidoReembolsado = "order_refunded";

        private readonly IRepositorioComprasResultados _repositorio;
        private readonly IPasarelaPagoResultados _pasarela;
        private readonly LemonSqueezy _lemonSqueezyConfig;
        private readonly ILogger<ServicioComprasResultados> _logger;

        public ServicioComprasResultados(
            IRepositorioComprasResultados repositorio,
            IPasarelaPagoResultados pasarela,
            LemonSqueezy lemonSqueezyConfig,
            ILogger<ServicioComprasResultados> logger)
        {
            _repositorio = repositorio;
            _pasarela = pasarela;
            _lemonSqueezyConfig = lemonSqueezyConfig;
            _logger = logger;
        }

        public string RutaFormularioCheckout => _pasarela.RutaFormularioCheckout;

        public async Task<IReadOnlyList<SectorCatalogoItem>> ObtenerSectoresAsync()
        {
            return await _repositorio.ObtenerSectoresAsync();
        }

        public async Task<(int Total, IReadOnlyList<CatalogoResultadoItem> Items)> ObtenerCatalogoAsync(int fkUsuario, int pagina, int tamPagina, FiltroCatalogoResultados filtro)
        {
            return await _repositorio.ObtenerCatalogoAsync(fkUsuario, pagina, tamPagina, filtro);
        }

        public async Task<bool> YaCompradoAsync(int fkUsuario, int resultadoId)
        {
            return await _repositorio.YaCompradoAsync(fkUsuario, resultadoId);
        }

        public async Task<IReadOnlyList<DocumentoDeProducto>> ObtenerDocumentosDeProductoAsync(int resultadoId)
        {
            return await _repositorio.ObtenerDocumentosDeProductoAsync(resultadoId);
        }

        public async Task<ResultadoParaCompra?> ObtenerDetalleProductoAsync(int resultadoId, int fkUsuario)
        {
            return await _repositorio.ObtenerResultadoParaCompraAsync(resultadoId, fkUsuario);
        }

        public async Task<string> CrearCheckoutAsync(int fkUsuario, string email, int resultadoId, string scheme, string host)
        {
            var resultado = await _repositorio.ObtenerResultadoParaCompraAsync(resultadoId, fkUsuario);
            if (resultado == null || !resultado.Publicado)
            {
                throw new InvalidOperationException($"El resultado {resultadoId} no está disponible para la venta.");
            }

            var (checkoutId, checkoutUrl) = await _pasarela.CrearCheckoutAsync(resultado, fkUsuario, email, scheme, host);

            await _repositorio.RegistrarCompraPendienteAsync(fkUsuario, resultadoId, checkoutId);

            return checkoutUrl;
        }

        // Webhook de LemonSqueezy: solo lo invoca LemonSqueezyController, no está detrás de IPasarelaPagoResultados.
        public async Task ProcesarWebhookAsync(string rawBody, string signatureHeader)
        {
            if (!PasarelaPagoLemonSqueezy.FirmaValida(_lemonSqueezyConfig.SigningSecret, rawBody, signatureHeader))
            {
                throw new FirmaWebhookInvalidaException("Firma de webhook de LemonSqueezy inválida.");
            }

            var json = JsonNode.Parse(rawBody);
            var nombreEvento = json?["meta"]?["event_name"]?.GetValue<string>();
            var atributos = json?["data"]?["attributes"];
            var identificadorPedido = atributos?["identifier"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(nombreEvento) || string.IsNullOrWhiteSpace(identificadorPedido))
            {
                _logger.LogWarning("Webhook de LemonSqueezy sin evento o identificador de pedido.");
                return;
            }

            if (!await _repositorio.RegistrarEventoWebhookAsync(nombreEvento, identificadorPedido))
            {
                _logger.LogInformation("Evento LemonSqueezy ya procesado: {NombreEvento}/{IdentificadorPedido}", nombreEvento, identificadorPedido);
                return;
            }

            if (atributos?["test_mode"]?.GetValue<bool>() == true)
            {
                _logger.LogWarning("Webhook de LemonSqueezy en test_mode recibido: {NombreEvento}/{IdentificadorPedido}", nombreEvento, identificadorPedido);
            }

            switch (nombreEvento)
            {
                case EventoPedidoCreado:
                    await ProcesarPedidoCreadoAsync(json, atributos, identificadorPedido);
                    break;
                case EventoPedidoReembolsado:
                    await _repositorio.MarcarReembolsadaAsync(identificadorPedido);
                    break;
            }
        }

        private async Task ProcesarPedidoCreadoAsync(JsonNode json, JsonNode atributos, string identificadorPedido)
        {
            var estado = atributos?["status"]?.GetValue<string>();
            if (!string.Equals(estado, EstadoPedidoPagado, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var customData = json?["meta"]?["custom_data"];
            var fkUsuarioTexto = customData?["fk_usuario"]?.ToString();
            var resultadoIdTexto = customData?["resultado_id"]?.ToString();
            if (!int.TryParse(fkUsuarioTexto, out var fkUsuario) || !int.TryParse(resultadoIdTexto, out var resultadoId))
            {
                _logger.LogWarning("No se pudo resolver fk_usuario/resultado_id en el pedido {IdentificadorPedido}", identificadorPedido);
                return;
            }

            var numeroPedido = atributos?["order_number"]?.GetValue<int>();
            var importeCentimos = atributos?["total"]?.GetValue<int>();
            var moneda = atributos?["currency"]?.GetValue<string>();
            var email = atributos?["user_email"]?.GetValue<string>();
            var esTest = atributos?["test_mode"]?.GetValue<bool>() ?? false;

            await _repositorio.ConfirmarCompraAsync(
                fkUsuario,
                resultadoId,
                identificadorPedido,
                numeroPedido,
                importeCentimos,
                moneda,
                email,
                esTest,
                DateTime.UtcNow);
        }
    }
}
