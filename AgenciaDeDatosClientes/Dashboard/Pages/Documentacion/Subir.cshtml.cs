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
        private readonly IServicioDocumentacionS3 _servicioDocumentacionS3;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<SubirModel> _logger;

        [BindProperty]
        public IFormFileCollection? Archivos { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CarpetaRuta { get; set; }

        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public SubirModel(
            IServicioDocumentacionS3 servicioDocumentacionS3,
            IServicioAuditoria servicioAuditoria,
            ILogger<SubirModel> logger)
        {
            _servicioDocumentacionS3 = servicioDocumentacionS3 ?? throw new ArgumentNullException(nameof(servicioDocumentacionS3));
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
                // Obtener archivos del formulario
                var archivos = Request.Form.Files;
                if (archivos == null || archivos.Count == 0)
                {
                    ModelState.AddModelError("Archivos", "Debe seleccionar al menos un archivo");
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
                var permisos = await _servicioDocumentacionS3.VerificarPermisosAsync(User);
                if (!permisos.PuedeEscribir)
                {
                    ModelState.AddModelError(string.Empty, "No tienes permisos para subir documentos.");
                    return Page();
                }

                // Validar todos los archivos antes de subir
                const long tamanoMaximo = 50 * 1024 * 1024; // 50 MB
                List<string> erroresValidacion = new List<string>();
                List<IFormFile> archivosValidos = new List<IFormFile>();

                foreach (var archivo in archivos)
                {
                    if (archivo.Length == 0)
                    {
                        erroresValidacion.Add($"{archivo.FileName}: El archivo está vacío");
                        continue;
                    }

                    if (archivo.Length > tamanoMaximo)
                    {
                        erroresValidacion.Add($"{archivo.FileName}: El archivo no puede superar los 50 MB. Tamaño actual: {archivo.Length / (1024 * 1024)} MB");
                        continue;
                    }

                    if (!EsTipoPermitido(archivo.ContentType))
                    {
                        erroresValidacion.Add($"{archivo.FileName}: Tipo de archivo no permitido ({archivo.ContentType})");
                        continue;
                    }

                    archivosValidos.Add(archivo);
                }

                if (erroresValidacion.Count > 0)
                {
                    foreach (var error in erroresValidacion)
                    {
                        ModelState.AddModelError("Archivos", error);
                    }
                    if (archivosValidos.Count == 0)
                    {
                        return Page();
                    }
                }

                if (archivosValidos.Count == 0)
                {
                    ModelState.AddModelError("Archivos", "No hay archivos válidos para subir");
                    return Page();
                }

                _logger.LogInformation("Subiendo {Cantidad} archivo(s) para usuario {UsuarioId}, Carpeta: {CarpetaRuta}",
                    archivosValidos.Count, idUsuario, CarpetaRuta ?? "raíz");

                // Subir archivos en paralelo para mayor velocidad
                var tareasSubida = archivosValidos.Select(async archivo =>
                {
                    try
                    {
                        _logger.LogInformation("Subiendo archivo: {NombreArchivo}, Tamaño: {Tamano} bytes, Tipo: {TipoMime}",
                            archivo.FileName, archivo.Length, archivo.ContentType);

                        string key = await _servicioDocumentacionS3.SubirArchivoAsync(
                            idUsuario,
                            archivo.OpenReadStream(),
                            archivo.FileName,
                            archivo.ContentType,
                            CarpetaRuta
                        );

                        // Registrar en auditoría
                        await _servicioAuditoria.RegistrarAccionAsync(
                            User,
                            Modelos.AuditoriaAccionEnum.DocumentacionSubir,
                            $"Documento subido: {archivo.FileName} (Key: {key})"
                        );

                        _logger.LogInformation("Archivo subido exitosamente. Key: {Key}, Nombre: {NombreArchivo}", key, archivo.FileName);
                        return new { Exito = true, Nombre = archivo.FileName, Key = key, Error = (string?)null };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al subir archivo {NombreArchivo}", archivo.FileName);
                        await _servicioAuditoria.RegistrarAccionAsync(
                            User,
                            Modelos.AuditoriaAccionEnum.DocumentacionSubir,
                            $"Error al subir documento: {archivo.FileName} - {ex.Message}"
                        );
                        return new { Exito = false, Nombre = archivo.FileName, Key = (string?)null, Error = ex.Message };
                    }
                }).ToList();

                // Esperar a que todas las subidas terminen
                var resultados = await Task.WhenAll(tareasSubida);

                // Construir mensajes de resultado
                var exitosos = resultados.Where(r => r.Exito).ToList();
                var fallidos = resultados.Where(r => !r.Exito).ToList();

                List<string> mensajes = new List<string>();

                if (exitosos.Count > 0)
                {
                    if (exitosos.Count == 1)
                    {
                        mensajes.Add($"Archivo '{exitosos[0].Nombre}' subido exitosamente.");
                    }
                    else
                    {
                        mensajes.Add($"{exitosos.Count} archivos subidos exitosamente: {string.Join(", ", exitosos.Select(e => $"'{e.Nombre}'"))}");
                    }
                }

                if (fallidos.Count > 0)
                {
                    if (fallidos.Count == 1)
                    {
                        mensajes.Add($"Error al subir '{fallidos[0].Nombre}': {fallidos[0].Error}");
                    }
                    else
                    {
                        mensajes.Add($"Error al subir {fallidos.Count} archivo(s): {string.Join("; ", fallidos.Select(f => $"'{f.Nombre}' ({f.Error})"))}");
                    }
                }

                if (erroresValidacion.Count > 0)
                {
                    mensajes.Add($"Advertencias de validación: {string.Join("; ", erroresValidacion)}");
                }

                if (exitosos.Count > 0)
                {
                    TempData["MensajeExito"] = string.Join(" ", mensajes);
                }
                else
                {
                    TempData["MensajeError"] = string.Join(" ", mensajes);
                }

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
                _logger.LogError(ex, "Error al subir archivos");

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    Modelos.AuditoriaAccionEnum.DocumentacionSubir,
                    $"Error al subir documentos: {ex.Message}"
                );

                MensajeError = $"Error al subir los archivos: {ex.Message}";
                return Page();
            }
        }

        private bool EsTipoPermitido(string tipoMime)
        {
            string[] tiposPermitidos = {
                "application/pdf", // PDF
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
                "application/msword", // DOC
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

