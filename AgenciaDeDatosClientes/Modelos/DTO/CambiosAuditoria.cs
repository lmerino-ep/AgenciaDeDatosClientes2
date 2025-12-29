namespace Modelos.DTO
{
    /// <summary>
    /// DTO con los cambios realizados parseados
    /// </summary>
    public class CambiosAuditoria
    {
        public Dictionary<string, object> Antes { get; set; }
        public Dictionary<string, object> Despues { get; set; }
    }
}

