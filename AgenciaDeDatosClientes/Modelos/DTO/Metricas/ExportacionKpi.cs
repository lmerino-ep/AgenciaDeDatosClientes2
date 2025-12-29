namespace Modelos.DTO.Metricas
{
    /// <summary>
    /// Solicitud de exportación de un KPI
    /// </summary>
    public class ExportacionKpi
    {
        public string KpiId { get; set; } = string.Empty;
        public Dictionary<string, object> Parametros { get; set; } = new Dictionary<string, object>();
        public string Formato { get; set; } = "json"; // "json" o "csv"
        public string? NombreArchivo { get; set; }
    }
}
