using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Usuarios;
using Negocio;
using Utils;

namespace Pages.Usuarios
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionUsuarios)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class CrearModel : PageModel
    {
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<CrearModel> _logger;

        public CrearModel(
            IServicioUsuarios servicioUsuarios,
            IServicioAuditoria servicioAuditoria,
            ILogger<CrearModel> logger)
        {
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public CrearUsuario Modelo { get; set; }

        public List<Modelos.Entidades.UsuarioTipo> TiposUsuario { get; set; } = new List<Modelos.Entidades.UsuarioTipo>();

        public string MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de crear usuario");
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
                    TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                    return Page();
                }

                // Verificar si el email ya existe
                if (await _servicioUsuarios.ExisteEmailAsync(Modelo.Email))
                {
                    ModelState.AddModelError("Modelo.Email", "El email ya está en uso");
                    TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                    return Page();
                }

                // Validar contraseña
                if (Modelo.Password != Modelo.ConfirmarPassword)
                {
                    ModelState.AddModelError("Modelo.ConfirmarPassword", "Las contraseñas no coinciden");
                    TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                    return Page();
                }

                // Hashear contraseña antes de enviarla al servicio (separación de capas)
                Modelo.Password = Hash.HashText(Modelo.Password);

                // Crear usuario
                int idUsuario = await _servicioUsuarios.CrearUsuarioAsync(Modelo);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Crear",
                    "Usuarios",
                    $"Usuario creado: {Modelo.Email}",
                    "Exito"
                );

                _logger.LogInformation("Usuario creado exitosamente con ID {UsuarioId}", idUsuario);
                TempData["MensajeExito"] = $"Usuario {Modelo.Email} creado exitosamente.";
                return RedirectToPage("/usuarios/listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Usuario.Crear",
                    "Usuarios",
                    $"Error al crear usuario: {Modelo.Email}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = $"Error al crear el usuario: {ex.Message}";
                TiposUsuario = await _servicioUsuarios.ObtenerTiposUsuarioAsync();
                return Page();
            }
        }
    }
}

