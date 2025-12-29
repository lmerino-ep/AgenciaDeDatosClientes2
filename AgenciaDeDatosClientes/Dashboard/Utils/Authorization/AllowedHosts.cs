namespace Utils.Authorization
{
    /// <summary>
    /// Clase estática que centraliza la configuración de hosts permitidos para la aplicación
    /// </summary>
    public static class AllowedHosts
    {
        /// <summary>
        /// Lista de hosts permitidos para el atributo [Host]
        /// </summary>
        public static readonly string[] Hosts = new[]
        {
            "clientes.epdata.es",
            "dev-clientes.epdata.es"
        };

        /// <summary>
        /// Obtiene la lista de hosts permitidos como array de strings
        /// </summary>
        /// <returns>Array con los hosts permitidos</returns>
        public static string[] GetHosts() => Hosts;

        /// <summary>
        /// Verifica si un host específico está en la lista de hosts permitidos
        /// Optimizado para rendimiento con bucle directo
        /// </summary>
        /// <param name="host">Host a verificar</param>
        /// <returns>True si el host está permitido, false en caso contrario</returns>
        public static bool IsHostAllowed(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;

            for (int i = 0; i < Hosts.Length; i++)
            {
                if (string.Equals(Hosts[i], host, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }
    }
} 
