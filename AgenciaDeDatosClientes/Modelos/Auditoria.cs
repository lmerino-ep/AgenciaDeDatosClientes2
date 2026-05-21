namespace Modelos
{
    /// <summary>
    /// Entidad que representa un registro de auditoría de acciones administrativas en el dashboard
    /// </summary>
    public class Auditoria
    {
        /// <summary>
        /// Identificador único del registro de auditoría
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Descripción legible de la acción (lo modificado)
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// ID del usuario que realizó la acción
        /// </summary>
        public int FK_Usuario { get; set; }

        /// <summary>
        /// ID del tipo de acción de auditoría
        /// </summary>
        public int FK_AuditoriaAccion { get; set; }

        /// <summary>
        /// Fecha y hora de última actualización con precisión de milisegundos
        /// </summary>
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Resultados de acciones de auditoría
    /// </summary>
    public static class ResultadosAuditoria
    {
        public const string Exito = "Exito";
        public const string Error = "Error";
        public const string NoAutorizado = "NoAutorizado";
    }
}

