using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.ResultadosEmpresariales;
using Negocio;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Pages.Clientes
{
    [Authorize]
    public class ResultadosEmpresarialesModel : PageModel
    {
        private readonly IServicioComprasResultados _servicioComprasResultados;

        public const int TamPagina = 20;

        public IReadOnlyList<CatalogoResultadoItem> Catalogo { get; private set; } = [];

        public IReadOnlyList<SectorCatalogoItem> Sectores { get; private set; } = [];

        public int PaginaActual { get; private set; } = 1;

        public int TotalPaginas { get; private set; } = 1;

        public int TotalResultados { get; private set; }

        public string RutaFormularioCheckout => _servicioComprasResultados.RutaFormularioCheckout;

        [BindProperty(SupportsGet = true)]
        public string? TextoBusqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FkSector { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? FechaDesde { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrdenCatalogoResultados Orden { get; set; } = OrdenCatalogoResultados.EmpresaAscendente;

        public ResultadosEmpresarialesModel(IServicioComprasResultados servicioComprasResultados)
        {
            _servicioComprasResultados = servicioComprasResultados;
        }

        public Dictionary<string, string?> FiltrosDeRuta(int pagina) => new()
        {
            ["pagina"] = pagina.ToString(),
            ["TextoBusqueda"] = TextoBusqueda,
            ["FkSector"] = FkSector?.ToString(),
            ["FechaDesde"] = FechaDesde?.ToString("yyyy-MM-dd"),
            ["FechaHasta"] = FechaHasta?.ToString("yyyy-MM-dd"),
            ["Orden"] = Orden.ToString()
        };

        public async Task OnGetAsync(int pagina = 1)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var fkUsuario))
            {
                return;
            }

            PaginaActual = pagina < 1 ? 1 : pagina;

            Sectores = await _servicioComprasResultados.ObtenerSectoresAsync();

            var filtro = new FiltroCatalogoResultados
            {
                TextoBusqueda = string.IsNullOrWhiteSpace(TextoBusqueda) ? null : TextoBusqueda.Trim(),
                FkSector = FkSector,
                FechaDesde = FechaDesde,
                FechaHasta = FechaHasta,
                Orden = Orden
            };

            var (total, items) = await _servicioComprasResultados.ObtenerCatalogoAsync(fkUsuario, PaginaActual, TamPagina, filtro);
            Catalogo = items;
            TotalResultados = total;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (decimal)TamPagina));
        }
    }
}
