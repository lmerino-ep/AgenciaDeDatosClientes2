using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Negocio;

namespace Controllers
{
    [Route("lemonsqueezy")]
    public class LemonSqueezyController : Controller
    {
        private readonly IServicioComprasResultados _servicioComprasResultados;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;
        private readonly ILogger<LemonSqueezyController> _logger;

        public LemonSqueezyController(
            IServicioComprasResultados servicioComprasResultados,
            UserManager<Modelos.Autenticacion.Usuario> userManager,
            ILogger<LemonSqueezyController> logger)
        {
            _servicioComprasResultados = servicioComprasResultados;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("checkout/{resultadoId:int}")]
        public async Task<IActionResult> CrearCheckout(int resultadoId)
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
                _logger.LogWarning(ex, "No se pudo crear el checkout para el resultado {ResultadoId}", resultadoId);
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signatureHeader = Request.Headers["X-Signature"].ToString();

            try
            {
                await _servicioComprasResultados.ProcesarWebhookAsync(payload, signatureHeader);
                return Ok();
            }
            catch (FirmaWebhookInvalidaException ex)
            {
                _logger.LogWarning(ex, "Webhook de LemonSqueezy rechazado: firma inválida.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando webhook de LemonSqueezy.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
