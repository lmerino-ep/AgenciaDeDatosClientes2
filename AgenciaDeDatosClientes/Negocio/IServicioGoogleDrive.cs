using Modelos.DTO;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de Google Drive
    /// </summary>
    public interface IServicioGoogleDrive
    {
        /// <summary>
        /// Lista archivos en la carpeta configurada de Google Drive o en una carpeta específica
        /// </summary>
        Task<List<ArchivoGoogleDrive>> ListarArchivosAsync(int idUsuario, string? filtroTipo = null, string? busqueda = null, DateTime? fechaDesde = null, string? carpetaId = null);

        /// <summary>
        /// Obtiene información de una carpeta (nombre, ID, padres) para construir breadcrumbs
        /// </summary>
        Task<ArchivoGoogleDrive?> ObtenerInformacionCarpetaAsync(int idUsuario, string carpetaId);

        /// <summary>
        /// Obtiene la ruta completa de breadcrumbs desde la carpeta base hasta la carpeta especificada
        /// </summary>
        Task<List<ArchivoGoogleDrive>> ObtenerRutaBreadcrumbsAsync(int idUsuario, string carpetaId);

        /// <summary>
        /// Obtiene los tipos de archivo únicos presentes en la carpeta actual
        /// </summary>
        Task<List<string>> ObtenerTiposArchivoDisponiblesAsync(int idUsuario, string? carpetaId = null);

        /// <summary>
        /// Sube un archivo a Google Drive
        /// </summary>
        Task<string> SubirArchivoAsync(int idUsuario, Stream flujo, string nombreArchivo, string tipoMime);

        /// <summary>
        /// Descarga un archivo de Google Drive
        /// </summary>
        Task<(Stream Stream, string NombreArchivo, string TipoMime)> DescargarArchivoAsync(int idUsuario, string idArchivo);

        /// <summary>
        /// Elimina un archivo de Google Drive
        /// </summary>
        Task EliminarArchivoAsync(int idUsuario, string idArchivo);

        /// <summary>
        /// Sobrescribe un archivo existente en Google Drive
        /// </summary>
        Task SobrescribirArchivoAsync(int idUsuario, string idArchivo, Stream flujo, string tipoMime);

        /// <summary>
        /// Verifica los permisos del usuario en la carpeta de Google Drive
        /// </summary>
        Task<PermisosGoogleDrive> VerificarPermisosAsync(int idUsuario);

        /// <summary>
        /// Obtiene la URL de autorización de Google Drive para el usuario
        /// </summary>
        string ObtenerUrlAutorizacion(string redirectUri, string state);

        /// <summary>
        /// Intercambia un código de autorización por tokens de Google Drive
        /// </summary>
        Task IntercambiarCodigoPorTokensAsync(int idUsuario, string codigo, string redirectUri);
    }

    /// <summary>
    /// Permisos del usuario en Google Drive
    /// </summary>
    public class PermisosGoogleDrive
    {
        public bool PuedeLeer { get; set; }
        public bool PuedeEscribir { get; set; }
        public bool PuedeEliminar { get; set; }
        public bool TieneAcceso { get; set; }
        public string Mensaje { get; set; }
    }
}

