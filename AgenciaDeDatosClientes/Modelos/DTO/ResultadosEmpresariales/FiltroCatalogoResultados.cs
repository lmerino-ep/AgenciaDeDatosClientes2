using System;

namespace Modelos.DTO.ResultadosEmpresariales
{
    public class FiltroCatalogoResultados
    {
        public string? TextoBusqueda { get; set; }

        public int? FkSector { get; set; }

        public DateTime? FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }

        public OrdenCatalogoResultados Orden { get; set; } = OrdenCatalogoResultados.EmpresaAscendente;
    }
}
