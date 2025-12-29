using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Aurora
    {
        public bool UseSecretsManager { get; set; } = false;
        public string Lectura { get; set; }
        public string Escritura { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveLectura { get; set; }
        public string SecretoClaveEscritura { get; set; }
    }
}
