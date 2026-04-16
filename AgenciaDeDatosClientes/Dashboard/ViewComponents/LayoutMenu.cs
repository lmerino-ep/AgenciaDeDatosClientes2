using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Negocio;

namespace ViewComponents
{
    public class LayoutMenu : ViewComponent
    {
        private readonly IServicioSuscripciones _servicioSuscripciones;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LayoutMenu(IServicioSuscripciones servicioSuscripciones, IHttpContextAccessor httpContextAccessor)
        {
            _servicioSuscripciones = servicioSuscripciones;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var modelo = new LayoutMenuModel();
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out var fkUsuario))
                {
                    var detalle = await _servicioSuscripciones.ObtenerSuscripcionActivaDetalleAsync(fkUsuario);
                    if (detalle != null && !string.IsNullOrWhiteSpace(detalle.PlanNombre))
                    {
                        modelo.NombrePlanActivoCabecera = detalle.PlanNombre.Trim();
                    }
                }
            }

            return View(modelo);
        }
    }
}
