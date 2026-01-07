namespace Negocio.Utils
{
    /// <summary>
    /// Gestor centralizado para la gestión de tipos de archivo y sus equivalencias
    /// Maneja mapeos entre tipos MIME, filtros, iconos y consultas de Google Drive
    /// </summary>
    public static class GestorTiposArchivo
    {
        /// <summary>
        /// Categorías de tipos de archivo disponibles para filtrado
        /// </summary>
        public enum CategoriaFiltro
        {
            Word,
            Excel,
            PowerPoint,
            Pdf,
            Texto,
            Imagenes
        }

        /// <summary>
        /// Obtiene la categoría de filtro correspondiente a un tipo MIME
        /// </summary>
        public static CategoriaFiltro? ObtenerCategoriaFiltro(string tipoMime)
        {
            if (string.IsNullOrEmpty(tipoMime))
                return null;

            // Word (Microsoft y Google Docs)
            if (EsWord(tipoMime))
                return CategoriaFiltro.Word;

            // Excel (Microsoft y Google Sheets)
            if (EsExcel(tipoMime))
                return CategoriaFiltro.Excel;

            // PowerPoint (Microsoft y Google Slides)
            if (EsPowerPoint(tipoMime))
                return CategoriaFiltro.PowerPoint;

            // PDF
            if (tipoMime == "application/pdf")
                return CategoriaFiltro.Pdf;

            // Texto
            if (EsTexto(tipoMime))
                return CategoriaFiltro.Texto;

            // Imágenes
            if (tipoMime.StartsWith("image/"))
                return CategoriaFiltro.Imagenes;

            return null;
        }

        /// <summary>
        /// Obtiene el nombre del filtro (string) correspondiente a un tipo MIME
        /// </summary>
        public static string? ObtenerNombreFiltro(string tipoMime)
        {
            var categoria = ObtenerCategoriaFiltro(tipoMime);
            return categoria?.ToString().ToLower();
        }

        /// <summary>
        /// Obtiene el nombre del filtro a partir de una categoría
        /// </summary>
        public static string ObtenerNombreFiltro(CategoriaFiltro categoria)
        {
            return categoria.ToString().ToLower();
        }

        /// <summary>
        /// Construye la consulta de filtrado para Google Drive Query Language
        /// </summary>
        public static string? ConstruirConsultaFiltro(string nombreFiltro)
        {
            if (string.IsNullOrEmpty(nombreFiltro))
                return null;

            nombreFiltro = nombreFiltro.ToLower();

            switch (nombreFiltro)
            {
                case "word":
                    // Word (Microsoft) y Google Docs
                    return "(mimeType contains 'wordprocessingml' or " +
                           "mimeType contains 'msword' or " +
                           "mimeType = 'application/vnd.google-apps.document')";

                case "excel":
                    // Excel (Microsoft) y Google Sheets
                    return "(mimeType contains 'spreadsheetml' or " +
                           "mimeType contains 'excel' or " +
                           "mimeType = 'application/vnd.google-apps.spreadsheet')";

                case "powerpoint":
                    // PowerPoint (Microsoft) y Google Slides
                    return "(mimeType contains 'presentationml' or " +
                           "mimeType contains 'powerpoint' or " +
                           "mimeType = 'application/vnd.google-apps.presentation')";

                case "pdf":
                    return "mimeType = 'application/pdf'";

                case "texto":
                    // Archivos de texto (.txt y equivalentes)
                    return "(mimeType = 'text/plain' or " +
                           "mimeType = 'text/txt' or " +
                           "mimeType = 'text/csv' or " +
                           "mimeType = 'text/xml' or " +
                           "mimeType = 'text/json' or " +
                           "mimeType = 'text/sql' or " +
                           "mimeType = 'text/x-sql')";

                case "imagenes":
                    // Google Drive Query no soporta startsWith, así que usamos contains
                    // Esto buscará cualquier tipo MIME que contenga 'image/'
                    return "mimeType contains 'image/'";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Obtiene el nombre de la clase CSS del icono según el tipo MIME
        /// </summary>
        public static string ObtenerIconoTipo(string? mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return "archivo-generico";

            // Carpetas de Google Drive
            if (mimeType == "application/vnd.google-apps.folder")
                return "carpeta";

            // Imágenes
            if (mimeType.StartsWith("image/"))
                return "imagen";

            // PDF
            if (mimeType == "application/pdf")
                return "pdf";

            // Word (Microsoft y Google Docs)
            if (EsWord(mimeType))
                return "word";

            // Excel (Microsoft y Google Sheets)
            if (EsExcel(mimeType))
                return "excel";

            // PowerPoint (Microsoft y Google Slides)
            if (EsPowerPoint(mimeType))
                return "powerpoint";

            // JSON
            if (mimeType == "application/json" || mimeType == "text/json")
                return "json";

            // SQL
            if (mimeType == "application/sql" || mimeType == "text/sql" || mimeType == "text/x-sql")
                return "sql";

            // XML
            if (mimeType == "application/xml" || mimeType == "text/xml")
                return "xml";

            // CSV
            if (mimeType == "text/csv" || mimeType == "application/csv")
                return "csv";

            // Texto (incluye .txt y otros tipos de texto)
            // Usar EsTexto para mantener consistencia con la lógica de filtrado
            if (EsTexto(mimeType))
                return "texto";

            // Archivo genérico por defecto
            return "archivo-generico";
        }

        /// <summary>
        /// Verifica si un tipo MIME corresponde a Word (Microsoft o Google Docs)
        /// </summary>
        public static bool EsWord(string tipoMime)
        {
            if (string.IsNullOrEmpty(tipoMime))
                return false;

            return tipoMime.Contains("wordprocessingml") ||
                   tipoMime.Contains("msword") ||
                   tipoMime == "application/vnd.google-apps.document";
        }

        /// <summary>
        /// Verifica si un tipo MIME corresponde a Excel (Microsoft o Google Sheets)
        /// </summary>
        public static bool EsExcel(string tipoMime)
        {
            if (string.IsNullOrEmpty(tipoMime))
                return false;

            return tipoMime.Contains("spreadsheetml") ||
                   tipoMime.Contains("excel") ||
                   tipoMime == "application/vnd.google-apps.spreadsheet";
        }

        /// <summary>
        /// Verifica si un tipo MIME corresponde a PowerPoint (Microsoft o Google Slides)
        /// </summary>
        public static bool EsPowerPoint(string tipoMime)
        {
            if (string.IsNullOrEmpty(tipoMime))
                return false;

            return tipoMime.Contains("presentationml") ||
                   tipoMime.Contains("powerpoint") ||
                   tipoMime == "application/vnd.google-apps.presentation";
        }

        /// <summary>
        /// Verifica si un tipo MIME corresponde a un archivo de texto
        /// </summary>
        public static bool EsTexto(string tipoMime)
        {
            if (string.IsNullOrEmpty(tipoMime) || !tipoMime.StartsWith("text/"))
                return false;

            // Incluir tipos de texto comunes, excluyendo HTML, CSS, JavaScript
            return tipoMime == "text/plain" ||
                   tipoMime == "text/txt" ||
                   tipoMime == "text/csv" ||
                   tipoMime == "text/xml" ||
                   tipoMime == "text/json" ||
                   tipoMime == "text/sql" ||
                   tipoMime == "text/x-sql";
        }

        /// <summary>
        /// Obtiene todos los tipos MIME que corresponden a una categoría de filtro
        /// </summary>
        public static List<string> ObtenerTiposMimePorCategoria(CategoriaFiltro categoria)
        {
            return categoria switch
            {
                CategoriaFiltro.Word => new List<string>
                {
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/msword",
                    "application/vnd.google-apps.document"
                },
                CategoriaFiltro.Excel => new List<string>
                {
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/vnd.ms-excel",
                    "application/vnd.google-apps.spreadsheet"
                },
                CategoriaFiltro.PowerPoint => new List<string>
                {
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    "application/vnd.ms-powerpoint",
                    "application/vnd.google-apps.presentation"
                },
                CategoriaFiltro.Pdf => new List<string> { "application/pdf" },
                CategoriaFiltro.Texto => new List<string>
                {
                    "text/plain",
                    "text/txt",
                    "text/csv",
                    "text/xml",
                    "text/json",
                    "text/sql",
                    "text/x-sql"
                },
                CategoriaFiltro.Imagenes => new List<string>(), // Se maneja con startsWith
                _ => new List<string>()
            };
        }
    }
}

