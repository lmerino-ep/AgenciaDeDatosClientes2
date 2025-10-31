using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Configuracion
    {
        public int MaxDegreeOfParallelism { get; set; }
        public int SegundosETagParaSitemap { get; set; }
        public int LimiteUrlsPorArchivoDeSitemap { get; set; }
        public int LimiteSeriesParaRealizarCargaAsincrona { get; set; }
    }
}
