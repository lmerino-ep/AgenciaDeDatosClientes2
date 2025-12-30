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

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de archivos en Google Drive usando credenciales del usuario
    /// </summary>
    public class ServicioGoogleDrive : IServicioGoogleDrive
    {
        private readonly Google _configuracionGoogle;
        private readonly RepositorioGoogleDrive _repositorio;
        private readonly ILogger<ServicioGoogleDrive> _logger;
        private readonly string _idCarpeta;

        public ServicioGoogleDrive(
            Google configuracionGoogle,
            RepositorioGoogleDrive repositorio,
            ILogger<ServicioGoogleDrive> logger)
        {
            _configuracionGoogle = configuracionGoogle ?? throw new ArgumentNullException(nameof(configuracionGoogle));
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
        /// Lista archivos en la carpeta configurada de Google Drive
        /// </summary>
        public async Task<List<ArchivoGoogleDrive>> ListarArchivosAsync(int idUsuario, string? filtroTipo = null, string? busqueda = null, DateTime? fechaDesde = null)
        {
            try
            {
                _logger.LogInformation("Listando archivos de Google Drive para usuario {UsuarioId}. FiltroTipo: {FiltroTipo}, Búsqueda: {Busqueda}, FechaDesde: {FechaDesde}",
                    idUsuario, filtroTipo, busqueda, fechaDesde);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);
                FilesResource.ListRequest solicitud = servicioDrive.Files.List();

                // Query base: archivos en la carpeta configurada que no estén en papelera
                string consulta = $"'{_idCarpeta}' in parents and trashed=false";

                // Filtro por tipo MIME
                if (!string.IsNullOrEmpty(filtroTipo))
                {
                    string mimeType = ObtenerMimeType(filtroTipo);
                    if (!string.IsNullOrEmpty(mimeType))
                    {
                        consulta += $" and mimeType contains '{mimeType}'";
                    }
                }

                // Filtro por nombre
                if (!string.IsNullOrEmpty(busqueda))
                {
                    consulta += $" and name contains '{busqueda.Replace("'", "\\'")}'";
                }

                // Filtro por fecha de modificación
                if (fechaDesde.HasValue)
                {
                    string fechaFormato = fechaDesde.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
                    consulta += $" and modifiedTime >= '{fechaFormato}'";
                }

                solicitud.Q = consulta;
                solicitud.Fields = "files(id, name, size, modifiedTime, mimeType, webViewLink)";
                solicitud.OrderBy = "modifiedTime desc";

                FileList resultado = await solicitud.ExecuteAsync();

                List<ArchivoGoogleDrive> archivos = resultado.Files.Select(archivo => new ArchivoGoogleDrive
                {
                    Id = archivo.Id,
                    Nombre = archivo.Name,
                    Tamano = long.Parse(archivo.Size ?? "0"),
                    FechaModificacion = archivo.ModifiedTime?.DateTime ?? DateTime.MinValue,
                    TipoMime = archivo.MimeType ?? "application/octet-stream",
                    UrlWeb = archivo.WebViewLink,
                    IconoTipo = ObtenerIconoTipo(archivo.MimeType)
                }).ToList();

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
        /// </summary>
        public async Task<(Stream Stream, string NombreArchivo, string TipoMime)> DescargarArchivoAsync(int idUsuario, string idArchivo)
        {
            try
            {
                _logger.LogInformation("Descargando archivo de Google Drive para usuario {UsuarioId}: {IdArchivo}", idUsuario, idArchivo);

                DriveService servicioDrive = await ObtenerServicioDriveAsync(idUsuario);

                // Obtener metadatos del archivo
                FilesResource.GetRequest solicitudMetadatos = servicioDrive.Files.Get(idArchivo);
                solicitudMetadatos.Fields = "name, mimeType";
                Google.Apis.Drive.v3.Data.File archivo = await solicitudMetadatos.ExecuteAsync();

                // Descargar el contenido
                FilesResource.GetRequest solicitudDescarga = servicioDrive.Files.Get(idArchivo);
                MemoryStream flujo = new MemoryStream();

                await solicitudDescarga.DownloadAsync(flujo);

                flujo.Position = 0;
                _logger.LogInformation("Archivo descargado exitosamente. ID: {IdArchivo}, Nombre: {Nombre}, Tamaño: {Tamano} bytes, Usuario: {UsuarioId}",
                    idArchivo, archivo.Name, flujo.Length, idUsuario);

                return (flujo, archivo.Name ?? $"documento_{idArchivo}", archivo.MimeType ?? "application/octet-stream");
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
                            testRequest.MaxResults = 1;
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
                        testRequest.MaxResults = 1;
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
            var scopes = new[] { DriveService.Scope.DriveFile };
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
                            string accessToken = accessTokenElement.GetString();
                            DateTime expiresAt = DateTime.UtcNow.AddHours(1);
                            
                            // Obtener refresh token existente del usuario
                            var (refreshTokenExistente, _, _) = await _repositorio.ObtenerTokensAsync(idUsuario);
                            
                            if (!string.IsNullOrEmpty(refreshTokenExistente))
                            {
                                await _repositorio.ActualizarAccessTokenAsync(idUsuario, accessToken, expiresAt);
                                _logger.LogInformation("Access token actualizado para usuario {UsuarioId} (refresh token ya existía)", idUsuario);
                                return;
                            }
                        }
                        
                        throw new InvalidOperationException("No se recibió refresh_token. Asegúrate de incluir 'prompt=consent' en la URL de autorización.");
                    }

                    string refreshToken = refreshTokenElement.GetString();
                    string accessToken = tokenResponse.TryGetProperty("access_token", out var at) ? at.GetString() : null;
                    DateTime? expiresAt = accessToken != null ? DateTime.UtcNow.AddHours(1) : null;

                    await _repositorio.GuardarTokensAsync(idUsuario, refreshToken, accessToken, expiresAt);

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
        /// Obtiene el email del usuario desde la base de datos
        /// </summary>
        private async Task<string?> ObtenerEmailUsuarioAsync(int idUsuario)
        {
            return await _repositorio.ObtenerEmailUsuarioAsync(idUsuario);
        }

        /// <summary>
        /// Obtiene el tipo MIME según el filtro de tipo de archivo
        /// </summary>
        private string? ObtenerMimeType(string filtroTipo)
        {
            return filtroTipo.ToLower() switch
            {
                "pdf" => "application/pdf",
                "word" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "powerpoint" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "imagenes" => "image/",
                _ => null
            };
        }

        /// <summary>
        /// Obtiene el icono según el tipo MIME
        /// </summary>
        private string ObtenerIconoTipo(string? mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return "📄";

            if (mimeType.StartsWith("image/"))
                return "🖼️";
            if (mimeType == "application/pdf")
                return "📕";
            if (mimeType.Contains("wordprocessingml") || mimeType.Contains("msword"))
                return "📘";
            if (mimeType.Contains("spreadsheetml") || mimeType.Contains("excel"))
                return "📗";
            if (mimeType.Contains("presentationml") || mimeType.Contains("powerpoint"))
                return "📙";
            if (mimeType.Contains("text"))
                return "📝";

            return "📄";
        }
    }
}
