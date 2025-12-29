using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Utils.Authorization
{
    /// <summary>
    /// Atributo personalizado que aplica la restricción de hosts permitidos usando la configuración centralizada
    /// </summary>
    public class AllowedHostsAttribute : ActionFilterAttribute
    {
        private static readonly string[] _allowedHosts = AllowedHosts.GetHosts();

        /// <summary>
        /// Constructor que establece una prioridad alta para que se ejecute temprano
        /// </summary>
        public AllowedHostsAttribute()
        {
            Order = -1000; // Prioridad muy alta
        }

        /// <summary>
        /// Aplica la validación de hosts antes de ejecutar la acción
        /// </summary>
        /// <param name="context">Contexto de la acción</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var host = context.HttpContext.Request.Host.Host;
            
            // Validación optimizada usando el array estático
            bool isAllowed = false;
            for (int i = 0; i < _allowedHosts.Length; i++)
            {
                if (string.Equals(_allowedHosts[i], host, StringComparison.OrdinalIgnoreCase))
                {
                    isAllowed = true;
                    break;
                }
            }
            
            if (!isAllowed)
            {
                context.Result = new BadRequestResult();
                return;
            }
            
            base.OnActionExecuting(context);
        }
    }
} 
