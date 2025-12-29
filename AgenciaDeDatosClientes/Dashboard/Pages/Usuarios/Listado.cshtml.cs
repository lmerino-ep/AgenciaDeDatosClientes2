using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Usuarios;
using Negocio;

namespace Pages.Usuarios
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.ConsultaUsuarios)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class ListadoModel : PageModel
    {
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly ILogger<ListadoModel> _logger;

        public ListadoModel(
            IServicioUsuarios servicioUsuarios,
            ILogger<ListadoModel> logger)
        {
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Propiedades para filtros
        [BindProperty(SupportsGet = true)]
        public int? FK_UsuarioTipo { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Activo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int TamanoPagina { get; set; } = 25;

        // Propiedades para mostrar
        public List<UsuarioListado> Usuarios { get; set; } = new List<UsuarioListado>();
        public int TotalUsuarios { get; set; }
        public int TotalPaginas { get; set; }
        public List<Modelos.Entidades.UsuarioTipo> TiposUsuario { get; set; } = new List<Modelos.Entidades.UsuarioTipo>();

        [TempData]
        public string MensajeExito { get; set; }

        [TempData]
        public string MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Obtener tipos de usuario para el dropdown
                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();

                // Obtener usuarios con filtros
                var resultado = await _servicioUsuarios.ListarUsuariosAsync(
                    FK_UsuarioTipo,
                    Activo,
                    Busqueda,
                    Pagina,
                    TamanoPagina
                );

                Usuarios = resultado.usuarios;
                TotalUsuarios = resultado.total;
                TotalPaginas = (int)Math.Ceiling((double)TotalUsuarios / TamanoPagina);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar usuarios");
                MensajeError = "Error al cargar la lista de usuarios. Por favor, intenta nuevamente.";
                return Page();
            }
        }
    }
}

