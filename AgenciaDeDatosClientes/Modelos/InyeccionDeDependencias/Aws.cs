using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.InyeccionDeDependencias
{
    /// <summary>
    /// Configuración de credenciales AWS
    /// </summary>
    public class Aws
    {
        /// <summary>
        /// Región de AWS
        /// </summary>
        public string Region { get; set; } = "eu-west-1";

        /// <summary>
        /// Access Key ID (solo para desarrollo)
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// Secret Access Key (solo para desarrollo)
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;
    }
}
