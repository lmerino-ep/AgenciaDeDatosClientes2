using AutoLogin;
using AutoLogin.Token;
using AutoLogin.Verification.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{
    public class AutoLoginController : Controller
    {
        private readonly ITokenHandler _tokenHandler;
        private readonly ITokenDbService _tokenDbService;
        private readonly SignInManager<Modelos.Autenticacion.Usuario> _signInManager;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;
        private readonly ILogger<AutoLoginController> _logger;

        public AutoLoginController(
            ITokenHandler tokenHandler,
            ITokenDbService tokenDbService,
            SignInManager<Modelos.Autenticacion.Usuario> signInManager,
            UserManager<Modelos.Autenticacion.Usuario> userManager,
            ILogger<AutoLoginController> logger)
        {
            _tokenHandler = tokenHandler;
            _tokenDbService = tokenDbService;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("/auth")]
        public async Task<IActionResult> Auth([FromQuery] string t, [FromQuery] string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return Redirect("/");
            }

            try
            {
                var token = _tokenHandler.ReadToken(t);
                if (token == null || !await _tokenDbService.CanVerifyTokenAsync(token, Audience.EpData))
                {
                    return Redirect("/");
                }

                if (User.Identity?.IsAuthenticated ?? false)
                {
                    await _signInManager.SignOutAsync();
                }

                if (token.UserId <= 0)
                {
                    return Redirect("/");
                }

                var usuario = await _userManager.FindByIdAsync(token.UserId.ToString());
                if (usuario == null)
                {
                    return Redirect("/");
                }

                await _tokenDbService.AddOrUpdateTokenAsync(token);
                await _signInManager.SignInAsync(usuario, true);

                if (EsReturnUrlSeguroParaClientes(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en autologin de clientes.");
                return Redirect("/");
            }
        }

        /// <summary>
        /// Evita redirecciones abiertas: solo rutas relativas al sitio, sin protocolo ni "//".
        /// </summary>
        private static bool EsReturnUrlSeguroParaClientes(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            returnUrl = returnUrl.Trim();
            if (!returnUrl.StartsWith('/'))
            {
                return false;
            }

            if (returnUrl.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            if (returnUrl.Contains("..", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
