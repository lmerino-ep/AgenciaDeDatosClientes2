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
        private readonly IServicioDocumentacionS3 _servicioDocumentacionS3;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<ListadoModel> _logger;

        public PermisosDocumentacionS3 Permisos { get; set; }

        public ListadoModel(
            IServicioDocumentacionS3 servicioDocumentacionS3,
            IServicioAuditoria servicioAuditoria,
            ILogger<ListadoModel> logger)
        {
            _servicioDocumentacionS3 = servicioDocumentacionS3 ?? throw new ArgumentNullException(nameof(servicioDocumentacionS3));
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
        public string? CarpetaRuta { get; set; }

        public List<ArchivoS3> Archivos { get; set; } = new List<ArchivoS3>();

        public List<ArchivoS3> Breadcrumbs { get; set; } = new List<ArchivoS3>();

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
                Permisos = await _servicioDocumentacionS3.VerificarPermisosAsync(User);

                if (!Permisos.TieneAcceso)
                {
                    MensajeError = Permisos.Mensaje ?? "No tienes acceso a la documentación.";
                    return Page();
                }

                _logger.LogInformation("Listando documentos para usuario {UsuarioId}. FiltroTipo: {FiltroTipo}, Busqueda: {Busqueda}, FiltroFecha: {FiltroFecha}, CarpetaRuta: {CarpetaRuta}",
                    idUsuario, FiltroTipo, Busqueda, FiltroFecha, CarpetaRuta ?? "raíz");

                DateTime? fechaDesde = ObtenerFechaDesdeFiltro(FiltroFecha);
                Archivos = await _servicioDocumentacionS3.ListarArchivosAsync(idUsuario, FiltroTipo, Busqueda, fechaDesde, CarpetaRuta);

                // Construir breadcrumbs si estamos en una subcarpeta
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    Breadcrumbs = await _servicioDocumentacionS3.ObtenerRutaBreadcrumbsAsync(idUsuario, CarpetaRuta);
                }

                // Obtener tipos de archivo disponibles en la carpeta actual
                TiposDisponibles = await _servicioDocumentacionS3.ObtenerTiposArchivoDisponiblesAsync(idUsuario, CarpetaRuta);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar documentos");
                MensajeError = $"Error al cargar los documentos: {ex.Message}";
                return Page();
            }
        }

        [BindProperty]
        public string? TipoEliminacion { get; set; }

        public async Task<IActionResult> OnPostVolverVersionAnteriorAsync(string key)
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
                Permisos = await _servicioDocumentacionS3.VerificarPermisosAsync(User);
                if (!Permisos.PuedeEliminar)
                {
                    TempData["MensajeError"] = "No tienes permisos para eliminar documentos.";
                    return RedirectToPage();
                }

                _logger.LogInformation("Volviendo a versión anterior: {Key} por usuario {UsuarioId}", key, idUsuario);

                await _servicioDocumentacionS3.VolverAVersionAnteriorAsync(idUsuario, key);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionEliminar,
                    $"Volver a versión anterior: {key}"
                );

                TempData["MensajeExito"] = "Se ha vuelto a la versión anterior del documento.";
                
                // Preservar CarpetaRuta al redirigir (construir URL manualmente para preservar el case)
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
                return RedirectToPage();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Intento de volver a versión anterior no válido: {Key}", key);
                TempData["MensajeError"] = ex.Message;
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al volver a versión anterior {Key}", key);
                TempData["MensajeError"] = $"Error al volver a la versión anterior: {ex.Message}";
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEliminarCompletamenteAsync(string key)
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
                Permisos = await _servicioDocumentacionS3.VerificarPermisosAsync(User);
                if (!Permisos.PuedeEliminar)
                {
                    TempData["MensajeError"] = "No tienes permisos para eliminar documentos.";
                    return RedirectToPage();
                }

                _logger.LogInformation("Eliminando completamente documento: {Key} por usuario {UsuarioId}", key, idUsuario);

                await _servicioDocumentacionS3.EliminarCompletamenteAsync(idUsuario, key);

                // Registrar en auditoría
                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionEliminar,
                    $"Documento eliminado completamente: {key}"
                );

                TempData["MensajeExito"] = "Documento eliminado completamente. La última versión se ha movido a la papelera.";
                
                // Preservar CarpetaRuta al redirigir (construir URL manualmente para preservar el case)
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar completamente documento {Key}", key);
                TempData["MensajeError"] = $"Error al eliminar el documento: {ex.Message}";
                if (!string.IsNullOrEmpty(CarpetaRuta))
                {
                    string url = $"/documentacion/listado?CarpetaRuta={Uri.EscapeDataString(CarpetaRuta)}";
                    return Redirect(url);
                }
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

