using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    public class S3
    {
        public bool UseSecretsManager { get; set; } = false;
        public string Bucket { get; set; }
        public string AccessKeyId { get; set; }
        public string CloudfrontDistributionId { get; set; }
        public string SecretAccessKey { get; set; }
        public string NombreDelSecreto { get; set; }
        public string SecretoClaveBucket { get; set; }
        public string SecretoClaveAccessKeyId { get; set; }
        public string SecretoClaveCloudfrontDistributionId { get; set; }
        public string SecretoClaveSecretAccessKey { get; set; }
    }
}
