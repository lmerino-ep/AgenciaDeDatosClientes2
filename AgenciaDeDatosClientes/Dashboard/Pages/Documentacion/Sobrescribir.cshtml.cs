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
    public class SobrescribirModel : PageModel
    {
        private readonly IServicioDocumentacionS3 _servicioDocumentacionS3;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<SobrescribirModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string NombreBase { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CarpetaRuta { get; set; }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public string? NombreArchivoActual { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public SobrescribirModel(
            IServicioDocumentacionS3 servicioDocumentacionS3,
            IServicioAuditoria servicioAuditoria,
            ILogger<SobrescribirModel> logger)
        {
            _servicioDocumentacionS3 = servicioDocumentacionS3 ?? throw new ArgumentNullException(nameof(servicioDocumentacionS3));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(NombreBase))
                {
                    MensajeError = "Nombre de archivo no proporcionado.";
                    return Page();
                }

                NombreArchivoActual = NombreBase;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar información del archivo {NombreBase}", NombreBase);
                MensajeError = $"Error al cargar el archivo: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (Archivo == null || Archivo.Length == 0)
                {
                    ModelState.AddModelError("Archivo", "Debe seleccionar un archivo");
                    await OnGetAsync();
                    return Page();
                }

                // Validar tamaño máximo (50 MB)
                const long tamanoMaximo = 50 * 1024 * 1024; // 50 MB
                if (Archivo.Length > tamanoMaximo)
                {
                    ModelState.AddModelError("Archivo", $"El archivo no puede superar los 50 MB. Tamaño actual: {Archivo.Length / (1024 * 1024)} MB");
                    await OnGetAsync();
                    return Page();
                }

                // Obtener ID del usuario autenticado
                string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
                {
                    ModelState.AddModelError(string.Empty, "Error al obtener información del usuario.");
                    await OnGetAsync();
                    return Page();
                }

                // Verificar permisos
                var permisos = await _servicioDocumentacionS3.VerificarPermisosAsync(User);
                if (!permisos.PuedeEscribir)
                {
                    ModelState.AddModelError(string.Empty, "No tienes permisos para subir nuevas versiones de documentos.");
                    await OnGetAsync();
                    return Page();
                }

                if (string.IsNullOrEmpty(NombreBase))
                {
                    ModelState.AddModelError(string.Empty, "Nombre de archivo base no proporcionado.");
                    await OnGetAsync();
                    return Page();
                }

                _logger.LogInformation("Subiendo nueva versión para usuario {UsuarioId}: {NombreBase}, Nuevo archivo: {NombreArchivo}, Tamaño: {Tamano} bytes, Tipo: {TipoMime}, Carpeta: {CarpetaRuta}",
                    idUsuario, NombreBase, Archivo.FileName, Archivo.Length, Archivo.ContentType, CarpetaRuta ?? "raíz");

                // Subir nueva versión del archivo (incrementa automáticamente el número de versión)
                string key = await _servicioDocumentacionS3.SubirNuevaVersionAsync(
                    idUsuario,
                    NombreBase,
                    Archivo.OpenReadStream(),
                    Archivo.ContentType,
                    CarpetaRuta
                );

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionSobrescribir,
                    $"Nueva versión subida: {NombreBase} → {Archivo.FileName} (Key: {key})"
                );

                _logger.LogInformation("Nueva versión subida exitosamente. Key: {Key}, Nombre base: {NombreBase}, Nombre nuevo: {NombreNuevo}",
                    key, NombreBase, Archivo.FileName);
                TempData["MensajeExito"] = $"Nueva versión de '{NombreBase}' subida exitosamente.";
                
                // Preservar CarpetaRuta al redirigir (construir URL manualmente para preservar el case)
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
                return RedirectToPage("/Documentacion/Listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir nueva versión {NombreBase}", NombreBase);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionSobrescribir,
                    $"Error al subir nueva versión: {NombreBase} - {ex.Message}"
                );

                MensajeError = $"Error al sobrescribir el archivo: {ex.Message}";
                await OnGetAsync();
                return Page();
            }
        }
    }
}

