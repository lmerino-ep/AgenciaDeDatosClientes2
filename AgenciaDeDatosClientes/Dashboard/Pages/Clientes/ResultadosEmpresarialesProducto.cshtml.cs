using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.ResultadosEmpresariales;
using Negocio;
using System.Security.Claims;

namespace Pages.Clientes
{
    [Authorize]
    public class ResultadosEmpresarialesProductoModel : PageModel
    {
        private const string RUTA_DESCARGAS_EPDATA = "/mi-cuenta/mis-resultados-empresariales";

        private readonly IServicioComprasResultados _servicioComprasResultados;
        private readonly Utils.Autologin.CreadorEnlaceAutologin _creadorEnlaceAutologin;

        public ResultadoParaCompra? Producto { get; private set; }

        public IReadOnlyList<DocumentoDeProducto> Documentos { get; private set; } = [];

        public string RutaFormularioCheckout => _servicioComprasResultados.RutaFormularioCheckout;

        public string UrlVerMisResultadosEpData { get; private set; } = RUTA_DESCARGAS_EPDATA;

        public ResultadosEmpresarialesProductoModel(IServicioComprasResultados servicioComprasResultados, Utils.Autologin.CreadorEnlaceAutologin creadorEnlaceAutologin)
        {
            _servicioComprasResultados = servicioComprasResultados;
            _creadorEnlaceAutologin = creadorEnlaceAutologin;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var fkUsuario))
            {
                return Unauthorized();
            }

            Producto = await _servicioComprasResultados.ObtenerDetalleProductoAsync(id, fkUsuario);
            if (Producto == null || !Producto.Publicado)
            {
                return NotFound();
            }

            Documentos = await _servicioComprasResultados.ObtenerDocumentosDeProductoAsync(id);
            UrlVerMisResultadosEpData = _creadorEnlaceAutologin.ObtenerUrlAutologinHaciaEpData(fkUsuario, RUTA_DESCARGAS_EPDATA);

            return Page();
        }
    }
}
