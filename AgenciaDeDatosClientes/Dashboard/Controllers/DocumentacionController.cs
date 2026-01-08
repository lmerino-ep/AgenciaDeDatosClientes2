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
        private readonly IServicioDocumentacionS3 _servicioDocumentacionS3;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<DocumentacionController> _logger;

        public DocumentacionController(
            IServicioDocumentacionS3 servicioDocumentacionS3,
            IServicioAuditoria servicioAuditoria,
            ILogger<DocumentacionController> logger)
        {
            _servicioDocumentacionS3 = servicioDocumentacionS3 ?? throw new ArgumentNullException(nameof(servicioDocumentacionS3));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Descarga un archivo de S3
        /// </summary>
        [HttpGet]
        [Route("/documentacion/descargar")]
        public async Task<IActionResult> Descargar([FromQuery] string key)
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

                if (string.IsNullOrEmpty(key))
                {
                    _logger.LogWarning("Key de archivo no proporcionado para descarga");
                    TempData["MensajeError"] = "Key de archivo no válido.";
                    return Redirect("/documentacion/listado");
                }

                _logger.LogInformation("Descargando documento: {Key} por usuario {UsuarioId}", key, idUsuario);

                var (stream, nombreArchivo, tipoMime) = await _servicioDocumentacionS3.DescargarArchivoAsync(idUsuario, key);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionDescargar,
                    $"Documento descargado: {nombreArchivo} (Key: {key})"
                );

                return File(stream, tipoMime, nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento {Key}", key);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionDescargar,
                    $"Error al descargar documento: {key} - {ex.Message}"
                );

                TempData["MensajeError"] = $"Error al descargar el documento: {ex.Message}";
                return Redirect("/documentacion/listado");
            }
        }
    }
}

