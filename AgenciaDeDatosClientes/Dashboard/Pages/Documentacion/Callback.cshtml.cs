using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Pages.Documentacion
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionDocumentacion)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class CallbackModel : PageModel
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly ILogger<CallbackModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string? Code { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? State { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Error { get; set; }

        public CallbackModel(
            IServicioGoogleDrive servicioGoogleDrive,
            ILogger<CallbackModel> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Verificar si hay error
                if (!string.IsNullOrEmpty(Error))
                {
                    _logger.LogWarning("Error en callback de Google OAuth: {Error}", Error);
                    TempData["MensajeError"] = $"Error al autorizar Google Drive: {Error}";
                    return RedirectToPage("/Documentacion/Listado");
                }

                // Verificar state
                string? stateGuardado = HttpContext.Session.GetString("GoogleDriveOAuthState");
                int? idUsuarioGuardado = HttpContext.Session.GetInt32("GoogleDriveOAuthUserId");

                if (string.IsNullOrEmpty(stateGuardado) || !idUsuarioGuardado.HasValue)
                {
                    _logger.LogWarning("State no encontrado en sesión o usuario no válido");
                    TempData["MensajeError"] = "Sesión expirada. Por favor, intenta autorizar nuevamente.";
                    return RedirectToPage("/Documentacion/Listado");
                }

                if (State != stateGuardado)
                {
                    _logger.LogWarning("State no coincide. Esperado: {StateGuardado}, Recibido: {State}", stateGuardado, State);
                    TempData["MensajeError"] = "Error de seguridad: el estado no coincide.";
                    return RedirectToPage("/Documentacion/Listado");
                }

                // Verificar código
                if (string.IsNullOrEmpty(Code))
                {
                    _logger.LogWarning("Código de autorización no recibido");
                    TempData["MensajeError"] = "No se recibió el código de autorización de Google.";
                    return RedirectToPage("/Documentacion/Listado");
                }

                // Limpiar sesión
                HttpContext.Session.Remove("GoogleDriveOAuthState");
                HttpContext.Session.Remove("GoogleDriveOAuthUserId");

                // Construir redirect URI
                string redirectUri = $"{Request.Scheme}://{Request.Host}/documentacion/callback";

                // Intercambiar código por tokens
                await _servicioGoogleDrive.IntercambiarCodigoPorTokensAsync(idUsuarioGuardado.Value, Code, redirectUri);

                _logger.LogInformation("Autorización de Google Drive completada exitosamente para usuario {UsuarioId}", idUsuarioGuardado.Value);
                TempData["MensajeExito"] = "Autorización de Google Drive completada exitosamente. Ya puedes gestionar documentos.";

                return RedirectToPage("/Documentacion/Listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar callback de Google OAuth");
                TempData["MensajeError"] = $"Error al completar la autorización: {ex.Message}";
                return RedirectToPage("/Documentacion/Listado");
            }
        }
    }
}

