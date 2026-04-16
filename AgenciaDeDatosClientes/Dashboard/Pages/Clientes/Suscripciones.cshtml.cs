using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.Actividad.Suscripciones;
using Modelos.DTO.Suscripciones;
using Modelos.Tarificacion;
using Negocio;
using System.Linq;
using System.Security.Claims;

namespace Pages.Clientes
{
    [Authorize]
    public class SuscripcionesModel : PageModel
    {
        private readonly IServicioSuscripciones _servicioSuscripciones;

        public bool EsTrabajadorEuropaPress { get; private set; }

        public bool TieneSuscripcionActiva { get; private set; }

        public GestionSuscripcionesEstado EstadoVista { get; private set; }

        public IReadOnlyList<SuscripcionTipoTarifa> PlanesDisponibles { get; private set; } = [];

        /// <summary>
        /// Plan de pago con facturación anual (p. ej. 12 €/año en Stripe).
        /// </summary>
        public SuscripcionTipoTarifa? PlanProfesionalAnual { get; private set; }

        /// <summary>
        /// Plan de pago con facturación mensual (p. ej. 1 €/mes en Stripe).
        /// </summary>
        public SuscripcionTipoTarifa? PlanProfesionalMensual { get; private set; }

        public SuscripcionActivaUsuario? SuscripcionActiva { get; private set; }

        public IReadOnlyList<FacturaStripeResumen> FacturasRecientes { get; private set; } = [];

        [BindProperty(SupportsGet = true, Name = SuscripcionesAvisoQuery.Parametro)]
        public string? Aviso { get; set; }

        public bool MostrarAvisoLimiteDescargas =>
            string.Equals(Aviso?.Trim(), SuscripcionesAvisoQuery.ValorLimiteDescargas, StringComparison.OrdinalIgnoreCase);

        public SuscripcionesModel(IServicioSuscripciones servicioSuscripciones)
        {
            _servicioSuscripciones = servicioSuscripciones;
        }

        public async Task OnGetAsync()
        {
            var usuarioTipo = User.FindFirstValue("FK_UsuarioTipo") ?? string.Empty;
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var fkUsuario))
            {
                EstadoVista = GestionSuscripcionesEstado.SinSuscripcionActiva;
                return;
            }

            var contextoTarifa = await _servicioSuscripciones.ObtenerContextoTarifaAsync(fkUsuario, usuarioTipo);
            EsTrabajadorEuropaPress = contextoTarifa.EsTrabajadorEuropaPress;
            TieneSuscripcionActiva = contextoTarifa.TieneSuscripcionActiva;

            if (EsTrabajadorEuropaPress)
            {
                EstadoVista = GestionSuscripcionesEstado.CuentaInternaEuropaPress;
                return;
            }

            if (TieneSuscripcionActiva)
            {
                SuscripcionActiva = await _servicioSuscripciones.ObtenerSuscripcionActivaDetalleAsync(fkUsuario);
                EstadoVista = GestionSuscripcionesEstado.ConSuscripcionStripe;
                if (SuscripcionActiva != null && !string.IsNullOrWhiteSpace(SuscripcionActiva.StripeCustomerId))
                {
                    FacturasRecientes = await _servicioSuscripciones.ObtenerFacturasRecientesAsync(SuscripcionActiva.StripeCustomerId);
                }

                return;
            }

            EstadoVista = GestionSuscripcionesEstado.SinSuscripcionActiva;
            PlanesDisponibles = await _servicioSuscripciones.ObtenerSuscripcionesDisponiblesAsync();
            (PlanProfesionalAnual, PlanProfesionalMensual) = ResolverPlanesProfesionalStripe(PlanesDisponibles);
        }

        /// <summary>
        /// Asocia filas de <see cref="SuscripcionTipo"/> a anual/mensual por nombre o, si no hay pistas, por <see cref="SuscripcionTipoTarifa.Id"/> ascendente (primero anual, segundo mensual).
        /// </summary>
        private static (SuscripcionTipoTarifa? Anual, SuscripcionTipoTarifa? Mensual) ResolverPlanesProfesionalStripe(
            IReadOnlyList<SuscripcionTipoTarifa> planes)
        {
            SuscripcionTipoTarifa? porNombreAnual = null;
            SuscripcionTipoTarifa? porNombreMensual = null;

            foreach (var p in planes)
            {
                var n = (p.Nombre ?? string.Empty).ToLowerInvariant();
                if (n.Contains("mensual"))
                {
                    if (n.Contains("premium"))
                    {
                        porNombreMensual = p;
                    }
                }
                else if (n.Contains("anual"))
                {
                    if (n.Contains("premium"))
                    {
                        porNombreAnual = p;
                    }
                }
            }

            var ordenados = planes.OrderBy(p => p.Id).ToList();
            SuscripcionTipoTarifa? anual = porNombreAnual;
            SuscripcionTipoTarifa? mensual = porNombreMensual;

            if (anual == null && ordenados.Count > 0)
            {
                anual = ordenados[0];
            }

            if (mensual == null && ordenados.Count > 1)
            {
                mensual = ordenados[1];
            }
            else if (mensual == null && ordenados.Count == 1)
            {
                mensual = ordenados[0];
            }

            if (anual != null && mensual != null
                && anual.Id == mensual.Id
                && ordenados.Count > 1)
            {
                mensual = ordenados.FirstOrDefault(p => p.Id != anual.Id) ?? mensual;
            }

            return (anual, mensual);
        }
    }
}
