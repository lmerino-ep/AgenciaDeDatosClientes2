using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Usuarios;
using Negocio;

namespace Pages.Usuarios
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionUsuarios)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class EditarModel : PageModel
    {
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<EditarModel> _logger;

        public EditarModel(
            IServicioUsuarios servicioUsuarios,
            IServicioAuditoria servicioAuditoria,
            ILogger<EditarModel> logger)
        {
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public EditarUsuario Modelo { get; set; }

        public UsuarioDetalle Usuario { get; set; }

        public List<Modelos.Entidades.UsuarioTipo> TiposUsuario { get; set; } = new List<Modelos.Entidades.UsuarioTipo>();

        public string MensajeError { get; set; }

        public async Task<IActionResult> OnPostEliminarAsync()
        {
            try
            {
                // Eliminar usuario (soft delete)
                await _servicioUsuarios.EliminarUsuarioAsync(Id);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Eliminar",
                    "Usuarios",
                    $"Usuario eliminado (soft delete): ID {Id}",
                    "Exito"
                );

                _logger.LogInformation("Usuario {UsuarioId} eliminado exitosamente", Id);
                TempData["MensajeExito"] = "Usuario eliminado exitosamente.";
                return RedirectToPage("/usuarios/listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Eliminar",
                    "Usuarios",
                    $"Error al eliminar usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = $"Error al eliminar el usuario: {ex.Message}";
                Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                if (Usuario == null)
                {
                    return NotFound();
                }

                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();

                Modelo = new EditarUsuario
                {
                    Id = Usuario.Id,
                    Email = Usuario.Email,
                    Nombre = Usuario.Nombre,
                    Apellidos = Usuario.Apellidos,
                    Empresa = Usuario.Empresa,
                    FK_UsuarioTipo = Usuario.FK_UsuarioTipo,
                    Activo = Usuario.Activo
                };

                return Page();
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                if (Usuario == null)
                {
                    return NotFound();
                }

                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();

                Modelo = new EditarUsuario
                {
                    Id = Usuario.Id,
                    Email = Usuario.Email,
                    Nombre = Usuario.Nombre,
                    Apellidos = Usuario.Apellidos,
                    Empresa = Usuario.Empresa,
                    FK_UsuarioTipo = Usuario.FK_UsuarioTipo,
                    Activo = Usuario.Activo
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de editar usuario {UsuarioId}", Id);
                MensajeError = "Error al cargar la página. Por favor, intenta nuevamente.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Modelo.Id);
                    TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                    return Page();
                }

                // Actualizar usuario
                await _servicioUsuarios.ActualizarUsuarioAsync(Modelo);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Editar",
                    "Usuarios",
                    $"Usuario editado: ID {Modelo.Id}",
                    "Exito"
                );

                _logger.LogInformation("Usuario {UsuarioId} actualizado exitosamente", Modelo.Id);
                TempData["MensajeExito"] = "Usuario actualizado exitosamente.";
                return RedirectToPage("/usuarios/listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UsuarioId}", Modelo.Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Editar",
                    "Usuarios",
                    $"Error al editar usuario ID {Modelo.Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = $"Error al actualizar el usuario: {ex.Message}";
                Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Modelo.Id);
                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                return Page();
            }
        }
    }
}

