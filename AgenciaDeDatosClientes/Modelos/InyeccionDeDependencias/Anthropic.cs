using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Anthropic
    {
        public bool UseSecretsManager { get; set; } = false;
        public string NombreDelSecreto { get; set; }

        public string SecretoClaveModelo { get; set; }
        public string SecretoClaveClave { get; set; }
        public string SecretoClaveEndpoint { get; set; }

        public string Modelo {  get; set; }
        public string Clave {  get; set; }
        public string Endpoint {  get; set; }
    }
}
