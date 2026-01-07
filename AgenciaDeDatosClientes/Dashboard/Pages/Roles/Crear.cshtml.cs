using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos;
using Negocio;
using Utils.Authorization;

namespace Pages.Roles
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionRoles)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class CrearModel : PageModel
    {
        private readonly IServicioRoles _servicioRoles;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<CrearModel> _logger;

        [BindProperty]
        public string Nombre { get; set; }

        [BindProperty]
        public string Descripcion { get; set; }

        [TempData]
        public string MensajeExito { get; set; }

        public string MensajeError { get; set; }

        public CrearModel(
            IServicioRoles servicioRoles,
            IServicioAuditoria servicioAuditoria,
            ILogger<CrearModel> logger)
        {
            _servicioRoles = servicioRoles ?? throw new ArgumentNullException(nameof(servicioRoles));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeError = "El nombre del rol es requerido.";
                return Page();
            }

            try
            {
                // Verificar si ya existe un rol con ese nombre
                bool existe = await _servicioRoles.ExisteRolConNombreAsync(Nombre);
                if (existe)
                {
                    MensajeError = $"Ya existe un rol con el nombre '{Nombre}'.";
                    return Page();
                }

                // Crear el rol
                int idRol = await _servicioRoles.CrearRolAsync(Nombre, Descripcion ?? string.Empty);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.TiposAccionAuditoria.RolesCrear,
                    Modelos.ModulosDashboard.Roles,
                    $"Rol creado: {Nombre}",
                    "Exito"
                );

                _logger.LogInformation("Rol creado exitosamente: {Nombre} (ID: {RolId})", Nombre, idRol);
                MensajeExito = $"Rol '{Nombre}' creado exitosamente.";
                
                return RedirectToPage("/roles/listado");
            }
            catch (InvalidOperationException ex)
            {
                MensajeError = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol: {Nombre}", Nombre);
                
                // Registrar error en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.TiposAccionAuditoria.RolesCrear,
                    Modelos.ModulosDashboard.Roles,
                    $"Error al crear rol: {Nombre}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al crear el rol. Por favor, intenta nuevamente.";
                return Page();
            }
        }
    }
}

