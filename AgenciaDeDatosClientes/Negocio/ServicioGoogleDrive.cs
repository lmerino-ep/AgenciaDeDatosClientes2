using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Logging;
using Modelos.DTO;
using Modelos.InyeccionDeDependencias;
using Datos;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Negocio.Utils;

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de archivos en Google Drive usando credenciales del usuario
    /// </summary>
    public class ServicioGoogleDrive : IServicioGoogleDrive
    {
        private readonly Modelos.InyeccionDeDependencias.Google _configuracionGoogle;
        private readonly IRepositorioGoogleDrive _repositorio;
        private readonly ILogger<ServicioGoogleDrive> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _idCarpeta;

        public ServicioGoogleDrive(
            Modelos.InyeccionDeDependencias.Google configuracionGoogle,
            IRepositorioGoogleDrive repositorio,
            ILogger<ServicioGoogleDrive> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuracionGoogle = configuracionGoogle ?? throw new ArgumentNullException(nameof(configuracionGoogle));
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            if (string.IsNullOrEmpty(configuracionGoogle.DriveFolder))
            {
                throw new InvalidOperationException("DriveFolder no está configurado para Google Drive");
            }

            if (string.IsNullOrEmpty(configuracionGoogle.ClientId) || string.IsNullOrEmpty(configuracionGoogle.ClientSecret))
            {
                throw new InvalidOperationException("ClientId y ClientSecret de Google OAuth deben estar configurados para Google Drive");
            }

            _idCarpeta = configuracionGoogle.DriveFolder;
        }

        /// <summary>
        /// Obtiene un DriveService configurado con las credenciales del usuario
        /// </summary>
        private async Task<DriveService> ObtenerServicioDriveAsync(int idUsuario)
        {
            // Obtener tokens del usuario
            var (refreshToken, accessToken, expiresAt) = await _repositorio.ObtenerTokensAsync(idUsuario);

            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new InvalidOperationException("El usuario no tiene permisos de Google Drive configurados. Por favor, autoriza el acceso a Google Drive.");
            }

            // Verificar si el access token está expirado o no existe
            string tokenActual = accessToken;
            if (string.IsNullOrEmpty(accessToken) || !expiresAt.HasValue || expiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
            {
                // Refrescar el token
                tokenActual = await RefrescarAccessTokenAsync(refreshToken);
                DateTime nuevaExpiracion = DateTime.UtcNow.AddHours(1); // Los tokens de Google suelen durar 1 hora
                await _repositorio.ActualizarAccessTokenAsync(idUsuario, tokenActual, nuevaExpiracion);
            }

            // Crear credenciales del usuario
            var tokenResponse = new TokenResponse
            {
                AccessToken = tokenActual,
                RefreshToken = refreshToken
            };

            var clientSecrets = new ClientSecrets
            {
                ClientId = _configuracionGoogle.ClientId,
                ClientSecret = _configuracionGoogle.ClientSecret
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { DriveService.Scope.DriveFile }
            });

            var credential = new UserCredential(flow, "user", tokenResponse);

            // Crear servicio de Drive
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "EpData Dashboard"
            });
        }

        /// <summary>
        /// Refresca el access token usando el refresh token
        /// </summary>
        private async Task<string> RefrescarAccessTokenAsync(string refreshToken)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", _configuracionGoogle.ClientId },
                    { "client_secret", _configuracionGoogle.ClientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                });

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

                if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                {
                    throw new InvalidOperationException("No se pudo obtener el access token al refrescar");
                }

                return accessTokenElement.GetString();
            }
        }

        /// <summary>
        /// Lista archivos en la carpeta configurada de Google Drive o en una carpeta específica
        /// </summary>
        public async Task<List<ArchivoGoogleDrive>> ListarArchivosAsync(int idUsuario, string? filtroTipo = null, string? busqueda = null, DateTime? fechaDesde = null, string? carpetaId = null)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                _logger.LogInformation("Listando archivos de Google Drive para usuario {UsuarioId}. FiltroTipo: {FiltroTipo}, Búsqueda: {Busqueda}, FechaDesde: {FechaDesde}, CarpetaId: {CarpetaId}",
                    idUsuario, filtroTipo, busqueda, fechaDesde, carpetaId ?? "raíz");

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);
                FilesResource.ListRequest solicitud = servicioDrive.Files.List();

                // Usar carpetaId si se proporciona, sino usar la carpeta base configurada
                string idCarpetaActual = carpetaId ?? _idCarpeta;

                // Query base: archivos y carpetas en la carpeta actual que no estén en papelera
                string consulta = $"'{idCarpetaActual}' in parents and trashed=false";

                // Filtro por tipo MIME (solo excluir carpetas si hay un filtro específico de tipo de archivo)
                if (!string.IsNullOrEmpty(filtroTipo))
                {
                    consulta += " and mimeType != 'application/vnd.google-apps.folder'";
                    
                    string? consultaFiltro = GestorTiposArchivo.ConstruirConsultaFiltro(filtroTipo);
                    if (!string.IsNullOrEmpty(consultaFiltro))
                    {
                        consulta += " and " + consultaFiltro;
                    }
                }
                // Si no hay filtro de tipo, incluir tanto archivos como carpetas

                // Filtro por nombre
                if (!string.IsNullOrEmpty(busqueda))
                {
                    consulta += $" and name contains '{busqueda.Replace("'", "\\'")}'";
                }

                // Filtro por fecha de modificación (solo para archivos, no para carpetas)
                if (fechaDesde.HasValue)
                {
                    string fechaFormato = fechaDesde.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
                    // Incluir carpetas o archivos modificados después de la fecha
                    consulta += $" and (mimeType = 'application/vnd.google-apps.folder' or modifiedTime >= '{fechaFormato}')";
                }

                _logger.LogInformation("Consulta Google Drive: {Consulta}", consulta);
                _logger.LogInformation("ID Carpeta: {IdCarpeta}", idCarpetaActual);

                solicitud.Q = consulta;
                // Incluir shortcuts (accesos directos) y sus propiedades
                solicitud.Fields = "files(id, name, size, modifiedTime, mimeType, webViewLink, shortcutDetails)";
                // Soporte para Shared Drives (unidades compartidas)
                solicitud.SupportsAllDrives = true;
                solicitud.IncludeItemsFromAllDrives = true;
                // No usar OrderBy con modifiedTime porque las carpetas pueden no tenerlo
                // Ordenaremos en memoria después

                // Manejar paginación si es necesario
                List<Google.Apis.Drive.v3.Data.File> todosLosArchivos = new List<Google.Apis.Drive.v3.Data.File>();
                
                do
                {
                    FileList resultado = await solicitud.ExecuteAsync();
                    
                    if (resultado.Files != null && resultado.Files.Count > 0)
                    {
                        todosLosArchivos.AddRange(resultado.Files);
                        _logger.LogInformation("Obtenidos {Cantidad} elementos en esta página. Total acumulado: {Total}", 
                            resultado.Files.Count, todosLosArchivos.Count);
                    }
                    
                    // Si hay más páginas, continuar
                    solicitud.PageToken = resultado.NextPageToken;
                } while (!string.IsNullOrEmpty(solicitud.PageToken));
                
                _logger.LogInformation("Resultado total de Google Drive: {Cantidad} elementos encontrados", todosLosArchivos.Count);

                List<ArchivoGoogleDrive> archivos = todosLosArchivos.Select(archivo =>
                {
                    DateTime fechaModificacion = DateTime.MinValue;
                    bool esCarpeta = archivo.MimeType == "application/vnd.google-apps.folder";
                    bool esShortcut = archivo.MimeType == "application/vnd.google-apps.shortcut";
                    
                    // Manejar shortcuts (accesos directos) - obtener el tipo MIME del archivo destino
                    string? tipoMimeParaIcono = archivo.MimeType;
                    if (esShortcut && archivo.ShortcutDetails != null)
                    {
                        // Si es un shortcut, usar el tipo MIME del archivo destino
                        tipoMimeParaIcono = archivo.ShortcutDetails.TargetMimeType;
                        // Si el destino es una carpeta, marcarlo como carpeta
                        if (tipoMimeParaIcono == "application/vnd.google-apps.folder")
                        {
                            esCarpeta = true;
                        }
                    }
                    
                    // Obtener fecha de modificación desde ModifiedTimeRaw (ModifiedTime está deprecated)
                    if (!string.IsNullOrEmpty(archivo.ModifiedTimeRaw))
                    {
                        // ModifiedTimeRaw viene en formato RFC 3339 (ej: "2024-01-15T10:30:00.000Z")
                        if (DateTime.TryParse(archivo.ModifiedTimeRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                        {
                            fechaModificacion = parsedDate.ToUniversalTime();
                        }
                    }

                    return new ArchivoGoogleDrive
                    {
                        Id = archivo.Id,
                        Nombre = archivo.Name,
                        Tamano = archivo.Size ?? 0,
                        FechaModificacion = fechaModificacion,
                        // Usar el tipo MIME del destino si es un shortcut, sino usar el tipo MIME original
                        TipoMime = tipoMimeParaIcono ?? archivo.MimeType ?? "application/octet-stream",
                        UrlWeb = archivo.WebViewLink,
                        IconoTipo = GestorTiposArchivo.ObtenerIconoTipo(tipoMimeParaIcono),
                        EsCarpeta = esCarpeta,
                        EsShortcut = esShortcut
                    };
                })
                // Ordenar: primero carpetas, luego archivos, ambos alfabéticamente A-Z
                .OrderByDescending(a => a.EsCarpeta)
                .ThenBy(a => a.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToList();

                _logger.LogInformation("Se encontraron {Cantidad} archivos en Google Drive para usuario {UsuarioId}", archivos.Count, idUsuario);
                return archivos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar archivos de Google Drive para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Sube un archivo a Google Drive
        /// </summary>
        public async Task<string> SubirArchivoAsync(int idUsuario, Stream flujo, string nombreArchivo, string tipoMime)
        {
            try
            {
                _logger.LogInformation("Subiendo archivo a Google Drive para usuario {UsuarioId}: {NombreArchivo}, Tipo: {TipoMime}", idUsuario, nombreArchivo, tipoMime);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                Google.Apis.Drive.v3.Data.File metadatosArchivo = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = nombreArchivo,
                    Parents = new List<string> { _idCarpeta }
                };

                FilesResource.CreateMediaUpload solicitud = servicioDrive.Files.Create(metadatosArchivo, flujo, tipoMime);
                solicitud.Fields = "id, name, webViewLink";
                solicitud.SupportsAllDrives = true;

                IUploadProgress progreso = await solicitud.UploadAsync();

                if (progreso.Status != UploadStatus.Completed)
                {
                    string mensajeError = progreso.Exception?.Message ?? "Error desconocido al subir archivo";
                    _logger.LogError("Error al subir archivo: {Error}", mensajeError);
                    throw new Exception($"Error al subir archivo: {mensajeError}");
                }

                string idArchivo = solicitud.ResponseBody.Id;
                _logger.LogInformation("Archivo subido exitosamente. ID: {IdArchivo}, Nombre: {NombreArchivo}, Usuario: {UsuarioId}", idArchivo, nombreArchivo, idUsuario);

                return idArchivo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo {NombreArchivo} a Google Drive para usuario {UsuarioId}", nombreArchivo, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Descarga un archivo de Google Drive
        /// Los archivos de Google Docs/Sheets/Slides se exportan en lugar de descargarse directamente
        /// </summary>
        public async Task<(Stream Stream, string NombreArchivo, string TipoMime)> DescargarArchivoAsync(int idUsuario, string idArchivo)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                _logger.LogInformation("Descargando archivo de Google Drive para usuario {UsuarioId}: {IdArchivo}", idUsuario, idArchivo);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                // Obtener metadatos del archivo
                FilesResource.GetRequest solicitudMetadatos = servicioDrive.Files.Get(idArchivo);
                solicitudMetadatos.Fields = "name, mimeType, shortcutDetails";
                solicitudMetadatos.SupportsAllDrives = true;
                Google.Apis.Drive.v3.Data.File archivo = await solicitudMetadatos.ExecuteAsync();

                // Si es un shortcut, obtener el archivo destino
                string archivoIdReal = idArchivo;
                if (archivo.MimeType == "application/vnd.google-apps.shortcut" && archivo.ShortcutDetails != null)
                {
                    archivoIdReal = archivo.ShortcutDetails.TargetId;
                    // Obtener metadatos del archivo destino
                    FilesResource.GetRequest solicitudDestino = servicioDrive.Files.Get(archivoIdReal);
                    solicitudDestino.Fields = "name, mimeType";
                    solicitudDestino.SupportsAllDrives = true;
                    archivo = await solicitudDestino.ExecuteAsync();
                }

                string tipoMime = archivo.MimeType ?? "application/octet-stream";
                string nombreArchivo = archivo.Name ?? $"documento_{archivoIdReal}";
                MemoryStream flujo = new MemoryStream();

                // Los archivos de Google Docs/Sheets/Slides necesitan ser exportados
                if (tipoMime == "application/vnd.google-apps.document")
                {
                    // Exportar Google Docs como DOCX
                    FilesResource.ExportRequest solicitudExport = servicioDrive.Files.Export(archivoIdReal, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                    await solicitudExport.DownloadAsync(flujo);
                    tipoMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    nombreArchivo = Path.ChangeExtension(nombreArchivo, ".docx");
                }
                else if (tipoMime == "application/vnd.google-apps.spreadsheet")
                {
                    // Exportar Google Sheets como XLSX
                    FilesResource.ExportRequest solicitudExport = servicioDrive.Files.Export(archivoIdReal, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    await solicitudExport.DownloadAsync(flujo);
                    tipoMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    nombreArchivo = Path.ChangeExtension(nombreArchivo, ".xlsx");
                }
                else if (tipoMime == "application/vnd.google-apps.presentation")
                {
                    // Exportar Google Slides como PPTX
                    FilesResource.ExportRequest solicitudExport = servicioDrive.Files.Export(archivoIdReal, "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                    await solicitudExport.DownloadAsync(flujo);
                    tipoMime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    nombreArchivo = Path.ChangeExtension(nombreArchivo, ".pptx");
                }
                else
                {
                    // Descargar archivo normal
                    FilesResource.GetRequest solicitudDescarga = servicioDrive.Files.Get(archivoIdReal);
                    solicitudDescarga.SupportsAllDrives = true;
                    await solicitudDescarga.DownloadAsync(flujo);
                }

                flujo.Position = 0;
                _logger.LogInformation("Archivo descargado exitosamente. ID: {IdArchivo}, Nombre: {Nombre}, Tamaño: {Tamano} bytes, Usuario: {UsuarioId}",
                    archivoIdReal, nombreArchivo, flujo.Length, idUsuario);

                return (flujo, nombreArchivo, tipoMime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo {IdArchivo} de Google Drive para usuario {UsuarioId}", idArchivo, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Elimina un archivo de Google Drive
        /// </summary>
        public async Task EliminarArchivoAsync(int idUsuario, string idArchivo)
        {
            try
            {
                _logger.LogInformation("Eliminando archivo de Google Drive para usuario {UsuarioId}: {IdArchivo}", idUsuario, idArchivo);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                FilesResource.DeleteRequest solicitud = servicioDrive.Files.Delete(idArchivo);
                solicitud.SupportsAllDrives = true;
                await solicitud.ExecuteAsync();

                _logger.LogInformation("Archivo eliminado exitosamente. ID: {IdArchivo}, Usuario: {UsuarioId}", idArchivo, idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo {IdArchivo} de Google Drive para usuario {UsuarioId}", idArchivo, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Sobrescribe un archivo existente en Google Drive
        /// </summary>
        public async Task SobrescribirArchivoAsync(int idUsuario, string idArchivo, Stream flujo, string tipoMime)
        {
            try
            {
                _logger.LogInformation("Sobrescribiendo archivo en Google Drive para usuario {UsuarioId}: {IdArchivo}, Tipo: {TipoMime}", idUsuario, idArchivo, tipoMime);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                Google.Apis.Drive.v3.Data.File metadatosArchivo = new Google.Apis.Drive.v3.Data.File();
                FilesResource.UpdateMediaUpload solicitud = servicioDrive.Files.Update(metadatosArchivo, idArchivo, flujo, tipoMime);
                solicitud.Fields = "id, name, modifiedTime";
                solicitud.SupportsAllDrives = true;

                IUploadProgress progreso = await solicitud.UploadAsync();

                if (progreso.Status != UploadStatus.Completed)
                {
                    string mensajeError = progreso.Exception?.Message ?? "Error desconocido al sobrescribir archivo";
                    _logger.LogError("Error al sobrescribir archivo: {Error}", mensajeError);
                    throw new Exception($"Error al sobrescribir archivo: {mensajeError}");
                }

                _logger.LogInformation("Archivo sobrescrito exitosamente. ID: {IdArchivo}, Usuario: {UsuarioId}", idArchivo, idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sobrescribir archivo {IdArchivo} en Google Drive para usuario {UsuarioId}", idArchivo, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Verifica los permisos del usuario en la carpeta de Google Drive
        /// </summary>
        public async Task<PermisosGoogleDrive> VerificarPermisosAsync(int idUsuario)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                _logger.LogInformation("Verificando permisos de Google Drive para usuario {UsuarioId}", idUsuario);

                // Obtener tokens del usuario
                var (refreshToken, accessToken, expiresAt) = await _repositorio.ObtenerTokensAsync(idUsuario);

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return new PermisosGoogleDrive
                    {
                        PuedeLeer = false,
                        PuedeEscribir = false,
                        PuedeEliminar = false,
                        TieneAcceso = false,
                        Mensaje = "El usuario no ha autorizado el acceso a Google Drive"
                    };
                }

                // Obtener servicio de Drive
                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                // Intentar obtener información de la carpeta para verificar permisos
                try
                {
                    FilesResource.GetRequest solicitud = servicioDrive.Files.Get(_idCarpeta);
                    solicitud.Fields = "id, name, permissions";
                    solicitud.SupportsAllDrives = true;
                    Google.Apis.Drive.v3.Data.File carpeta = await solicitud.ExecuteAsync();

                    // Verificar permisos del usuario en la carpeta
                    // Obtener el email del usuario actual
                    var usuario = await ObtenerEmailUsuarioAsync(idUsuario);
                    if (string.IsNullOrEmpty(usuario))
                    {
                        return new PermisosGoogleDrive
                        {
                            PuedeLeer = false,
                            PuedeEscribir = false,
                            PuedeEliminar = false,
                            TieneAcceso = false,
                            Mensaje = "No se pudo obtener el email del usuario"
                        };
                    }

                    // Obtener permisos de la carpeta
                    PermissionsResource.ListRequest permisosRequest = servicioDrive.Permissions.List(_idCarpeta);
                    permisosRequest.Fields = "permissions(id, emailAddress, role, type)";
                    PermissionList permisos = await permisosRequest.ExecuteAsync();

                    var permisoUsuario = permisos.Permissions?.FirstOrDefault(p => 
                        p.Type == "user" && 
                        p.EmailAddress?.Equals(usuario, StringComparison.OrdinalIgnoreCase) == true);

                    if (permisoUsuario == null)
                    {
                        // Intentar verificar si puede listar archivos (permiso de lectura)
                        try
                        {
                            FilesResource.ListRequest testRequest = servicioDrive.Files.List();
                            testRequest.Q = $"'{_idCarpeta}' in parents and trashed=false";
                            testRequest.SupportsAllDrives = true;
                            testRequest.IncludeItemsFromAllDrives = true;
                            testRequest.PageSize = 1;
                            await testRequest.ExecuteAsync();

                            return new PermisosGoogleDrive
                            {
                                PuedeLeer = true,
                                PuedeEscribir = false, // Se verificará intentando escribir
                                PuedeEliminar = false, // Se verificará intentando eliminar
                                TieneAcceso = true,
                                Mensaje = "Usuario tiene acceso a la carpeta"
                            };
                        }
                        catch
                        {
                            return new PermisosGoogleDrive
                            {
                                PuedeLeer = false,
                                PuedeEscribir = false,
                                PuedeEliminar = false,
                                TieneAcceso = false,
                                Mensaje = "El usuario no tiene acceso a la carpeta"
                            };
                        }
                    }

                    // Determinar permisos según el rol
                    bool puedeLeer = permisoUsuario.Role == "reader" || permisoUsuario.Role == "writer" || permisoUsuario.Role == "owner";
                    bool puedeEscribir = permisoUsuario.Role == "writer" || permisoUsuario.Role == "owner";
                    bool puedeEliminar = permisoUsuario.Role == "owner";

                    return new PermisosGoogleDrive
                    {
                        PuedeLeer = puedeLeer,
                        PuedeEscribir = puedeEscribir,
                        PuedeEliminar = puedeEliminar,
                        TieneAcceso = true,
                        Mensaje = $"Permisos: {permisoUsuario.Role}"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al verificar permisos específicos, intentando verificación básica para usuario {UsuarioId}", idUsuario);

                    // Si falla, intentar una operación básica de lectura
                    try
                    {
                        FilesResource.ListRequest testRequest = servicioDrive.Files.List();
                        testRequest.Q = $"'{_idCarpeta}' in parents and trashed=false";
                        testRequest.PageSize = 1;
                        testRequest.SupportsAllDrives = true;
                        testRequest.IncludeItemsFromAllDrives = true;
                        await testRequest.ExecuteAsync();

                        return new PermisosGoogleDrive
                        {
                            PuedeLeer = true,
                            PuedeEscribir = false, // No se puede determinar sin intentar escribir
                            PuedeEliminar = false, // No se puede determinar sin intentar eliminar
                            TieneAcceso = true,
                            Mensaje = "Usuario tiene acceso de lectura a la carpeta"
                        };
                    }
                    catch
                    {
                        return new PermisosGoogleDrive
                        {
                            PuedeLeer = false,
                            PuedeEscribir = false,
                            PuedeEliminar = false,
                            TieneAcceso = false,
                            Mensaje = $"Error al verificar permisos: {ex.Message}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar permisos de Google Drive para usuario {UsuarioId}", idUsuario);
                return new PermisosGoogleDrive
                {
                    PuedeLeer = false,
                    PuedeEscribir = false,
                    PuedeEliminar = false,
                    TieneAcceso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Obtiene la URL de autorización de Google Drive para el usuario
        /// </summary>
        public string ObtenerUrlAutorizacion(string redirectUri, string state)
        {
            // Usar scope 'drive' para acceder a archivos existentes y Shared Drives
            // 'drive.file' solo permite acceso a archivos creados por la app
            var scopes = new[] { DriveService.Scope.Drive };
            string scopeString = string.Join(" ", scopes);

            var parameters = new Dictionary<string, string>
            {
                { "client_id", _configuracionGoogle.ClientId },
                { "redirect_uri", redirectUri },
                { "response_type", "code" },
                { "scope", scopeString },
                { "access_type", "offline" }, // Necesario para obtener refresh token
                { "prompt", "consent" }, // Forzar consentimiento para obtener refresh token
                { "state", state }
            };

            string queryString = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
            return $"https://accounts.google.com/o/oauth2/v2/auth?{queryString}";
        }

        /// <summary>
        /// Intercambia un código de autorización por tokens de Google Drive
        /// </summary>
        public async Task IntercambiarCodigoPorTokensAsync(int idUsuario, string codigo, string redirectUri)
        {
            try
            {
                _logger.LogInformation("Intercambiando código de autorización por tokens de Google Drive para usuario {UsuarioId}", idUsuario);

                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
                    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "client_id", _configuracionGoogle.ClientId },
                        { "client_secret", _configuracionGoogle.ClientSecret },
                        { "code", codigo },
                        { "redirect_uri", redirectUri },
                        { "grant_type", "authorization_code" }
                    });

                    var response = await httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);

                    if (!tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement))
                    {
                        // Si no hay refresh_token, intentar usar el access_token existente
                        // Esto puede pasar si el usuario ya autorizó antes
                        if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
                        {
                            string nuevoAccessToken = accessTokenElement.GetString();
                            DateTime nuevaExpiracion = DateTime.UtcNow.AddHours(1);
                            
                            // Obtener refresh token existente del usuario
                            var (refreshTokenExistente, _, _) = await _repositorio.ObtenerTokensAsync(idUsuario);
                            
                            if (!string.IsNullOrEmpty(refreshTokenExistente))
                            {
                                await _repositorio.ActualizarAccessTokenAsync(idUsuario, nuevoAccessToken, nuevaExpiracion);
                                _logger.LogInformation("Access token actualizado para usuario {UsuarioId} (refresh token ya existía)", idUsuario);
                                return;
                            }
                        }
                        
                        throw new InvalidOperationException("No se recibió refresh_token. Asegúrate de incluir 'prompt=consent' en la URL de autorización.");
                    }

                    string refreshToken = refreshTokenElement.GetString();
                    string nuevoAccessToken2 = tokenResponse.TryGetProperty("access_token", out var at) ? at.GetString() : null;
                    DateTime? nuevaExpiracion2 = nuevoAccessToken2 != null ? DateTime.UtcNow.AddHours(1) : null;

                    await _repositorio.GuardarTokensAsync(idUsuario, refreshToken, nuevoAccessToken2, nuevaExpiracion2);

                    _logger.LogInformation("Tokens de Google Drive guardados exitosamente para usuario {UsuarioId}", idUsuario);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intercambiar código por tokens de Google Drive para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene información de una carpeta (nombre, ID, padres) para construir breadcrumbs
        /// </summary>
        public async Task<ArchivoGoogleDrive?> ObtenerInformacionCarpetaAsync(int idUsuario, string carpetaId)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);
                FilesResource.GetRequest solicitud = servicioDrive.Files.Get(carpetaId);
                solicitud.Fields = "id, name, parents, mimeType, webViewLink";
                solicitud.SupportsAllDrives = true;

                Google.Apis.Drive.v3.Data.File carpeta = await solicitud.ExecuteAsync();

                if (carpeta.MimeType != "application/vnd.google-apps.folder")
                {
                    return null; // No es una carpeta
                }

                DateTime fechaModificacion = DateTime.MinValue;
                if (!string.IsNullOrEmpty(carpeta.ModifiedTimeRaw))
                {
                    if (DateTime.TryParse(carpeta.ModifiedTimeRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                    {
                        fechaModificacion = parsedDate.ToUniversalTime();
                    }
                }

                return new ArchivoGoogleDrive
                {
                    Id = carpeta.Id,
                    Nombre = carpeta.Name,
                    Tamano = 0,
                    FechaModificacion = fechaModificacion,
                    TipoMime = carpeta.MimeType,
                    UrlWeb = carpeta.WebViewLink,
                    IconoTipo = "carpeta",
                    EsCarpeta = true,
                    EsShortcut = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información de carpeta {CarpetaId} para usuario {UsuarioId}", carpetaId, idUsuario);
                return null;
            }
        }

        /// <summary>
        /// Obtiene la ruta completa de breadcrumbs desde la carpeta base hasta la carpeta especificada
        /// </summary>
        public async Task<List<ArchivoGoogleDrive>> ObtenerRutaBreadcrumbsAsync(int idUsuario, string carpetaId)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                List<ArchivoGoogleDrive> breadcrumbs = new List<ArchivoGoogleDrive>();
                string? carpetaActualId = carpetaId;
                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);
                int nivel = 0;
                const int maxNiveles = 10;

                // Construir la ruta desde la carpeta actual hacia arriba hasta llegar a la carpeta base
                while (!string.IsNullOrEmpty(carpetaActualId) && carpetaActualId != _idCarpeta && nivel < maxNiveles)
                {
                    // Obtener información de la carpeta actual
                    var carpeta = await ObtenerInformacionCarpetaAsync(idUsuario, carpetaActualId);
                    if (carpeta == null)
                        break;

                    // Insertar al inicio de la lista (para mantener el orden: padre -> hijo)
                    breadcrumbs.Insert(0, carpeta);

                    // Obtener el padre de esta carpeta
                    FilesResource.GetRequest solicitud = servicioDrive.Files.Get(carpetaActualId);
                    solicitud.Fields = "parents";
                    solicitud.SupportsAllDrives = true;

                    Google.Apis.Drive.v3.Data.File carpetaInfo = await solicitud.ExecuteAsync();
                    
                    // Si tiene padres, tomar el primero (normalmente solo hay uno)
                    if (carpetaInfo.Parents != null && carpetaInfo.Parents.Count > 0)
                    {
                        carpetaActualId = carpetaInfo.Parents[0];
                    }
                    else
                    {
                        break; // No tiene más padres
                    }

                    nivel++;
                }

                if (nivel >= maxNiveles)
                {
                    _logger.LogWarning("Se alcanzó el límite de niveles de breadcrumbs ({MaxNiveles}) para carpeta {CarpetaId}", maxNiveles, carpetaId);
                }

                return breadcrumbs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ruta de breadcrumbs para carpeta {CarpetaId} y usuario {UsuarioId}", carpetaId, idUsuario);
                return new List<ArchivoGoogleDrive>();
            }
        }

        /// <summary>
        /// Obtiene los tipos de archivo únicos presentes en la carpeta actual
        /// </summary>
        public async Task<List<string>> ObtenerTiposArchivoDisponiblesAsync(int idUsuario, string? carpetaId = null)
        {
            try
            {
                // Verificar y limpiar tokens si el usuario cambió
                await _repositorio.VerificarYLimpiarTokensSiUsuarioCambioAsync(idUsuario);

                // Obtener todos los archivos de la carpeta (sin filtros)
                var archivos = await ListarArchivosAsync(idUsuario, null, null, null, carpetaId);

                // Obtener tipos MIME únicos de archivos reales (excluir carpetas y shortcuts)
                // Los shortcuts ya tienen su TipoMime reemplazado por el del destino, pero debemos excluirlos
                // porque queremos contar solo archivos reales, no accesos directos
                var tiposMime = archivos
                    .Where(a => !a.EsCarpeta && !a.EsShortcut && !string.IsNullOrEmpty(a.TipoMime))
                    .Select(a => a.TipoMime)
                    .Distinct()
                    .ToList();

                // Mapear tipos MIME a nombres de filtro usando el gestor centralizado
                HashSet<string> tiposDisponibles = new HashSet<string>();

                foreach (var tipoMime in tiposMime)
                {
                    string? nombreFiltro = GestorTiposArchivo.ObtenerNombreFiltro(tipoMime);
                    if (!string.IsNullOrEmpty(nombreFiltro))
                    {
                        tiposDisponibles.Add(nombreFiltro);
                    }
                }

                return tiposDisponibles.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de archivo disponibles para usuario {UsuarioId} y carpeta {CarpetaId}", idUsuario, carpetaId ?? "raíz");
                return new List<string>();
            }
        }

        /// <summary>
        /// Obtiene el email del usuario desde los claims de autenticación
        /// </summary>
        private Task<string?> ObtenerEmailUsuarioAsync(int idUsuario)
        {
            // Obtener email desde los claims del usuario autenticado
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return Task.FromResult<string?>(null);
            }

            string? email = user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.Identity?.Name;
            
            return Task.FromResult(email);
        }

    }
}
