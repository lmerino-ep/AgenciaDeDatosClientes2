using Modelos.DTO.ResultadosEmpresariales;

namespace Negocio.PagosResultados
{
    // Permite alternar Stripe/LemonSqueezy por configuración sin reescribir el flujo de compra.
    public interface IPasarelaPagoResultados
    {
        // Ruta POST (sin resultadoId) del proveedor, para que las vistas no fijen el proveedor en el HTML.
        string RutaFormularioCheckout { get; }

        Task<(string CheckoutId, string CheckoutUrl)> CrearCheckoutAsync(
            ResultadoParaCompra resultado,
            int fkUsuario,
            string email,
            string scheme,
            string host);
    }
}
