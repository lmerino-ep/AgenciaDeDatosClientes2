using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Usuarios;
using Negocio;
using System.Security.Cryptography;
using Utils;

namespace Pages.Usuarios
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionUsuarios)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class CambiarContrasenaModel : PageModel
    {
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<CambiarContrasenaModel> _logger;

        public CambiarContrasenaModel(
            IServicioUsuarios servicioUsuarios,
            IServicioAuditoria servicioAuditoria,
            ILogger<CambiarContrasenaModel> logger)
        {
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public CambiarContrasena Modelo { get; set; }

        public UsuarioDetalle Usuario { get; set; }

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

                Modelo = new CambiarContrasena
                {
                    IdUsuario = Id
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de cambiar contraseña para usuario {UsuarioId}", Id);
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
                    return Page();
                }

                // Validar que las contraseñas coinciden
                if (Modelo.NuevaPassword != Modelo.ConfirmarPassword)
                {
                    ModelState.AddModelError("Modelo.ConfirmarPassword", "Las contraseñas no coinciden");
                    Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                    return Page();
                }

                // Hashear contraseña antes de enviarla al servicio (separación de capas)
                Modelo.NuevaPassword = Hash.HashText(Modelo.NuevaPassword);

                // Cambiar contraseña
                await _servicioUsuarios.CambiarContrasenaAsync(Modelo);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioCambiarContrasena,
                    $"Contraseña cambiada para usuario ID {Id}" + (Modelo.EnviarPorEmail ? " (enviada por email)" : "")
                );

                _logger.LogInformation("Contraseña del usuario {UsuarioId} cambiada exitosamente", Id);
                TempData["MensajeExito"] = "Contraseña cambiada exitosamente." + (Modelo.EnviarPorEmail ? " Se ha enviado un email al usuario." : "");
                return RedirectToPage("/usuarios/editar", new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.UsuarioCambiarContrasena,
                    $"Error al cambiar contraseña para usuario ID {Id} - {ex.Message}"
                );

                MensajeError = $"Error al cambiar la contraseña: {ex.Message}";
                Usuario = await _servicioUsuarios.ObtenerUsuarioAsync(Id);
                return Page();
            }
        }

        /// <summary>
        /// Genera una contraseña segura automáticamente
        /// </summary>
        public IActionResult OnGetGenerarPassword()
        {
            string password = GenerarPasswordSegura();
            return new JsonResult(new { password });
        }

        /// <summary>
        /// Genera una contraseña segura de 16 caracteres
        /// </summary>
        private string GenerarPasswordSegura()
        {
            const string mayusculas = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string minusculas = "abcdefghijkmnopqrstuvwxyz";
            const string numeros = "0123456789";
            const string simbolos = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            using (var rng = RandomNumberGenerator.Create())
            {
                var password = new char[16];
                var buffer = new byte[1];

                // Asegurar al menos un carácter de cada tipo
                password[0] = mayusculas[GetRandomByte(rng) % mayusculas.Length];
                password[1] = minusculas[GetRandomByte(rng) % minusculas.Length];
                password[2] = numeros[GetRandomByte(rng) % numeros.Length];
                password[3] = simbolos[GetRandomByte(rng) % simbolos.Length];

                // Llenar el resto con caracteres aleatorios
                string todosCaracteres = mayusculas + minusculas + numeros + simbolos;
                for (int i = 4; i < 16; i++)
                {
                    password[i] = todosCaracteres[GetRandomByte(rng) % todosCaracteres.Length];
                }

                // Mezclar los caracteres
                for (int i = 0; i < 16; i++)
                {
                    int j = GetRandomByte(rng) % 16;
                    (password[i], password[j]) = (password[j], password[i]);
                }

                return new string(password);
            }
        }

        private int GetRandomByte(RandomNumberGenerator rng)
        {
            var buffer = new byte[1];
            rng.GetBytes(buffer);
            return buffer[0];
        }
    }
}

