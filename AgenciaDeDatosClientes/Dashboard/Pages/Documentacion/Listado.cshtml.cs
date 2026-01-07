using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO;
using Negocio;
using System.Security.Claims;

namespace Pages.Documentacion
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.GestionDocumentacion)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class ListadoModel : PageModel
    {
        private readonly IServicioGoogleDrive _servicioGoogleDrive;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<ListadoModel> _logger;

        public PermisosGoogleDrive Permisos { get; set; }

        public ListadoModel(
            IServicioGoogleDrive servicioGoogleDrive,
            IServicioAuditoria servicioAuditoria,
            ILogger<ListadoModel> logger)
        {
            _servicioGoogleDrive = servicioGoogleDrive ?? throw new ArgumentNullException(nameof(servicioGoogleDrive));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public string? FiltroTipo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Busqueda { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FiltroFecha { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CarpetaId { get; set; }

        public List<ArchivoGoogleDrive> Archivos { get; set; } = new List<ArchivoGoogleDrive>();

        public List<ArchivoGoogleDrive> Breadcrumbs { get; set; } = new List<ArchivoGoogleDrive>();

        public List<string> TiposDisponibles { get; set; } = new List<string>();

        public string? MensajeError { get; set; }

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

                // Verificar permisos
                Permisos = await _servicioGoogleDrive.VerificarPermisosAsync(idUsuario);

                if (!Permisos.TieneAcceso)
                {
                    MensajeError = Permisos.Mensaje ?? "No tienes acceso a Google Drive. Por favor, autoriza el acceso.";
                    return Page();
                }

                _logger.LogInformation("Listando documentos para usuario {UsuarioId}. FiltroTipo: {FiltroTipo}, Busqueda: {Busqueda}, FiltroFecha: {FiltroFecha}, CarpetaId: {CarpetaId}",
                    idUsuario, FiltroTipo, Busqueda, FiltroFecha, CarpetaId ?? "raíz");

                DateTime? fechaDesde = ObtenerFechaDesdeFiltro(FiltroFecha);
                Archivos = await _servicioGoogleDrive.ListarArchivosAsync(idUsuario, FiltroTipo, Busqueda, fechaDesde, CarpetaId);

                // Construir breadcrumbs si estamos en una subcarpeta
                if (!string.IsNullOrEmpty(CarpetaId))
                {
                    Breadcrumbs = await _servicioGoogleDrive.ObtenerRutaBreadcrumbsAsync(idUsuario, CarpetaId);
                }

                // Obtener tipos de archivo disponibles en la carpeta actual
                TiposDisponibles = await _servicioGoogleDrive.ObtenerTiposArchivoDisponiblesAsync(idUsuario, CarpetaId);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar documentos");
                MensajeError = $"Error al cargar los documentos: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEliminarAsync(string id)
        {
            try
            {
                // Obtener ID del usuario autenticado
                string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int idUsuario))
                {
                    _logger.LogWarning("No se pudo obtener el ID del usuario autenticado");
                    TempData["MensajeError"] = "Error al obtener información del usuario.";
                    return RedirectToPage();
                }

                // Verificar permisos
                Permisos = await _servicioGoogleDrive.VerificarPermisosAsync(idUsuario);
                if (!Permisos.PuedeEliminar)
                {
                    TempData["MensajeError"] = "No tienes permisos para eliminar documentos en esta carpeta.";
                    return RedirectToPage();
                }

                _logger.LogInformation("Eliminando documento: {IdArchivo} por usuario {UsuarioId}", id, idUsuario);

                await _servicioGoogleDrive.EliminarArchivoAsync(idUsuario, id);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionEliminar,
                    $"Documento eliminado: {id}"
                );

                TempData["MensajeExito"] = "Documento eliminado exitosamente.";
                
                // Preservar CarpetaId al redirigir
                if (!string.IsNullOrEmpty(CarpetaId))
                {
                    return RedirectToPage(new { CarpetaId });
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento {IdArchivo}", id);
                TempData["MensajeError"] = $"Error al eliminar el documento: {ex.Message}";
                return RedirectToPage();
            }
        }

        private DateTime? ObtenerFechaDesdeFiltro(string? filtroFecha)
        {
            if (string.IsNullOrEmpty(filtroFecha))
                return null;

            return filtroFecha.ToLower() switch
            {
                "semana" => DateTime.UtcNow.AddDays(-7),
                "mes" => DateTime.UtcNow.AddMonths(-1),
                "año" => DateTime.UtcNow.AddYears(-1),
                _ => null
            };
        }

    }
}

