using Modelos.DTO;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de documentación en S3
    /// </summary>
    public interface IServicioDocumentacionS3
    {
        /// <summary>
        /// Lista archivos y carpetas en la carpeta de documentación de S3 o en una carpeta específica
        /// Solo muestra la versión más reciente de cada archivo
        /// </summary>
        Task<List<ArchivoS3>> ListarArchivosAsync(int idUsuario, string? filtroTipo = null, string? busqueda = null, DateTime? fechaDesde = null, string? carpetaRuta = null);

        /// <summary>
        /// Obtiene la ruta completa de breadcrumbs desde la raíz hasta la carpeta especificada
        /// </summary>
        Task<List<ArchivoS3>> ObtenerRutaBreadcrumbsAsync(int idUsuario, string carpetaRuta);

        /// <summary>
        /// Obtiene los tipos de archivo únicos presentes en la carpeta actual
        /// </summary>
        Task<List<string>> ObtenerTiposArchivoDisponiblesAsync(int idUsuario, string? carpetaRuta = null);

        /// <summary>
        /// Sube un archivo nuevo a S3. Si el archivo ya existe, se añade automáticamente un número de versión.
        /// Si es un archivo nuevo, se añade _v001 al nombre.
        /// </summary>
        Task<string> SubirArchivoAsync(int idUsuario, Stream flujo, string nombreArchivo, string tipoMime, string? carpetaRuta = null);

        /// <summary>
        /// Descarga un archivo de S3
        /// </summary>
        Task<(Stream Stream, string NombreArchivo, string TipoMime)> DescargarArchivoAsync(int idUsuario, string key);

        /// <summary>
        /// Elimina un archivo de S3 (elimina todas sus versiones)
        /// </summary>
        Task EliminarArchivoAsync(int idUsuario, string key);

        /// <summary>
        /// Vuelve a la versión anterior de un archivo (elimina la versión actual)
        /// </summary>
        Task VolverAVersionAnteriorAsync(int idUsuario, string key);

        /// <summary>
        /// Elimina completamente un archivo: mueve la última versión a papelera y elimina el resto permanentemente
        /// </summary>
        Task EliminarCompletamenteAsync(int idUsuario, string key);

        /// <summary>
        /// Sube una nueva versión de un archivo existente. Incrementa automáticamente el número de versión.
        /// </summary>
        Task<string> SubirNuevaVersionAsync(int idUsuario, string nombreBase, Stream flujo, string tipoMime, string? carpetaRuta = null);

        /// <summary>
        /// Verifica los permisos del usuario basados en sus roles
        /// </summary>
        Task<PermisosDocumentacionS3> VerificarPermisosAsync(System.Security.Claims.ClaimsPrincipal user);
    }

    /// <summary>
    /// Permisos del usuario en la documentación S3 basados en roles
    /// </summary>
    public class PermisosDocumentacionS3
    {
        public bool PuedeLeer { get; set; }
        public bool PuedeEscribir { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool TieneAcceso { get; set; }
        public string Mensaje { get; set; }
    }
}

