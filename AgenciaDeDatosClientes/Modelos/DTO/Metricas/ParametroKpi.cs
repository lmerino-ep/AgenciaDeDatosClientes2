namespace Modelos.DTO.Metricas
{
    /// <summary>
    /// Parámetro de un KPI
    /// </summary>
    public class ParametroKpi
    {
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "fecha", "numero", "texto", "booleano"
        public bool Requerido { get; set; }
        public string ValorPorDefecto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}

