namespace Modelos.DTO
{
    /// <summary>
    /// DTO para un archivo de Google Drive
    /// </summary>
    public class ArchivoGoogleDrive
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public long Tamano { get; set; }
        public DateTime FechaModificacion { get; set; }
        public string TipoMime { get; set; }
        public string UrlWeb { get; set; }
        public string IconoTipo { get; set; } // Para mostrar icono según tipo
        public string TamanoFormateado => FormatearTamano(Tamano);

        private string FormatearTamano(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024:F2} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024 * 1024):F2} MB";
            return $"{bytes / (1024 * 1024 * 1024):F2} GB";
        }
    }
}
