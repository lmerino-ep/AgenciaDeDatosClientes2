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
    public class SubirModel : PageModel
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<SubirModel> _logger;

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public SubirModel(
            IServicioGoogleDrive servicioGoogleDrive,
            IServicioAuditoria servicioAuditoria,
            ILogger<SubirModel> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (Archivo == null || Archivo.Length == 0)
                {
                    ModelState.AddModelError("Archivo", "Debe seleccionar un archivo");
                    return Page();
                }

                // Validar tamaño máximo (50 MB)
                const long tamanoMaximo = 50 * 1024 * 1024; // 50 MB
                if (Archivo.Length > tamanoMaximo)
                {
                    ModelState.AddModelError("Archivo", $"El archivo no puede superar los 50 MB. Tamaño actual: {Archivo.Length / (1024 * 1024)} MB");
                    return Page();
                }

                // Validar tipo de archivo
                if (!EsTipoPermitido(Archivo.ContentType))
                {
                    ModelState.AddModelError("Archivo", $"Tipo de archivo no permitido: {Archivo.ContentType}");
                    return Page();
                }

                // Obtener ID del usuario autenticado
                string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
                {
                    ModelState.AddModelError(string.Empty, "Error al obtener información del usuario.");
                    return Page();
                }

                // Verificar permisos
                var permisos = await _servicioGoogleDrive.VerificarPermisosAsync(idUsuario);
                if (!permisos.PuedeEscribir)
                {
                    ModelState.AddModelError(string.Empty, "No tienes permisos para subir documentos en esta carpeta.");
                    return Page();
                }

                _logger.LogInformation("Subiendo archivo para usuario {UsuarioId}: {NombreArchivo}, Tamaño: {Tamano} bytes, Tipo: {TipoMime}",
                    idUsuario, Archivo.FileName, Archivo.Length, Archivo.ContentType);

                // Subir archivo a Google Drive
                string idArchivo = await _servicioGoogleDrive.SubirArchivoAsync(
                    idUsuario,
                    Archivo.OpenReadStream(),
                    Archivo.FileName,
                    Archivo.ContentType
                );

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionSubir,
                    $"Documento subido: {Archivo.FileName} (ID: {idArchivo})"
                );

                _logger.LogInformation("Archivo subido exitosamente. ID: {IdArchivo}, Nombre: {NombreArchivo}", idArchivo, Archivo.FileName);
                TempData["MensajeExito"] = $"Archivo '{Archivo.FileName}' subido exitosamente.";
                return RedirectToPage("/Documentacion/Listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo {NombreArchivo}", Archivo?.FileName);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionSubir,
                    $"Error al subir documento: {Archivo?.FileName} - {ex.Message}"
                );

                MensajeError = $"Error al subir el archivo: {ex.Message}";
                return Page();
            }
        }

        private bool EsTipoPermitido(string tipoMime)
        {
            string[] tiposPermitidos = {
                "application/pdf", // PDF
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // XLSX
                "application/vnd.openxmlformats-officedocument.presentationml.presentation", // PPTX
                "image/png", // PNG
                "image/jpeg", // JPG
                "text/plain", // TXT
                "text/markdown" // MD
            };

            return tiposPermitidos.Contains(tipoMime);
        }
    }
}

