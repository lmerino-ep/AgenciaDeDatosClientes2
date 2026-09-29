namespace Modelos.DTO.ResultadosEmpresariales
{
    public class ResultadoParaCompra
    {
        public int ResultadoId { get; set; }

        public string EmpresaNombre { get; set; }

        public string SectorNombre { get; set; }

        public string PeriodoNombre { get; set; }

        public short Anio { get; set; }

        public int PrecioCentimos { get; set; }

        public bool Publicado { get; set; }

        public bool YaComprado { get; set; }

        public string Titulo => $"{EmpresaNombre} — {PeriodoNombre} {Anio}";
    }
}
