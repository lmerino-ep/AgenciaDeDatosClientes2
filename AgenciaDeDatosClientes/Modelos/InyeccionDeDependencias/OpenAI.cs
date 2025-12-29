using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class OpenAI
    {
        public bool UseSecretsManager { get; set; } = false;
        public int DimensionesVector { get; set; }
        public int SegundosTimeOut { get; set; }
        public string Modelo { get; set; }
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveDimensionesVector { get; set; }
        public string SecretoClaveSegundosTimeOut { get; set; }
        public string SecretoClaveModelo { get; set; }
        public string SecretoClaveApiKey { get; set; }
        public string SecretoClaveEndpoint { get; set; }
    }
}
