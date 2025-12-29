namespace Modelos.DTO.Metricas
{
    /// <summary>
    /// Representa un KPI disponible en el sistema
    /// </summary>
    public class Kpi
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public List<ParametroKpi> Parametros { get; set; } = new List<ParametroKpi>();
    }
}
