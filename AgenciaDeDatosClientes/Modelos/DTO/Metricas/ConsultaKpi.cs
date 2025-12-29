namespace Modelos.DTO.Metricas
{
    /// <summary>
    /// Solicitud de consulta de un KPI
    /// </summary>
    public class ConsultaKpi
    {
        public string KpiId { get; set; } = string.Empty;
        public Dictionary<string, object> Parametros { get; set; } = new Dictionary<string, object>();
        public bool ForzarRecalculo { get; set; } = false; // Ignorar caché
    }
}
