using Negocio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Roles;

namespace Pages.Usuarios
{
    public class RolesModel : PageModel
    {
        private readonly IServicioRoles _servicioRoles;
        private readonly ILogger<RolesModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public UsuarioRoles Usuario { get; set; }

        public RolesModel(
            IServicioRoles servicioRoles,
            ILogger<RolesModel> logger)
        {
            _servicioRoles = servicioRoles ?? throw new ArgumentNullException(nameof(servicioRoles));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Consultando roles del usuario {UsuarioId}", Id);

                Usuario = await _servicioRoles.ObtenerUsuarioConRolesAsync(Id);

                if (Usuario == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", Id);
                    return NotFound();
                }

                // Ordenar roles por nombre
                if (Usuario.RolesAsignados != null)
                {
                    Usuario.RolesAsignados = Usuario.RolesAsignados.OrderBy(r => r.Nombre).ToList();
                }
                if (Usuario.RolesDisponibles != null)
                {
                    Usuario.RolesDisponibles = Usuario.RolesDisponibles.OrderBy(r => r.Nombre).ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar roles del usuario {UsuarioId}", Id);
                TempData["Error"] = "Error al cargar los roles del usuario. Por favor, intenta nuevamente.";
                return RedirectToPage("/Usuarios/Listado");
            }
        }
    }
}
