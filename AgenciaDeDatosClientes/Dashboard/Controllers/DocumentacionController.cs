using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio;
using System.Security.Claims;

namespace Controllers
{
    /// <summary>
    /// Controlador para operaciones de documentación (endpoints que no requieren página)
    /// </summary>
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionDocumentacion)]
    public class DocumentacionController : Controller
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<DocumentacionController> _logger;

        public DocumentacionController(
            IServicioGoogleDrive servicioGoogleDrive,
            IServicioAuditoria servicioAuditoria,
            ILogger<DocumentacionController> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Descarga un archivo de Google Drive
        /// </summary>
        [HttpGet]
        [Route("/documentacion/descargar/{id}")]
        public async Task<IActionResult> Descargar(string id)
        {
            try
            {
                // Obtener ID del usuario autenticado
                string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
                {
                    _logger.LogWarning("No se pudo obtener el ID del usuario autenticado");
                    TempData["MensajeError"] = "Error al obtener información del usuario.";
                    return Redirect("/documentacion/listado");
                }

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("ID de archivo no proporcionado para descarga");
                    TempData["MensajeError"] = "ID de archivo no válido.";
                    return Redirect("/documentacion/listado");
                }

                _logger.LogInformation("Descargando documento: {IdArchivo} por usuario {UsuarioId}", id, idUsuario);

                var (stream, nombreArchivo, tipoMime) = await _servicioGoogleDrive.DescargarArchivoAsync(idUsuario, id);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionDescargar,
                    $"Documento descargado: {nombreArchivo} (ID: {id})"
                );

                return File(stream, tipoMime, nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {IdArchivo}", id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionDescargar,
                    $"Error al descargar documento: {id} - {ex.Message}"
                );

                TempData["MensajeError"] = $"Error al descargar el documento: {ex.Message}";
                return Redirect("/documentacion/listado");
            }
        }
    }
}

