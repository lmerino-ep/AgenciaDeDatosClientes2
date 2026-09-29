using Modelos.DTO.ResultadosEmpresariales;

namespace Datos
{
    public interface IRepositorioComprasResultados
    {
        Task<IReadOnlyList<SectorCatalogoItem>> ObtenerSectoresAsync();

        Task<(int Total, IReadOnlyList<CatalogoResultadoItem> Items)> ObtenerCatalogoAsync(int fkUsuario, int pagina, int tamPagina, FiltroCatalogoResultados filtro);

        Task<IReadOnlyList<DocumentoDeProducto>> ObtenerDocumentosDeProductoAsync(int resultadoId);

        Task<ResultadoParaCompra?> ObtenerResultadoParaCompraAsync(int resultadoId, int fkUsuario);

        Task<bool> YaCompradoAsync(int fkUsuario, int resultadoId);

        Task RegistrarCompraPendienteAsync(int fkUsuario, int resultadoId, string checkoutId);

        Task ConfirmarCompraAsync(
            int fkUsuario,
            int resultadoId,
            string identificadorPedido,
            int? numeroPedido,
            int? importeCentimos,
            string moneda,
            string emailComprador,
            bool esTest,
            DateTime fechaCompra);

        Task MarcarReembolsadaAsync(string identificadorPedido);

        // Marca 'Fallida' una compra 'Pendiente' (pago rechazado o checkout expirado); no toca una ya 'Pagada'.
        Task MarcarFallidaAsync(int fkUsuario, int resultadoId, string checkoutId);

        /// <summary>
        /// Registra el evento de webhook para deduplicación. Devuelve false si el evento ya se había procesado.
        /// </summary>
        Task<bool> RegistrarEventoWebhookAsync(string nombreEvento, string identificadorPedido);
    }
}
