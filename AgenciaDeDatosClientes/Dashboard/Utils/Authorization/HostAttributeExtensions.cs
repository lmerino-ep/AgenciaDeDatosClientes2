using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Utils.Authorization
{
    /// <summary>
    /// Extensiones para facilitar el uso de la configuración centralizada de hosts permitidos
    /// </summary>
    public static class HostAttributeExtensions
    {
        /// <summary>
        /// Crea un atributo Host con los hosts permitidos desde la configuración centralizada
        /// </summary>
        /// <returns>Atributo Host configurado con los hosts permitidos</returns>
        public static HostAttribute CreateAllowedHostsAttribute()
        {
            return new HostAttribute(AllowedHosts.GetHosts());
        }

        /// <summary>
        /// Obtiene los hosts permitidos como parámetros para el atributo Host
        /// </summary>
        /// <returns>Array de strings con los hosts permitidos</returns>
        public static string[] GetAllowedHosts()
        {
            return AllowedHosts.GetHosts();
        }
    }
} 