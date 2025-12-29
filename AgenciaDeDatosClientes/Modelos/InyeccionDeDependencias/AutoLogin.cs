using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class AutoLogin
    {
        public bool UseSecretsManager { get; set; } = false;
        public string NombreDelSecreto { get; set; }

        public string SecretoClaveSecretKeys { get; set; }
        public string SecretoClaveEndPoint { get; set; }
        public string SecretoClaveAccountName { get; set; }
        public string SecretoClaveAccountKey { get; set; }
        public string SecretoClaveStorageConnectionString { get; set; }
        public string SecretoClaveTableName { get; set; }

        public string[] SecretKeys { get; set; }
        public string EndPoint { get; set; }
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string StorageConnectionString { get; set; }
        public string TableName { get; set; }
    }
}
