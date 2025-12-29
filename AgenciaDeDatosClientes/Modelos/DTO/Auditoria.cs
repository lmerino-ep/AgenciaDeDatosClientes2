namespace Modelos.DTO
{
    /// <summary>
    /// DTO para registrar una acción de auditoría
    /// </summary>
    public class Auditoria
    {
        public int FK_Usuario { get; set; }
        public string Accion { get; set; }
        public string Modulo { get; set; }
        public string Descripcion { get; set; }
        public string EntidadAfectada { get; set; }
        public int? IdEntidadAfectada { get; set; }
        public string CambiosRealizados { get; set; }
        public string DireccionIP { get; set; }
        public string UserAgent { get; set; }
        public int? DuracionMs { get; set; }
        public string Resultado { get; set; }
        public string MensajeError { get; set; }
    }
}
