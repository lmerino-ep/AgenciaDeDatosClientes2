namespace Modelos.DTO
{
    /// <summary>
    /// DTO para un parámetro de KPI
    /// </summary>
    public class ParametroKpi
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; } // String, Int, Date, Boolean
        public bool Requerido { get; set; }
        public object ValorPorDefecto { get; set; }
        public string Descripcion { get; set; }
    }
}

