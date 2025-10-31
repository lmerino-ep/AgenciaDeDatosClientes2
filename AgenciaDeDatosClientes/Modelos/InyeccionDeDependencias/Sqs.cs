using NugetPortable.SQS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Sqs
    {
        public Colas ColasSerieTipo { get; set; }
        public Colas ColasRepresentacion { get; set; }
        public Colas ColasPlantillaWeb { get; set; }
        public Colas ColasCargaInicial { get; set; }
        public Colas ColasParsers { get; set; }
        public int NumeroMaximoDeMensajesPorRequest { get; set; } = 10; //10 es el máximo que podemos leer en una sola request contra el SQS de AWS
        public int LimiteDeSegundosDeEsperaHastaRecibirMensajes { get; set; } = 5; //20 es el máximo de segundos que podemos esperar a ver si entra algún mensaje en la cola que podamos consumir
        public int SegundosOcultoParaSucesivasRequestsDespuesDeHaberSidoConsultadoPorNuestroProceso { get; set; }
        public int SegundosDeEsperaSiNoHayMensajesQueProcesar { get; set; }
    }
}
