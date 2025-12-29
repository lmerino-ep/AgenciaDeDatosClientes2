using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Zilliz
    {
        public bool UseSecretsManager { get; set; } = false;
        public int LimiteBulkInsert { get; set; }
        public string NombreColeccion { get; set; }
        public string AlgoritmoCalculoSimilitud { get; set; }
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveLimiteBulkInsert { get; set; }
        public string SecretoClaveNombreColeccion { get; set; }
        public string SecretoClaveAlgoritmoCalculoSimilitud { get; set; }
        public string SecretoClaveApiKey { get; set; }
        public string SecretoClaveEndpoint { get; set; }
    }
}
