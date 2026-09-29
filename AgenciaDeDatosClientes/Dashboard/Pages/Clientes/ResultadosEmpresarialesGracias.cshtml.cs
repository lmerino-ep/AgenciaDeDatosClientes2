using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;

namespace Pages.Clientes
{
    [Authorize]
    public class ResultadosEmpresarialesGraciasModel : PageModel
    {
        private const string RUTA_DESCARGAS_EPDATA = "/mi-cuenta/mis-resultados-empresariales";

        private readonly IServicioComprasResultados _servicioComprasResultados;
        private readonly Utils.Autologin.CreadorEnlaceAutologin _creadorEnlaceAutologin;

        public bool CompraConfirmada { get; private set; }

        public string UrlDescargasEpData { get; private set; } = RUTA_DESCARGAS_EPDATA;

        public ResultadosEmpresarialesGraciasModel(IServicioComprasResultados servicioComprasResultados, Utils.Autologin.CreadorEnlaceAutologin creadorEnlaceAutologin)
        {
            _servicioComprasResultados = servicioComprasResultados;
            _creadorEnlaceAutologin = creadorEnlaceAutologin;
        }

        public async Task OnGetAsync(int resultadoId)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var fkUsuario))
            {
                return;
            }

            CompraConfirmada = await _servicioComprasResultados.YaCompradoAsync(fkUsuario, resultadoId);
            UrlDescargasEpData = _creadorEnlaceAutologin.ObtenerUrlAutologinHaciaEpData(fkUsuario, RUTA_DESCARGAS_EPDATA);
        }
    }
}
