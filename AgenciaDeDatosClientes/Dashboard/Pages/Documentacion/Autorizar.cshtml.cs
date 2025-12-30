using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;

namespace Pages.Documentacion
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionDocumentacion)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class AutorizarModel : PageModel
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly ILogger<AutorizarModel> _logger;

        public AutorizarModel(
            IServicioGoogleDrive servicioGoogleDrive,
            ILogger<AutorizarModel> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult OnGet()
        {
            try
            {
                // Obtener ID del usuario autenticado
                string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
                {
                    _logger.LogWarning("No se pudo obtener el ID del usuario autenticado");
                    TempData["MensajeError"] = "Error al obtener información del usuario.";
                    return RedirectToPage("/Documentacion/Listado");
                }

                // Generar state para seguridad (puede incluir el ID del usuario)
                string state = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("GoogleDriveOAuthState", state);
                HttpContext.Session.SetInt32("GoogleDriveOAuthUserId", idUsuario);

                // Construir redirect URI
                string redirectUri = $"{Request.Scheme}://{Request.Host}/documentacion/callback";

                // Obtener URL de autorización
                string urlAutorizacion = _servicioGoogleDrive.ObtenerUrlAutorizacion(redirectUri, state);

                _logger.LogInformation("Redirigiendo usuario {UsuarioId} a Google OAuth para autorizar Google Drive", idUsuario);

                return Redirect(urlAutorizacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar autorización de Google Drive");
                TempData["MensajeError"] = $"Error al iniciar la autorización: {ex.Message}";
                return RedirectToPage("/Documentacion/Listado");
            }
        }
    }
}

