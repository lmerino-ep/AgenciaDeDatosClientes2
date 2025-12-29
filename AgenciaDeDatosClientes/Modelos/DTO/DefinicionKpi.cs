namespace Modelos.DTO
{
    /// <summary>
    /// DTO para la definición de un KPI
    /// </summary>
    public class DefinicionKpi
    {
        public string KpiId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public List<ParametroKpi> Parametros { get; set; }
        public List<string> TablasOrigen { get; set; }
        public string Categoria { get; set; }
    }
}

