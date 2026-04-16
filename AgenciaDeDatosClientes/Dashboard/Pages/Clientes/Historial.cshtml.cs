using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Suscripciones;
using Modelos.InyeccionDeDependencias;
using Negocio;
using System.Globalization;
using System.Security.Claims;

namespace Pages.Clientes
{
    [Authorize]
    public class HistorialModel : PageModel
    {
        private readonly IServicioSuscripciones _servicioSuscripciones;
        private readonly Configuracion _configuracion;

        public IReadOnlyList<HistorialDescargaItem> Descargas { get; private set; } = [];

        public HistorialModel(IServicioSuscripciones servicioSuscripciones, Configuracion configuracion)
        {
            _servicioSuscripciones = servicioSuscripciones;
            _configuracion = configuracion;
        }

        public string? UrlPublicaRepresentacion(HistorialDescargaItem item)
        {
            if (string.IsNullOrWhiteSpace(item.RepresentacionTituloUrl))
            {
                return null;
            }

            var baseUrl = _configuracion.UrlBasePublicacionEpData?.TrimEnd('/') ?? "https://www.epdata.es";
            var slug = item.RepresentacionTituloUrl.Trim().Trim('/');
            return $"{baseUrl}/{slug}/{item.RepresentacionGuid:D}";
        }

        public string FormatoInstanteUtcIso(DateTime fechaGuardadaEnUtc)
        {
            var utc = fechaGuardadaEnUtc.Kind switch
            {
                DateTimeKind.Utc => fechaGuardadaEnUtc,
                DateTimeKind.Local => fechaGuardadaEnUtc.ToUniversalTime(),
                _ => DateTime.SpecifyKind(fechaGuardadaEnUtc, DateTimeKind.Utc)
            };
            return utc.ToString("o", CultureInfo.InvariantCulture);
        }

        public async Task OnGetAsync()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var fkUsuario))
            {
                Descargas = [];
                return;
            }

            Descargas = await _servicioSuscripciones.ObtenerHistorialDescargasAsync(fkUsuario, 200);
        }
    }
}
