namespace Modelos.DTO
{
    /// <summary>
    /// DTO para un archivo o carpeta en S3
    /// </summary>
    public class ArchivoS3
    {
        public string Key { get; set; } // Ruta completa en S3 (ej: "documentacion/carpeta/archivo.pdf")
        public string Nombre { get; set; } // Nombre del archivo o carpeta
        public long Tamano { get; set; }
        public DateTime FechaModificacion { get; set; }
        public string TipoMime { get; set; }
        public string UrlWeb { get; set; } // URL pública de CloudFront o S3
        public string IconoTipo { get; set; } // Para mostrar icono según tipo
        public bool EsCarpeta { get; set; } // Indica si es una carpeta
        public string TamanoFormateado => EsCarpeta ? "-" : FormatearTamano(Tamano);
        public string NombreBase { get; set; } // Nombre sin versión (ej: "Contrato de negocio.xlsx")
        public int? NumeroVersion { get; set; } // Número de versión si tiene (ej: 5 para "_v005")

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

