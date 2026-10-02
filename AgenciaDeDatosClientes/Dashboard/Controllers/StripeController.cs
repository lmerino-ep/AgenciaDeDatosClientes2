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
        private readonly IServicioComprasResultados _servicioComprasResultados;
        private readonly IServicioWebhookStripe _servicioWebhookStripe;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;
        private readonly ILogger<StripeController> _logger;

        public StripeController(
            IServicioSuscripciones servicioSuscripciones,
            IServicioComprasResultados servicioComprasResultados,
            IServicioWebhookStripe servicioWebhookStripe,
            UserManager<Modelos.Autenticacion.Usuario> userManager,
            ILogger<StripeController> logger)
        {
            _servicioSuscripciones = servicioSuscripciones;
            _servicioComprasResultados = servicioComprasResultados;
            _servicioWebhookStripe = servicioWebhookStripe;
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

        // GET puente sin mutación: solo renderiza un form que se auto-envía por POST al mismo origen (ver CrearCheckoutResultados).
        [Authorize]
        [HttpGet("checkout-resultados/{resultadoId:int}")]
        public IActionResult PuenteCheckoutResultados(int resultadoId)
        {
            var html = $"<!DOCTYPE html><html><body onload=\"document.forms[0].submit()\"><form method=\"post\" action=\"/stripe/checkout-resultados/{resultadoId}\"></form></body></html>";
            return Content(html, "text/html");
        }

        [Authorize]
        [HttpPost("checkout-resultados/{resultadoId:int}")]
        public async Task<IActionResult> CrearCheckoutResultados(int resultadoId)
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

            try
            {
                var checkoutUrl = await _servicioComprasResultados.CrearCheckoutAsync(
                    fkUsuario,
                    email,
                    resultadoId,
                    Request.Scheme,
                    Request.Host.ToString());

                return Redirect(checkoutUrl);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "No se pudo crear el checkout de Stripe para el resultado {ResultadoId}.", resultadoId);
                return BadRequest(ex.Message);
            }
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
                await _servicioWebhookStripe.ProcesarAsync(payload, signatureHeader);
                return Ok();
            }
            catch (FirmaWebhookInvalidaException ex)
            {
                _logger.LogWarning(ex, "Webhook de Stripe rechazado: firma inválida.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando webhook de Stripe.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
