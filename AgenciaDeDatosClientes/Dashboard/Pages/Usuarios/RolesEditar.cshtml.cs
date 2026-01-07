using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Roles;
using Negocio;
using System.Text.Json;

namespace Pages.Usuarios
{
    public class RolesEditarModel : PageModel
    {
        private readonly IServicioRoles _servicioRoles;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<RolesEditarModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public UsuarioRoles Usuario { get; set; }
        public List<Modelos.DTO.Roles.Rol> TodosLosRoles { get; set; }
        public List<int> RolesAsignadosIds { get; set; }

        [BindProperty]
        public List<int> RolesSeleccionados { get; set; } = new List<int>();

        [TempData]
        public string MensajeExito { get; set; }

        [TempData]
        public string MensajeError { get; set; }

        public RolesEditarModel(
            IServicioRoles servicioRoles,
            IServicioAuditoria servicioAuditoria,
            ILogger<RolesEditarModel> logger)
        {
            _servicioRoles = servicioRoles ?? throw new ArgumentNullException(nameof(servicioRoles));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Cargando formulario de edición de roles para usuario {UsuarioId}", Id);

                Usuario = await _servicioRoles.ObtenerUsuarioConRolesAsync(Id);

                if (Usuario == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", Id);
                    return NotFound();
                }

                TodosLosRoles = await _servicioRoles.ObtenerTodosLosRolesAsync();
                // Ordenar roles por nombre
                TodosLosRoles = TodosLosRoles.OrderBy(r => r.Nombre).ToList();
                RolesAsignadosIds = Usuario.RolesAsignados.Select(r => r.Id).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de edición de roles para usuario {UsuarioId}", Id);
                MensajeError = "Error al cargar el formulario. Por favor, intenta nuevamente.";
                return RedirectToPage("/Usuarios/Roles", new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Guardando cambios de roles para usuario {UsuarioId}", Id);

                // Obtener usuario actual para auditoría
                Usuario = await _servicioRoles.ObtenerUsuarioConRolesAsync(Id);
                if (Usuario == null)
                {
                    return NotFound();
                }

                // Guardar estado anterior para auditoría
                List<int> rolesAnteriores = Usuario.RolesAsignados.Select(r => r.Id).ToList();

                // Asignar nuevos roles
                await _servicioRoles.AsignarRolesAsync(Id, RolesSeleccionados ?? new List<int>());

                // Preparar información de cambios para auditoría
                List<int> rolesAgregados = (RolesSeleccionados ?? new List<int>())
                    .Except(rolesAnteriores)
                    .ToList();

                List<int> rolesEliminados = rolesAnteriores
                    .Except(RolesSeleccionados ?? new List<int>())
                    .ToList();

                string cambiosRealizados = JsonSerializer.Serialize(new
                {
                    antes = rolesAnteriores,
                    despues = RolesSeleccionados ?? new List<int>(),
                    agregados = rolesAgregados,
                    eliminados = rolesEliminados
                });

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.RolAsignar,
                    $"Modificación de roles del usuario {Usuario.Email}. " +
                    $"Agregados: {rolesAgregados.Count}, Eliminados: {rolesEliminados.Count}. Cambios: {cambiosRealizados}"
                );

                _logger.LogInformation("Roles actualizados exitosamente para usuario {UsuarioId}. " +
                    "Agregados: {RolesAgregados}, Eliminados: {RolesEliminados}",
                    Id, rolesAgregados.Count, rolesEliminados.Count);

                MensajeExito = $"Roles actualizados correctamente. Se agregaron {rolesAgregados.Count} y se eliminaron {rolesEliminados.Count} roles.";
                return RedirectToPage("/Usuarios/Roles", new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar cambios de roles para usuario {UsuarioId}", Id);

                // Registrar error en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.RolAsignar,
                    $"Error al modificar roles del usuario ID {Id} - {ex.Message}"
                );

                MensajeError = "Error al guardar los cambios. Por favor, intenta nuevamente.";

                // Recargar datos para mostrar el formulario nuevamente
                await OnGetAsync();
                return Page();
            }
        }
    }
}
