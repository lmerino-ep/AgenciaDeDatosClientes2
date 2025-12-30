using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;

namespace Pages.Documentacion
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionDocumentacion)]
    public class DescargarModel : PageModel
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<DescargarModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        public DescargarModel(
            IServicioGoogleDrive servicioGoogleDrive,
            IServicioAuditoria servicioAuditoria,
            ILogger<DescargarModel> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
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

                _logger.LogInformation("Descargando documento: {IdArchivo} por usuario {UsuarioId}", Id, idUsuario);

                var (archivo, nombreArchivo, tipoMime) = await _servicioGoogleDrive.DescargarArchivoAsync(idUsuario, Id);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Documentacion.Descargar",
                    "Documentacion",
                    $"Documento descargado: {nombreArchivo} (ID: {Id})",
                    "Exito"
                );

                return File(archivo, tipoMime, nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {IdArchivo}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Documentacion.Descargar",
                    "Documentacion",
                    $"Error al descargar documento: {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                TempData["MensajeError"] = $"Error al descargar el documento: {ex.Message}";
                return RedirectToPage("/Documentacion/Listado");
            }
        }
    }
}

