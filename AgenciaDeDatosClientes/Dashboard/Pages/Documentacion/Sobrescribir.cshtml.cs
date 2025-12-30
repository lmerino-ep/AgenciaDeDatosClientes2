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
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<SobrescribirModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        [BindProperty]
        public IFormFile? Archivo { get; set; }

        public string? NombreArchivoActual { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public SobrescribirModel(
            IServicioGoogleDrive servicioGoogleDrive,
            IServicioAuditoria servicioAuditoria,
            ILogger<SobrescribirModel> logger)
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
                    MensajeError = "Error al obtener información del usuario.";
                    return Page();
                }

                // Obtener información del archivo actual
                var (_, nombre, _) = await _servicioGoogleDrive.DescargarArchivoAsync(idUsuario, Id);
                NombreArchivoActual = nombre;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar información del archivo {IdArchivo}", Id);
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
                var permisos = await _servicioGoogleDrive.VerificarPermisosAsync(idUsuario);
                if (!permisos.PuedeEscribir)
                {
                    ModelState.AddModelError(string.Empty, "No tienes permisos para sobrescribir documentos en esta carpeta.");
                    await OnGetAsync();
                    return Page();
                }

                _logger.LogInformation("Sobrescribiendo archivo para usuario {UsuarioId}: {IdArchivo}, Nuevo archivo: {NombreArchivo}, Tamaño: {Tamano} bytes, Tipo: {TipoMime}",
                    idUsuario, Id, Archivo.FileName, Archivo.Length, Archivo.ContentType);

                // Obtener nombre del archivo actual para auditoría
                var (_, nombreActual, _) = await _servicioGoogleDrive.DescargarArchivoAsync(idUsuario, Id);

                // Sobrescribir archivo en Google Drive
                await _servicioGoogleDrive.SobrescribirArchivoAsync(
                    idUsuario,
                    Id,
                    Archivo.OpenReadStream(),
                    Archivo.ContentType
                );

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Documentacion.Sobrescribir",
                    "Documentacion",
                    $"Documento sobrescrito: {nombreActual} → {Archivo.FileName} (ID: {Id})",
                    "Exito"
                );

                _logger.LogInformation("Archivo sobrescrito exitosamente. ID: {IdArchivo}, Nombre anterior: {NombreAnterior}, Nombre nuevo: {NombreNuevo}",
                    Id, nombreActual, Archivo.FileName);
                TempData["MensajeExito"] = $"Archivo '{nombreActual}' sobrescrito exitosamente con '{Archivo.FileName}'.";
                return RedirectToPage("/Documentacion/Listado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sobrescribir archivo {IdArchivo}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Documentacion.Sobrescribir",
                    "Documentacion",
                    $"Error al sobrescribir documento: {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = $"Error al sobrescribir el archivo: {ex.Message}";
                await OnGetAsync();
                return Page();
            }
        }
    }
}

