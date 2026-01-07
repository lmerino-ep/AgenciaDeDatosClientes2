using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Roles;
using Negocio;
using Utils.Authorization;

namespace Pages.Roles
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionRoles)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class ListadoModel : PageModel
    {
        private readonly IServicioRoles _servicioRoles;
        private readonly ILogger<ListadoModel> _logger;

        public List<ListadoRol> Roles { get; set; } = new List<ListadoRol>();
        public string MensajeError { get; set; }

        public class ListadoRol
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public int TotalUsuarios { get; set; }
        }

        public ListadoModel(
            IServicioRoles servicioRoles,
            ILogger<ListadoModel> logger)
        {
            _servicioRoles = servicioRoles ?? throw new ArgumentNullException(nameof(servicioRoles));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var todosLosRoles = await _servicioRoles.ObtenerTodosLosRolesAsync();
                
                // Obtener cantidad de usuarios por rol
                Roles = todosLosRoles.Select(r => new ListadoRol
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    TotalUsuarios = 0 // Se calculará después
                }).ToList();

                // Obtener conteo de usuarios por rol
                await CargarConteoUsuariosAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de roles");
                MensajeError = "Error al cargar el listado de roles. Por favor, intenta nuevamente.";
                return Page();
            }
        }

        private async Task CargarConteoUsuariosAsync()
        {
            try
            {
                var conteos = await _servicioRoles.ObtenerConteoUsuariosPorRolAsync();
                foreach (var rol in Roles)
                {
                    if (conteos.ContainsKey(rol.Id))
                    {
                        rol.TotalUsuarios = conteos[rol.Id];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cargar conteo de usuarios por rol");
                // Continuar sin el conteo si hay error
            }
        }
    }
}

