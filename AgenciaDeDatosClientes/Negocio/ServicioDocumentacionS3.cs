using Microsoft.Extensions.Logging;
using Modelos.DTO;
using Modelos.InyeccionDeDependencias;
using Nuget.Helper;
using NugetPortable.Herramientas.CDN;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Negocio.Utils;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using Amazon.Runtime;

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de documentación en S3 con versionado automático
    /// </summary>
    public class ServicioDocumentacionS3 : IServicioDocumentacionS3
    {
        private readonly S3 _configuracionS3;
        private readonly ILogger<ServicioDocumentacionS3> _logger;
        private readonly HelperS3 _helperS3;
        private readonly IAmazonS3 _s3Client;
        private readonly string _carpetaBase = "documentacion";

        public ServicioDocumentacionS3(
            S3 configuracionS3,
            ILogger<ServicioDocumentacionS3> logger,
            Transparent transparentCDN)
        {
            _configuracionS3 = configuracionS3 ?? throw new ArgumentNullException(nameof(configuracionS3));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(configuracionS3.Bucket))
            {
                throw new InvalidOperationException("Bucket de S3 no está configurado");
            }

            if (string.IsNullOrEmpty(configuracionS3.AccessKeyId) || string.IsNullOrEmpty(configuracionS3.SecretAccessKey))
            {
                throw new InvalidOperationException("AccessKeyId y SecretAccessKey de S3 deben estar configurados");
            }

            _helperS3 = new HelperS3(
                configuracionS3.Bucket,
                configuracionS3.AccessKeyId,
                configuracionS3.CloudfrontDistributionId,
                configuracionS3.SecretAccessKey,
                transparentCDN
            );

            // Crear cliente S3 para subir archivos con ContentType personalizado
            var region = RegionEndpoint.EUWest1;
            AWSCredentials credentials = new BasicAWSCredentials(configuracionS3.AccessKeyId, configuracionS3.SecretAccessKey);
            AmazonS3Config s3Config = new AmazonS3Config
            {
                ServiceURL = "https://s3.amazonaws.com",
                RegionEndpoint = region,
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 3
            };
            _s3Client = new AmazonS3Client(credentials, s3Config);
        }

        /// <summary>
        /// Lista archivos y carpetas en la carpeta de documentación de S3 o en una carpeta específica
        /// Solo muestra la versión más reciente de cada archivo
        /// </summary>
        public async Task<List<ArchivoS3>> ListarArchivosAsync(int idUsuario, string? filtroTipo = null, string? busqueda = null, DateTime? fechaDesde = null, string? carpetaRuta = null)
        {
            try
            {
                _logger.LogInformation("Listando archivos de S3 para usuario {UsuarioId}. FiltroTipo: {FiltroTipo}, Búsqueda: {Busqueda}, FechaDesde: {FechaDesde}, CarpetaRuta: {CarpetaRuta}",
                    idUsuario, filtroTipo, busqueda, fechaDesde, carpetaRuta ?? "raíz");

                // Construir la ruta completa en S3
                string rutaCompleta = ConstruirRutaCompleta(carpetaRuta);
                if (!rutaCompleta.EndsWith("/"))
                {
                    rutaCompleta += "/";
                }

                // Obtener todos los objetos en la carpeta usando HelperS3
                List<string> objetosS3 = await _helperS3.GetArchivosEnCarpeta(rutaCompleta);

                // Procesar objetos: separar archivos y carpetas, y agrupar versiones
                Dictionary<string, ArchivoS3> archivosPorNombreBase = new Dictionary<string, ArchivoS3>();
                List<ArchivoS3> carpetas = new List<ArchivoS3>();

                foreach (string key in objetosS3)
                {
                    
                    // Omitir el objeto "carpeta/" mismo si existe
                    if (key == rutaCompleta || key == rutaCompleta.TrimEnd('/'))
                    {
                        continue;
                    }

                    // Obtener la ruta relativa desde la carpeta base
                    string rutaRelativa = key.Substring(_carpetaBase.Length).TrimStart('/');
                    
                    // Si estamos en una subcarpeta, obtener la ruta relativa desde esa subcarpeta
                    // Nota: S3 es case-sensitive, así que debemos comparar preservando el case original
                    if (!string.IsNullOrEmpty(carpetaRuta))
                    {
                        string prefijoCarpeta = carpetaRuta.TrimEnd('/') + "/";
                        // Comparar case-sensitive porque S3 es case-sensitive
                        if (!rutaRelativa.StartsWith(prefijoCarpeta, StringComparison.Ordinal))
                        {
                            continue; // Este archivo no está en la carpeta actual
                        }
                        rutaRelativa = rutaRelativa.Substring(prefijoCarpeta.Length);
                    }

                    // Determinar si es una carpeta (termina en /)
                    bool esCarpeta = key.EndsWith("/");
                    
                    if (esCarpeta)
                    {
                        // Es una carpeta
                        string nombreCarpeta = rutaRelativa.TrimEnd('/');
                        if (string.IsNullOrEmpty(nombreCarpeta) || nombreCarpeta.Contains("/"))
                        {
                            continue; // Es una subcarpeta anidada o la carpeta raíz
                        }

                        // Ocultar la carpeta "papelera" del listado
                        if (nombreCarpeta.Equals("papelera", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        carpetas.Add(new ArchivoS3
                        {
                            Key = key,
                            Nombre = nombreCarpeta,
                            Tamano = 0,
                            FechaModificacion = DateTime.UtcNow, // Las carpetas en S3 no tienen fecha de modificación
                            TipoMime = "application/vnd.google-apps.folder",
                            UrlWeb = ConstruirUrlS3(key),
                            IconoTipo = "carpeta",
                            EsCarpeta = true,
                            NombreBase = nombreCarpeta,
                            NumeroVersion = null
                        });
                    }
                    else
                    {
                        // Verificar que el archivo no esté en una subcarpeta
                        // Si la ruta relativa contiene "/", significa que está en una subcarpeta y no debe mostrarse
                        if (rutaRelativa.Contains("/"))
                        {
                            continue; // Este archivo está en una subcarpeta, no debe mostrarse
                        }

                        // Es un archivo - procesar versionado
                        var (nombreBase, numeroVersion) = ExtraerNombreBaseYVersion(rutaRelativa);
                        
                        // Solo mantener la versión más reciente de cada archivo
                        if (!archivosPorNombreBase.ContainsKey(nombreBase) || 
                            (numeroVersion.HasValue && 
                             (!archivosPorNombreBase[nombreBase].NumeroVersion.HasValue || 
                              numeroVersion.Value > archivosPorNombreBase[nombreBase].NumeroVersion.Value)))
                        {
                            // Obtener información del archivo desde S3 usando los metadatos del objeto
                            string extension = Path.GetExtension(nombreBase).TrimStart('.');
                            string tipoMime = ObtenerTipoMimePorExtension(extension);

                            // Obtener metadatos del archivo (tamaño y fecha de modificación)
                            var (tamano, fechaModificacion) = await ObtenerMetadatosArchivoAsync(key);

                            archivosPorNombreBase[nombreBase] = new ArchivoS3
                            {
                                Key = key,
                                Nombre = rutaRelativa, // Nombre completo con versión si aplica
                                Tamano = tamano,
                                FechaModificacion = fechaModificacion,
                                TipoMime = tipoMime,
                                UrlWeb = ConstruirUrlS3(key),
                                IconoTipo = GestorTiposArchivo.ObtenerIconoTipo(tipoMime),
                                EsCarpeta = false,
                                NombreBase = nombreBase,
                                NumeroVersion = numeroVersion
                            };
                        }
                    }
                }

                // Combinar carpetas y archivos
                List<ArchivoS3> resultado = new List<ArchivoS3>();
                resultado.AddRange(carpetas);
                resultado.AddRange(archivosPorNombreBase.Values);

                // Aplicar filtros
                if (!string.IsNullOrEmpty(filtroTipo))
                {
                    resultado = FiltrarPorTipo(resultado, filtroTipo);
                }

                if (!string.IsNullOrEmpty(busqueda))
                {
                    resultado = resultado.Where(a => 
                        a.NombreBase.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        a.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (fechaDesde.HasValue)
                {
                    resultado = resultado.Where(a => a.FechaModificacion >= fechaDesde.Value).ToList();
                }

                // Ordenar: primero carpetas, luego archivos, ambos alfabéticamente
                resultado = resultado
                    .OrderByDescending(a => a.EsCarpeta)
                    .ThenBy(a => a.NombreBase ?? a.Nombre, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _logger.LogInformation("Se encontraron {Cantidad} archivos en S3 para usuario {UsuarioId}", resultado.Count, idUsuario);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar archivos de S3 para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene la ruta completa de breadcrumbs desde la raíz hasta la carpeta especificada
        /// </summary>
        public async Task<List<ArchivoS3>> ObtenerRutaBreadcrumbsAsync(int idUsuario, string carpetaRuta)
        {
            try
            {
                List<ArchivoS3> breadcrumbs = new List<ArchivoS3>();

                // Añadir la raíz
                breadcrumbs.Add(new ArchivoS3
                {
                    Key = _carpetaBase + "/",
                    Nombre = "Documentación",
                    EsCarpeta = true,
                    UrlWeb = ConstruirUrlS3(_carpetaBase + "/")
                });

                // Si hay una carpeta específica, construir breadcrumbs
                if (!string.IsNullOrEmpty(carpetaRuta))
                {
                    string[] partes = carpetaRuta.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    string rutaAcumulada = "";

                    foreach (string parte in partes)
                    {
                        rutaAcumulada += parte + "/";
                        breadcrumbs.Add(new ArchivoS3
                        {
                            Key = ConstruirRutaCompleta(rutaAcumulada.TrimEnd('/')),
                            Nombre = parte,
                            EsCarpeta = true,
                            UrlWeb = ConstruirUrlS3(ConstruirRutaCompleta(rutaAcumulada.TrimEnd('/')))
                        });
                    }
                }

                return breadcrumbs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener breadcrumbs para usuario {UsuarioId} y carpeta {CarpetaRuta}", idUsuario, carpetaRuta);
                throw;
            }
        }

        /// <summary>
        /// Obtiene los tipos de archivo únicos presentes en la carpeta actual
        /// </summary>
        public async Task<List<string>> ObtenerTiposArchivoDisponiblesAsync(int idUsuario, string? carpetaRuta = null)
        {
            try
            {
                List<ArchivoS3> archivos = await ListarArchivosAsync(idUsuario, null, null, null, carpetaRuta);
                
                HashSet<string> tiposDisponibles = new HashSet<string>();
                
                foreach (var archivo in archivos.Where(a => !a.EsCarpeta))
                {
                    string? nombreFiltro = GestorTiposArchivo.ObtenerNombreFiltro(archivo.TipoMime);
                    if (!string.IsNullOrEmpty(nombreFiltro))
                    {
                        tiposDisponibles.Add(nombreFiltro);
                    }
                }

                return tiposDisponibles.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de archivo disponibles para usuario {UsuarioId} y carpeta {CarpetaRuta}", idUsuario, carpetaRuta ?? "raíz");
                return new List<string>();
            }
        }

        /// <summary>
        /// Sube un archivo nuevo a S3. Si el archivo ya existe, se añade automáticamente un número de versión.
        /// Si es un archivo nuevo, se añade _v001 al nombre.
        /// </summary>
        public async Task<string> SubirArchivoAsync(int idUsuario, Stream flujo, string nombreArchivo, string tipoMime, string? carpetaRuta = null)
        {
            try
            {
                _logger.LogInformation("Subiendo archivo a S3 para usuario {UsuarioId}: {NombreArchivo}, Tipo: {TipoMime}, Carpeta: {CarpetaRuta}",
                    idUsuario, nombreArchivo, tipoMime, carpetaRuta ?? "raíz");

                // Limpiar el nombre del archivo
                nombreArchivo = LimpiarNombreArchivo(nombreArchivo);

                // Construir la ruta completa
                string rutaCarpeta = ConstruirRutaCompleta(carpetaRuta);
                string nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo);
                string extension = Path.GetExtension(nombreArchivo);

                // Buscar si ya existe una versión de este archivo
                int siguienteVersion = await ObtenerSiguienteVersionAsync(rutaCarpeta, nombreBase, extension);

                // Construir el nombre final con versión
                string nombreFinal = siguienteVersion == 1 
                    ? $"{nombreBase}_v{siguienteVersion:D3}{extension}"
                    : $"{nombreBase}_v{siguienteVersion:D3}{extension}";

                string rutaCompleta = $"{rutaCarpeta}/{nombreFinal}".TrimStart('/');

                // Leer el contenido del stream
                byte[] contenido;
                using (var memoryStream = new MemoryStream())
                {
                    await flujo.CopyToAsync(memoryStream);
                    contenido = memoryStream.ToArray();
                }

                // Subir el archivo usando el SDK directamente para controlar el ContentType
                HttpStatusCode resultado = await SubirArchivoConContentTypeAsync(
                    contenido,
                    rutaCompleta,
                    tipoMime
                );

                if (resultado != HttpStatusCode.OK)
                {
                    throw new Exception($"Error al subir archivo a S3. Código HTTP: {resultado}");
                }

                _logger.LogInformation("Archivo subido exitosamente. Ruta: {RutaCompleta}, Versión: {Version}, Usuario: {UsuarioId}", 
                    rutaCompleta, siguienteVersion, idUsuario);
                
                return rutaCompleta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo {NombreArchivo} a S3 para usuario {UsuarioId}", nombreArchivo, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Descarga un archivo de S3
        /// </summary>
        public async Task<(Stream Stream, string NombreArchivo, string TipoMime)> DescargarArchivoAsync(int idUsuario, string key)
        {
            try
            {
                // Decodificar la URL (puede venir codificada desde la web: %2F -> /, %20 -> espacio, etc.)
                key = Uri.UnescapeDataString(key);
                
                _logger.LogInformation("Descargando archivo de S3 para usuario {UsuarioId}: {Key}", idUsuario, key);

                // Limpiar la ruta del archivo
                string rutaLimpia = key;
                if (rutaLimpia.StartsWith("/"))
                {
                    rutaLimpia = rutaLimpia.Substring(1);
                }

                // Obtener el contenido del archivo usando HelperS3
                // Nota: HelperS3.GetContenidoDeArchivo devuelve string, pero necesitamos bytes para archivos binarios
                // Por ahora, usaremos la URL firmada o pública para descargas directas
                // En una implementación completa, se podría usar GetPreSignedUrlRequest
                
                string nombreArchivo = Path.GetFileName(key);
                string extension = Path.GetExtension(nombreArchivo).TrimStart('.');
                string tipoMime = ObtenerTipoMimePorExtension(extension);

                // Para archivos binarios, redirigir a la URL pública o usar URL firmada
                // Por ahora, devolvemos un stream vacío y el controlador deberá redirigir a la URL
                // O mejor: usar GenerarUrlFirmadaDescarga si el archivo es privado
                string urlFirmada = _helperS3.GenerarUrlFirmadaDescarga(rutaLimpia);
                
                // Si no hay URL firmada, usar la URL pública
                if (string.IsNullOrEmpty(urlFirmada))
                {
                    urlFirmada = ConstruirUrlS3(key);
                }

                // Leer el contenido desde la URL (esto es una solución temporal)
                // En producción, sería mejor redirigir directamente a la URL
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var contenido = await httpClient.GetByteArrayAsync(urlFirmada);
                    MemoryStream memoryStream = new MemoryStream(contenido);
                    return (memoryStream, nombreArchivo, tipoMime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar archivo {Key} de S3 para usuario {UsuarioId}", key, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Elimina un archivo de S3 (elimina todas sus versiones) - Método legacy, usar VolverAVersionAnteriorAsync o EliminarCompletamenteAsync
        /// </summary>
        public async Task EliminarArchivoAsync(int idUsuario, string key)
        {
            await EliminarCompletamenteAsync(idUsuario, key);
        }

        /// <summary>
        /// Vuelve a la versión anterior de un archivo (elimina solo la versión actual)
        /// </summary>
        public async Task VolverAVersionAnteriorAsync(int idUsuario, string key)
        {
            try
            {
                // Decodificar la URL (puede venir codificada desde la web: %2F -> /, %20 -> espacio, etc.)
                key = Uri.UnescapeDataString(key);
                
                _logger.LogInformation("Volviendo a versión anterior para usuario {UsuarioId}: {Key}", idUsuario, key);

                // Extraer nombre base y extensión para buscar todas las versiones
                string nombreRelativo = key.Substring(key.LastIndexOf('/') + 1);
                var (nombreBase, versionActual) = ExtraerNombreBaseYVersion(nombreRelativo);
                string extension = Path.GetExtension(nombreBase);
                nombreBase = Path.GetFileNameWithoutExtension(nombreBase);

                // Construir la ruta de la carpeta
                string rutaCarpeta = key.Substring(0, key.LastIndexOf('/'));

                // Buscar todas las versiones del archivo
                List<string> objetosS3 = await _helperS3.GetArchivosEnCarpeta(rutaCarpeta);
                List<(string key, int version)> todasLasVersiones = objetosS3.Where(k => 
                {
                    if (k.EndsWith("/")) return false; // Omitir carpetas
                    string nombreArchivo = Path.GetFileName(k);
                    var (nb, version) = ExtraerNombreBaseYVersion(nombreArchivo);
                    return nb == nombreBase + extension && version.HasValue;
                }).Select(k =>
                {
                    string nombreArchivo = Path.GetFileName(k);
                    var (_, version) = ExtraerNombreBaseYVersion(nombreArchivo);
                    return (key: k, version: version.Value);
                }).OrderByDescending(v => v.version).ToList();

                // Verificar si hay versiones anteriores
                if (todasLasVersiones.Count == 0)
                {
                    throw new InvalidOperationException("No se encontraron versiones del archivo.");
                }

                // Verificar si la versión actual es la 1 o si es la única versión
                if (!versionActual.HasValue || versionActual.Value == 1 || todasLasVersiones.Count == 1)
                {
                    throw new InvalidOperationException("Este archivo está en su primera versión. No se puede volver a una versión anterior.");
                }

                // Verificar que existe una versión anterior
                bool existeVersionAnterior = todasLasVersiones.Any(v => v.version < versionActual.Value);
                if (!existeVersionAnterior)
                {
                    throw new InvalidOperationException("Este archivo está en su primera versión. No se puede volver a una versión anterior.");
                }

                // Mover la versión actual a la papelera antes de eliminarla (por si es necesario recuperarla)
                string rutaPapelera = $"{_carpetaBase}/papelera";
                await MoverArchivoAPapeleraAsync(key, rutaPapelera);

                // Eliminar la versión actual de su ubicación original
                HttpStatusCode resultado = await _helperS3.EliminarArchivo(key);
                if (resultado != HttpStatusCode.NoContent && resultado != HttpStatusCode.OK)
                {
                    throw new Exception($"Error al eliminar la versión actual. Código HTTP: {resultado}");
                }

                _logger.LogInformation("Versión actual movida a papelera y eliminada exitosamente. Key: {Key}, Versión eliminada: {Version}, Usuario: {UsuarioId}", 
                    key, versionActual.Value, idUsuario);
            }
            catch (InvalidOperationException)
            {
                // Re-lanzar InvalidOperationException sin modificar (son errores de negocio)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al volver a versión anterior {Key} de S3 para usuario {UsuarioId}", key, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Elimina completamente un archivo: mueve la última versión a papelera y elimina el resto permanentemente
        /// </summary>
        public async Task EliminarCompletamenteAsync(int idUsuario, string key)
        {
            try
            {
                // Decodificar la URL (puede venir codificada desde la web: %2F -> /, %20 -> espacio, etc.)
                key = Uri.UnescapeDataString(key);
                
                _logger.LogInformation("Eliminando completamente archivo de S3 para usuario {UsuarioId}: {Key}", idUsuario, key);

                // Extraer nombre base y extensión
                string nombreRelativo = key.Substring(key.LastIndexOf('/') + 1);
                var (nombreBase, _) = ExtraerNombreBaseYVersion(nombreRelativo);
                string extension = Path.GetExtension(nombreBase);
                nombreBase = Path.GetFileNameWithoutExtension(nombreBase);

                // Construir la ruta de la carpeta
                string rutaCarpeta = key.Substring(0, key.LastIndexOf('/'));

                // Buscar todas las versiones del archivo
                List<string> objetosS3 = await _helperS3.GetArchivosEnCarpeta(rutaCarpeta);
                List<string> todasLasVersiones = objetosS3.Where(k => 
                {
                    if (k.EndsWith("/")) return false; // Omitir carpetas
                    string nombreArchivo = Path.GetFileName(k);
                    var (nb, version) = ExtraerNombreBaseYVersion(nombreArchivo);
                    return nb == nombreBase + extension && version.HasValue;
                }).OrderByDescending(k =>
                {
                    string nombreArchivo = Path.GetFileName(k);
                    var (_, version) = ExtraerNombreBaseYVersion(nombreArchivo);
                    return version ?? 0;
                }).ToList();

                if (todasLasVersiones.Count == 0)
                {
                    _logger.LogWarning("No se encontraron versiones del archivo {Key}", key);
                    return;
                }

                // La última versión es la primera en la lista ordenada
                string ultimaVersionKey = todasLasVersiones.First();
                string rutaPapelera = $"{_carpetaBase}/papelera";

                // Mover la última versión a papelera (copiar y luego eliminar)
                await MoverArchivoAPapeleraAsync(ultimaVersionKey, rutaPapelera);

                // Eliminar todas las versiones permanentemente
                foreach (string versionKey in todasLasVersiones)
                {
                    HttpStatusCode resultado = await _helperS3.EliminarArchivo(versionKey);
                    if (resultado != HttpStatusCode.NoContent && resultado != HttpStatusCode.OK)
                    {
                        _logger.LogWarning("No se pudo eliminar la versión {VersionKey}. Código HTTP: {Codigo}", versionKey, resultado);
                    }
                }

                _logger.LogInformation("Archivo eliminado completamente. Key: {Key}, Versiones eliminadas: {Cantidad}, Última versión movida a papelera, Usuario: {UsuarioId}", 
                    key, todasLasVersiones.Count, idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar completamente archivo {Key} de S3 para usuario {UsuarioId}", key, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Mueve un archivo a la carpeta papelera (copia el archivo y luego lo elimina de la ubicación original)
        /// </summary>
        private async Task MoverArchivoAPapeleraAsync(string keyOrigen, string rutaPapelera)
        {
            try
            {
                // Asegurar que la carpeta papelera existe
                string rutaPapeleraConBarra = rutaPapelera.EndsWith("/") ? rutaPapelera : rutaPapelera + "/";
                await _helperS3.CrearCarpeta(rutaPapeleraConBarra, "public-read", null);

                // Obtener el nombre del archivo
                string nombreArchivo = Path.GetFileName(keyOrigen);
                string keyDestino = $"{rutaPapeleraConBarra}{nombreArchivo}";

                // Limpiar las rutas (eliminar / inicial si existe)
                string keyOrigenLimpia = keyOrigen.StartsWith("/") ? keyOrigen.Substring(1) : keyOrigen;
                string keyDestinoLimpia = keyDestino.StartsWith("/") ? keyDestino.Substring(1) : keyDestino;

                // Copiar el archivo a papelera
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _configuracionS3.Bucket,
                    SourceKey = keyOrigenLimpia,
                    DestinationBucket = _configuracionS3.Bucket,
                    DestinationKey = keyDestinoLimpia,
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.CopyObjectAsync(copyRequest);
                _logger.LogInformation("Archivo copiado a papelera: {KeyOrigen} -> {KeyDestino}", keyOrigen, keyDestino);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mover archivo a papelera: {KeyOrigen}", keyOrigen);
                throw;
            }
        }

        /// <summary>
        /// Sube una nueva versión de un archivo existente. Incrementa automáticamente el número de versión.
        /// </summary>
        public async Task<string> SubirNuevaVersionAsync(int idUsuario, string nombreBase, Stream flujo, string tipoMime, string? carpetaRuta = null)
        {
            try
            {
                _logger.LogInformation("Subiendo nueva versión a S3 para usuario {UsuarioId}: {NombreBase}, Tipo: {TipoMime}, Carpeta: {CarpetaRuta}",
                    idUsuario, nombreBase, tipoMime, carpetaRuta ?? "raíz");

                // Construir la ruta completa
                string rutaCarpeta = ConstruirRutaCompleta(carpetaRuta);
                string extension = Path.GetExtension(nombreBase);
                string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreBase);

                // Buscar la versión más reciente
                int siguienteVersion = await ObtenerSiguienteVersionAsync(rutaCarpeta, nombreSinExtension, extension);

                // Construir el nombre final con versión
                string nombreFinal = $"{nombreSinExtension}_v{siguienteVersion:D3}{extension}";
                string rutaCompleta = $"{rutaCarpeta}/{nombreFinal}".TrimStart('/');

                // Leer el contenido del stream
                byte[] contenido;
                using (var memoryStream = new MemoryStream())
                {
                    await flujo.CopyToAsync(memoryStream);
                    contenido = memoryStream.ToArray();
                }

                // Subir el archivo usando el SDK directamente para controlar el ContentType
                HttpStatusCode resultado = await SubirArchivoConContentTypeAsync(
                    contenido,
                    rutaCompleta,
                    tipoMime
                );

                if (resultado != HttpStatusCode.OK)
                {
                    throw new Exception($"Error al subir nueva versión a S3. Código HTTP: {resultado}");
                }

                _logger.LogInformation("Nueva versión subida exitosamente. Ruta: {RutaCompleta}, Versión: {Version}, Usuario: {UsuarioId}", 
                    rutaCompleta, siguienteVersion, idUsuario);
                
                return rutaCompleta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir nueva versión {NombreBase} a S3 para usuario {UsuarioId}", nombreBase, idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Verifica los permisos del usuario basados en sus roles
        /// </summary>
        public async Task<PermisosDocumentacionS3> VerificarPermisosAsync(ClaimsPrincipal user)
        {
            try
            {
                if (user == null || !(user.Identity?.IsAuthenticated ?? false))
                {
                    return new PermisosDocumentacionS3
                    {
                        PuedeLeer = false,
                        PuedeEscribir = false,
                        PuedeEliminar = false,
                        TieneAcceso = false,
                        Mensaje = "Usuario no autenticado"
                    };
                }

                // Cualquier usuario autenticado puede leer
                bool puedeLeer = true;

                // Solo usuarios con rol "Dashboard - Gestión de Documentación" o "Dashboard - Acceso total" pueden escribir y eliminar
                // Usar strings directamente en lugar de referencias a Utils.Authorization (capa de presentación)
                bool puedeEscribir = user.IsInRole("Dashboard - Gestión de Documentación") ||
                                     user.IsInRole("Dashboard - Acceso total");
                
                bool puedeEliminar = puedeEscribir;

                return new PermisosDocumentacionS3
                {
                    PuedeLeer = puedeLeer,
                    PuedeEscribir = puedeEscribir,
                    PuedeEliminar = puedeEliminar,
                    TieneAcceso = puedeLeer,
                    Mensaje = puedeLeer ? "Acceso concedido" : "No tienes acceso a la documentación"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar permisos");
                return new PermisosDocumentacionS3
                {
                    PuedeLeer = false,
                    PuedeEscribir = false,
                    PuedeEliminar = false,
                    TieneAcceso = false,
                    Mensaje = $"Error al verificar permisos: {ex.Message}"
                };
            }
        }

        #region Métodos privados

        /// <summary>
        /// Sube un archivo a S3 usando el SDK directamente para controlar el ContentType
        /// </summary>
        private async Task<HttpStatusCode> SubirArchivoConContentTypeAsync(byte[] contenido, string rutaCompleta, string tipoMime)
        {
            try
            {
                // Limpiar la ruta (eliminar / inicial si existe)
                if (rutaCompleta.StartsWith("/"))
                {
                    rutaCompleta = rutaCompleta.Substring(1);
                }

                var putRequest = new PutObjectRequest
                {
                    BucketName = _configuracionS3.Bucket,
                    Key = rutaCompleta,
                    InputStream = new MemoryStream(contenido),
                    ContentType = tipoMime,
                    CannedACL = S3CannedACL.PublicRead,
                    Headers = 
                    {
                        CacheControl = "public,max-age=2592000,s-maxage=2592000,post-check=2592000,pre-check=5184000"
                    }
                };

                var response = await _s3Client.PutObjectAsync(putRequest);
                return response.HttpStatusCode;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error de S3 al subir archivo {RutaCompleta}", rutaCompleta);
                return HttpStatusCode.InternalServerError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desconocido al subir archivo {RutaCompleta}", rutaCompleta);
                return HttpStatusCode.InternalServerError;
            }
        }

        private string ConstruirRutaCompleta(string? carpetaRuta)
        {
            if (string.IsNullOrEmpty(carpetaRuta))
            {
                return _carpetaBase;
            }
            
            return $"{_carpetaBase}/{carpetaRuta.TrimStart('/').TrimEnd('/')}";
        }

        private string ConstruirUrlS3(string key)
        {
            // Construir URL pública de S3 o CloudFront
            return $"https://{_configuracionS3.Bucket}/{key}";
        }

        /// <summary>
        /// Obtiene los metadatos (tamaño y fecha de modificación) de un archivo en S3
        /// </summary>
        private async Task<(long tamano, DateTime fechaModificacion)> ObtenerMetadatosArchivoAsync(string key)
        {
            try
            {
                // Limpiar la ruta (eliminar / inicial si existe)
                string keyLimpia = key.StartsWith("/") ? key.Substring(1) : key;

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _configuracionS3.Bucket,
                    Key = keyLimpia
                };

                var response = await _s3Client.GetObjectMetadataAsync(request);
                
                long tamano = response.ContentLength;
                DateTime fechaModificacion = response.LastModified.HasValue 
                    ? response.LastModified.Value.ToUniversalTime() 
                    : DateTime.UtcNow;

                return (tamano, fechaModificacion);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Archivo no encontrado, devolver valores por defecto
                _logger.LogWarning("Archivo no encontrado en S3: {Key}", key);
                return (0, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                // En caso de error, devolver valores por defecto y registrar el error
                _logger.LogWarning(ex, "Error al obtener metadatos del archivo {Key}, usando valores por defecto", key);
                return (0, DateTime.UtcNow);
            }
        }

        private (string nombreBase, int? numeroVersion) ExtraerNombreBaseYVersion(string nombreArchivo)
        {
            // Patrón: nombre_v###.ext o nombre.ext
            // Ejemplos: "Contrato_v005.xlsx" -> ("Contrato.xlsx", 5)
            //          "Documento.pdf" -> ("Documento.pdf", null)
            //          "Archivo_v001.docx" -> ("Archivo.docx", 1)
            
            // Buscar el patrón _v### antes de la extensión
            var match = Regex.Match(nombreArchivo, @"^(.+?)_v(\d{3})(\.[^.]+)?$");
            
            if (match.Success)
            {
                string nombreSinVersion = match.Groups[1].Value;
                string extension = match.Groups[3].Value ?? "";
                
                if (int.TryParse(match.Groups[2].Value, out int version))
                {
                    return (nombreSinVersion + extension, version);
                }
            }
            
            // Si no tiene versión, devolver el nombre completo
            return (nombreArchivo, null);
        }

        private async Task<int> ObtenerSiguienteVersionAsync(string rutaCarpeta, string nombreBase, string extension)
        {
            // Listar todos los archivos en la carpeta
            List<string> objetosS3 = await _helperS3.GetArchivosEnCarpeta(rutaCarpeta);
            
            int maxVersion = 0;
            string nombreCompleto = nombreBase + extension;
            
            foreach (string key in objetosS3)
            {
                string nombreArchivo = Path.GetFileName(key);
                var (nb, version) = ExtraerNombreBaseYVersion(nombreArchivo);
                
                if (nb == nombreCompleto && version.HasValue)
                {
                    maxVersion = Math.Max(maxVersion, version.Value);
                }
            }
            
            return maxVersion + 1;
        }

        private string LimpiarNombreArchivo(string nombreArchivo)
        {
            // Eliminar caracteres no válidos para nombres de archivo
            string nombreLimpio = Regex.Replace(nombreArchivo, @"[<>:""/\\|?*]", "_");
            return nombreLimpio.Trim();
        }

        private List<ArchivoS3> FiltrarPorTipo(List<ArchivoS3> archivos, string filtroTipo)
        {
            return archivos.Where(a =>
            {
                if (a.EsCarpeta)
                    return true; // Incluir todas las carpetas
                
                string? nombreFiltro = GestorTiposArchivo.ObtenerNombreFiltro(a.TipoMime);
                return nombreFiltro?.Equals(filtroTipo, StringComparison.OrdinalIgnoreCase) ?? false;
            }).ToList();
        }

        private string ObtenerTipoMimePorExtension(string extension)
        {
            return extension.ToLower() switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "doc" => "application/msword",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "xls" => "application/vnd.ms-excel",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "ppt" => "application/vnd.ms-powerpoint",
                "txt" => "text/plain",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}

