using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Negocio;

namespace Controllers
{
    [Route("stripe")]
    public class StripeController : Controller
    {
        private readonly IServicioSuscripciones _servicioSuscripciones;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;
        private readonly ILogger<StripeController> _logger;

        public StripeController(
            IServicioSuscripciones servicioSuscripciones,
            UserManager<Modelos.Autenticacion.Usuario> userManager,
            ILogger<StripeController> logger)
        {
            _servicioSuscripciones = servicioSuscripciones;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("checkout/{suscripcionTipoId:long}")]
        public async Task<IActionResult> CrearCheckout(long suscripcionTipoId)
        {
            var userIdString = _userManager.GetUserId(HttpContext.User);
            if (!int.TryParse(userIdString, out var fkUsuario))
            {
                return Unauthorized();
            }

            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("No se pudo resolver el email del usuario autenticado.");
            }

            var checkoutUrl = await _servicioSuscripciones.CrearSesionCheckoutAsync(
                fkUsuario,
                email,
                suscripcionTipoId,
                Request.Scheme,
                Request.Host.ToString());

            return Redirect(checkoutUrl);
        }

        [Authorize]
        [HttpGet("portal")]
        public async Task<IActionResult> Portal()
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var fkUsuario))
            {
                return Unauthorized();
            }

            var detalle = await _servicioSuscripciones.ObtenerSuscripcionActivaDetalleAsync(fkUsuario);
            if (detalle == null || string.IsNullOrWhiteSpace(detalle.StripeCustomerId))
            {
                return Redirect("/clientes/suscripciones");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var returnUrl = $"{baseUrl}/clientes/suscripciones";
            var portalUrl = await _servicioSuscripciones.CrearUrlPortalGestionSuscripcionAsync(detalle.StripeCustomerId, returnUrl);
            return Redirect(portalUrl);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                await _servicioSuscripciones.ProcesarWebhookAsync(payload, signatureHeader);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook de Stripe.");
                return BadRequest();
            }
        }
    }
}
