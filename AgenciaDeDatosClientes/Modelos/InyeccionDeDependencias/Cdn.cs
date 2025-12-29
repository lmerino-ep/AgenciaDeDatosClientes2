using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Cdn
    {
        public bool UseSecretsManager { get; set; } = false;
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CompanyId { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveClientId { get; set; }
        public string SecretoClaveClientSecret { get; set; }
        public string SecretoClaveCompanyId { get; set; }
    }
}
