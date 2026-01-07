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
    public class CambiarTipoModel : PageModel
    {
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<CambiarTipoModel> _logger;

        public CambiarTipoModel(
            IServicioUsuarios servicioUsuarios,
            IServicioAuditoria servicioAuditoria,
            ILogger<CambiarTipoModel> logger)
        {
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public CambiarTipoUsuario Modelo { get; set; }

        public UsuarioDetalle Usuario { get; set; }

        public List<Modelos.DTO.Usuarios.UsuarioTipo> TiposUsuario { get; set; } = new List<Modelos.DTO.Usuarios.UsuarioTipo>();

        public string MensajeError { get; set; }

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

                Modelo = new CambiarTipoUsuario
                {
                    IdUsuario = Id,
                    FK_UsuarioTipo = Usuario.FK_UsuarioTipo
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de cambiar tipo de usuario {UsuarioId}", Id);
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
                    Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                    TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                    return Page();
                }

                // Cambiar tipo de usuario
                await _servicioUsuarios.CambiarTipoUsuarioAsync(Modelo);

                // Registrar en auditoría
                var tipoNombre = TiposUsuario.FirstOrDefault(t => t.Id == Modelo.FK_UsuarioTipo)?.Nombre ?? "Desconocido";
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioCambiarTipo,
                    $"Tipo de usuario cambiado a {tipoNombre} para usuario ID {Id}"
                );

                _logger.LogInformation("Tipo de usuario {UsuarioId} cambiado exitosamente", Id);
                TempData["MensajeExito"] = "Tipo de usuario cambiado exitosamente.";
                return RedirectToPage("/usuarios/editar", new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar tipo de usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioCambiarTipo,
                    $"Error al cambiar tipo de usuario ID {Id} - {ex.Message}"
                );

                MensajeError = $"Error al cambiar el tipo de usuario: {ex.Message}";
                Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                return Page();
            }
        }
    }
}

