using Modelos.DTO.ResultadosEmpresariales;

namespace Negocio
{
    public interface IServicioComprasResultados
    {
        // Ruta POST (sin resultadoId) del proveedor de pago activo.
        string RutaFormularioCheckout { get; }

        Task<IReadOnlyList<SectorCatalogoItem>> ObtenerSectoresAsync();

        Task<(int Total, IReadOnlyList<CatalogoResultadoItem> Items)> ObtenerCatalogoAsync(int fkUsuario, int pagina, int tamPagina, FiltroCatalogoResultados filtro);

        Task<string> CrearCheckoutAsync(int fkUsuario, string email, int resultadoId, string scheme, string host);

        Task<bool> YaCompradoAsync(int fkUsuario, int resultadoId);

        Task<IReadOnlyList<DocumentoDeProducto>> ObtenerDocumentosDeProductoAsync(int resultadoId);

        Task<ResultadoParaCompra?> ObtenerDetalleProductoAsync(int resultadoId, int fkUsuario);

        Task ProcesarWebhookAsync(string rawBody, string signatureHeader);
    }
}
