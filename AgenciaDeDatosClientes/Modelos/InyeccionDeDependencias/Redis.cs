using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class Redis
    {
        public bool UseSecretsManager { get; set; } = false;
        public string Host { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveHost { get; set; }
        public string SecretoClaveUsarSSL { get; set; }
        public string SecretoClaveSegundosTimeout { get; set; }
        public string SecretoClaveConnectTimeout { get; set; }
        public string SecretoClaveSyncTimeout { get; set; }
        public bool UsarSSL { get; set; }
        public int SegundosTimeout { get; set; }
        public int ConnectTimeout { get; set; }
        public int SyncTimeout { get; set; }
    }
}
