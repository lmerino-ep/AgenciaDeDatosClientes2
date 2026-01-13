using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NugetPortable.Cache;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Pages
{
    [ResponseCache(CacheProfileName = "NoCache")]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly SignInManager<Modelos.Autenticacion.Usuario> _signInManager;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;
        private readonly HtmlSanitizer _sanitizerContraXss;

        #region Propiedades página
        [BindProperty(SupportsGet = true)]
        public string? returnUrl { get; set; } //Lo ponemos nulable para que sea opcional
        ///////////////
        [BindProperty]
        [DataType(DataType.EmailAddress, ErrorMessage = "Introduzca una dirección de email válida")]
        [Required(ErrorMessage = "El email del usuario es obligatorio")]
        public string Email { get; set; }
        [BindProperty, DataType(DataType.Password)]
        [Required(ErrorMessage = "La constraseña es obligatoria")]
        public string Password { get; set; }
        [BindProperty]
        public bool Recordar { get; set; }
        #endregion


        public LoginModel(ILogger<LoginModel> logger, IRedis redis, SignInManager<Modelos.Autenticacion.Usuario> signInManager, UserManager<Modelos.Autenticacion.Usuario> userManager, HtmlSanitizer sanitizerContraXss)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _sanitizerContraXss = sanitizerContraXss;
        }

        public async Task<ActionResult> OnGetAsync()
        {
            try
            {
                if (User?.Identity?.IsAuthenticated ?? false)
                {
                    return new RedirectResult(Url.Content(returnUrl) ?? Url.Content("~/"));
                }
                else
                {
                    return Page();
                }
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var sanitizedEmail = _sanitizerContraXss.Sanitize(Email);
                    // NO sanitizar la contraseña - puede modificar caracteres especiales válidos
                    // La contraseña se envía por HTTPS y no se muestra en HTML, por lo que no necesita sanitización XSS
                    Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
                        sanitizedEmail,
                        Password,  // Usar la contraseña sin sanitizar
                        Recordar,
                        false
                    );
                    if (result.Succeeded)
                    {
                        var urlContentReturnUrl = Url.Content(returnUrl);
                        var urlDestino = Url.Content("~/");
                        var computedUrl = urlContentReturnUrl ?? urlDestino;

                        // Obtener UserId después del login exitoso
                        var userId = _userManager.GetUserId(User) ?? "Unknown";
                        _logger.LogInformation("[AUTH] Login exitoso. UserId: {UserId}, Recordar: {Recordar}",
                            userId, Recordar);

                        return new RedirectResult(computedUrl);
                    }
                    else
                    {
                        var failureReason = result.IsLockedOut ? "Cuenta bloqueada" :
                                          result.IsNotAllowed ? "Cuenta sin permiso" :
                                          result.RequiresTwoFactor ? "Requiere autenticación de dos factores" :
                                          "Credenciales incorrectas";

                        // Intentar obtener UserId buscando el usuario por email (si existe)
                        string userId = "Unknown";
                        try
                        {
                            var usuario = await _userManager.FindByEmailAsync(sanitizedEmail);
                            if (usuario != null)
                            {
                                userId = usuario.Id.ToString();
                            }
                        }
                        catch
                        {
                            // Si no se puede obtener el usuario, mantener "Unknown"
                        }

                        _logger.LogWarning("[AUTH] Error al iniciar sesión. UserId: {UserId}, Razón: {Razon}, IsLockedOut: {IsLockedOut}, IsNotAllowed: {IsNotAllowed}, RequiresTwoFactor: {RequiresTwoFactor}",
                            userId, failureReason, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

                        ModelState.AddModelError(string.Empty, $"Error al iniciar sesión: {failureReason}");
                    }
                }
                return Page();
            }
            catch (Exception e)
            {
                // Limpiar saltos de línea para que CloudWatch lo guarde como un único mensaje
                var stackTrace = (e.StackTrace ?? "No stack trace")
                    .Replace("\r\n", " >> ")
                    .Replace("\n", " >> ")
                    .Replace("\r", " >> ");

                var message = e.Message
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");

                _logger.LogError("Error en OnPostAsync del login. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                    e.GetType().FullName, message, stackTrace);
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
