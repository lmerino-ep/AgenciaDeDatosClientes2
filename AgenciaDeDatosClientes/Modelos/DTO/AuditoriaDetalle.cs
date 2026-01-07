namespace Modelos.DTO
{
    /// <summary>
    /// DTO para el detalle completo de una acción de auditoría
    /// </summary>
    public class AuditoriaDetalle
    {
        public long Id { get; set; }
        public int FK_Usuario { get; set; }
        public string AccionNombre { get; set; }
        public string CategoriaNombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime LastUpdate { get; set; }
        
        // Propiedades de compatibilidad (obsoletas)
        [Obsolete("Usar AccionNombre en su lugar")]
        public string Accion => AccionNombre;
        
        [Obsolete("Usar CategoriaNombre en su lugar")]
        public string Modulo => CategoriaNombre;
        
        [Obsolete("Usar LastUpdate en su lugar")]
        public DateTime FechaHora => LastUpdate;
    }
}

