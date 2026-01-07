using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos;
using Modelos.DTO.Roles;
using Negocio;
using Utils.Authorization;

namespace Pages.Roles
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionRoles)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class EditarModel : PageModel
    {
        private readonly IServicioRoles _servicioRoles;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<EditarModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string Descripcion { get; set; }

        public Rol Rol { get; set; }

        [TempData]
        public string MensajeExito { get; set; }

        public string MensajeError { get; set; }

        public EditarModel(
            IServicioRoles servicioRoles,
            IServicioAuditoria servicioAuditoria,
            ILogger<EditarModel> logger)
        {
            _servicioRoles = servicioRoles ?? throw new ArgumentNullException(nameof(servicioRoles));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (Id <= 0)
                {
                    MensajeError = "ID de rol no válido.";
                    return Page();
                }

                Rol = await _servicioRoles.ObtenerRolPorIdAsync(Id);

                if (Rol == null)
                {
                    _logger.LogWarning("Rol {RolId} no encontrado", Id);
                    return NotFound();
                }

                // Cargar valores actuales en las propiedades para el formulario
                Descripcion = Rol.Descripcion;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar rol {RolId} para edición", Id);
                MensajeError = "Error al cargar el rol. Por favor, intenta nuevamente.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Recargar el rol para mostrar los datos actuales
                Rol = await _servicioRoles.ObtenerRolPorIdAsync(Id);
                return Page();
            }

            try
            {
                // Obtener datos originales para auditoría
                var rolOriginal = await _servicioRoles.ObtenerRolPorIdAsync(Id);
                if (rolOriginal == null)
                {
                    MensajeError = "El rol no existe.";
                    return Page();
                }

                // Actualizar solo la descripción del rol
                await _servicioRoles.ActualizarDescripcionRolAsync(Id, Descripcion ?? string.Empty);

                // Registrar en auditoría con cambios (solo descripción)
                var cambios = new Dictionary<string, object>
                {
                    ["Descripcion"] = new { Antes = rolOriginal.Descripcion ?? "", Despues = Descripcion ?? "" }
                };

                var auditoria = new Modelos.DTO.Auditoria
                {
                    FK_Usuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0"),
                    Accion = TiposAccionAuditoria.RolesEditar,
                    Modulo = ModulosDashboard.Roles,
                    Descripcion = $"Descripción del rol '{rolOriginal.Nombre}' actualizada",
                    EntidadAfectada = "Rol",
                    IdEntidadAfectada = Id,
                    CambiosRealizados = System.Text.Json.JsonSerializer.Serialize(cambios),
                    DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Resultado = "Exito"
                };

                await _servicioAuditoria.RegistrarAccionAsync(auditoria);

                _logger.LogInformation("Descripción del rol {RolId} ({Nombre}) actualizada exitosamente", Id, rolOriginal.Nombre);
                MensajeExito = $"Descripción del rol '{rolOriginal.Nombre}' actualizada exitosamente.";
                
                return RedirectToPage("/roles/listado");
            }
            catch (InvalidOperationException ex)
            {
                MensajeError = ex.Message;
                Rol = await _servicioRoles.ObtenerRolPorIdAsync(Id);
                return Page();
            }
            catch (Exception ex)
            {
                var rolParaError = await _servicioRoles.ObtenerRolPorIdAsync(Id);
                string nombreRol = rolParaError?.Nombre ?? "Desconocido";
                
                _logger.LogError(ex, "Error al actualizar descripción del rol {RolId}", Id);
                
                // Registrar error en auditoría
                var auditoriaError = new Modelos.DTO.Auditoria
                {
                    FK_Usuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0"),
                    Accion = TiposAccionAuditoria.RolesEditar,
                    Modulo = ModulosDashboard.Roles,
                    Descripcion = $"Error al actualizar descripción del rol: {nombreRol}",
                    EntidadAfectada = "Rol",
                    IdEntidadAfectada = Id,
                    DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Resultado = "Error",
                    MensajeError = ex.Message
                };

                await _servicioAuditoria.RegistrarAccionAsync(auditoriaError);

                MensajeError = "Error al actualizar el rol. Por favor, intenta nuevamente.";
                Rol = await _servicioRoles.ObtenerRolPorIdAsync(Id);
                return Page();
            }
        }
    }
}

