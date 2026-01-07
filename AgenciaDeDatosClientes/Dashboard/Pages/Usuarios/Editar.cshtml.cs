using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Usuarios;
using Modelos.DTO.Herramientas;
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

        public List<Modelos.DTO.Usuarios.UsuarioTipo> TiposUsuario { get; set; } = new List<Modelos.DTO.Usuarios.UsuarioTipo>();

        public DropdownFiltrable DropdownUsuariosHerencia { get; set; } = new DropdownFiltrable
        {
            Elementos = new Dictionary<string, List<ElementoDropdownFiltrable>>(),
            Habilitado = true,
            Placeholder = "Seleccione el usuario que heredará la información",
            Valor = ""
        };

        public string MensajeError { get; set; }

        public async Task<IActionResult> OnPostDesactivarAsync()
        {
            try
            {
                // Desactivar usuario (soft delete)
                await _servicioUsuarios.EliminarUsuarioAsync(Id);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioEliminar,
                    $"Usuario desactivado: ID {Id}"
                );

                _logger.LogInformation("Usuario {UsuarioId} desactivado exitosamente", Id);
                TempData["MensajeExito"] = "Usuario desactivado exitosamente.";
                return RedirectToPage("/usuarios/listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioEliminar,
                    $"Error al desactivar usuario ID {Id} - {ex.Message}"
                );

                MensajeError = $"Error al desactivar el usuario: {ex.Message}";
                await CargarDatosAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEliminarDefinitivamenteAsync(int idUsuarioHereditario)
        {
            try
            {
                if (idUsuarioHereditario <= 0)
                {
                    MensajeError = "Debe seleccionar un usuario que herede la información.";
                    await CargarDatosAsync();
                    return Page();
                }

                // Eliminar usuario definitivamente (hard delete)
                await _servicioUsuarios.EliminarUsuarioDefinitivamenteAsync(Id, idUsuarioHereditario);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioEliminar,
                    $"Usuario eliminado definitivamente: ID {Id}. Información heredada por usuario ID {idUsuarioHereditario}"
                );

                _logger.LogInformation("Usuario {UsuarioId} eliminado definitivamente exitosamente. Usuario hereditario: {UsuarioHereditario}", Id, idUsuarioHereditario);
                TempData["MensajeExito"] = "Usuario eliminado definitivamente. Toda su información ha sido transferida al usuario seleccionado.";
                return RedirectToPage("/usuarios/listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UsuarioId} definitivamente", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioEliminar,
                    $"Error al eliminar usuario ID {Id} definitivamente - {ex.Message}"
                );

                MensajeError = $"Error al eliminar el usuario: {ex.Message}";
                await CargarDatosAsync();
                return Page();
            }
        }

        private async Task CargarDatosAsync()
        {
            Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
            if (Usuario == null)
            {
                return;
            }

            TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();

            // Cargar usuarios para herencia (tipo 4 o 5) solo si el usuario tiene acceso total
            if (User.IsInRole("Dashboard - Acceso total"))
            {
                var usuariosHerencia = await _servicioUsuarios.ObtenerUsuariosParaHerenciaAsync(Id);
                DropdownUsuariosHerencia = new DropdownFiltrable
                {
                    Placeholder = "Seleccione el usuario que heredará la información",
                    Habilitado = true,
                    Valor = "",
                    Elementos = new Dictionary<string, List<ElementoDropdownFiltrable>>
                    {
                        [""] = usuariosHerencia.Select(u => new ElementoDropdownFiltrable
                        {
                            Texto = $"{u.Nombre} {u.Apellidos} ({u.Email})",
                            Valor = u.Id.ToString(),
                            Habilitado = true
                        }).ToList()
                    }
                };
            }

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
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await CargarDatosAsync();
                
                if (Usuario == null)
                {
                    return NotFound();
                }

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
                    Modelos.AuditoriaAccionEnum.UsuarioEditar,
                    $"Usuario editado: ID {Modelo.Id}"
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
                    Modelos.AuditoriaAccionEnum.UsuarioEditar,
                    $"Error al editar usuario ID {Modelo.Id} - {ex.Message}"
                );

                MensajeError = $"Error al actualizar el usuario: {ex.Message}";
                await CargarDatosAsync();
                return Page();
            }
        }
    }
}

