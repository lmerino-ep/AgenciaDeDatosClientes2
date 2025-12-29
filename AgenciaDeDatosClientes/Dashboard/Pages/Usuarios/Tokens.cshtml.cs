using Negocio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Tokens;

namespace Pages.Usuarios
{
    public class TokensModel : PageModel
    {
        private readonly IServicioTokens _servicioTokens;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<TokensModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public TokensUsuario TokensUsuario { get; set; }

        [TempData]
        public string MensajeExito { get; set; }

        [TempData]
        public string MensajeError { get; set; }

        [TempData]
        public string TokenGenerado { get; set; }

        [TempData]
        public string TipoTokenGenerado { get; set; }

        public TokensModel(
            IServicioTokens servicioTokens,
            IServicioAuditoria servicioAuditoria,
            ILogger<TokensModel> logger)
        {
            _servicioTokens = servicioTokens ?? throw new ArgumentNullException(nameof(servicioTokens));
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Consultando tokens del usuario {UsuarioId}", Id);

                TokensUsuario = await _servicioTokens.ObtenerTokensUsuarioAsync(Id);

                if (TokensUsuario == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", Id);
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar tokens del usuario {UsuarioId}", Id);
                MensajeError = "Error al cargar los tokens del usuario. Por favor, intenta nuevamente.";
                return RedirectToPage("/Usuarios/Listado");
            }
        }

        public async Task<IActionResult> OnPostGenerarToken1Async(bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando RefreshToken1 para usuario {UsuarioId}, enviarPorEmail={EnviarPorEmail}",
                    Id, enviarPorEmail);

                ResultadoGeneracionToken resultado = await _servicioTokens.GenerarRefreshToken1Async(Id, enviarPorEmail);

                if (resultado.Exito)
                {
                    // Registrar en auditoría
                    await _servicioAuditoria.RegistrarAccionAsync(
                        User,
                        "Tokens.GenerarRefreshToken1",
                        "Tokens",
                        $"RefreshToken1 generado para usuario ID {Id}" +
                        (enviarPorEmail ? " (enviado por email)" : ""),
                        "Exito"
                    );

                    string mensajeEmail = enviarPorEmail && resultado.EmailEnviado
                        ? " El token ha sido enviado al email del usuario."
                        : (enviarPorEmail && !resultado.EmailEnviado
                            ? " ADVERTENCIA: No se pudo enviar el email."
                            : "");

                    MensajeExito = $"RefreshToken1 generado exitosamente.{mensajeEmail}";

                    // Guardar token para mostrar en modal (solo si no se envió por email o si falló el envío)
                    if (!enviarPorEmail || !resultado.EmailEnviado)
                    {
                        TokenGenerado = resultado.RefreshToken1;
                        TipoTokenGenerado = "RefreshToken1";
                    }

                    _logger.LogInformation("RefreshToken1 generado exitosamente para usuario {UsuarioId}", Id);
                }
                else
                {
                    MensajeError = $"Error al generar RefreshToken1: {resultado.Mensaje}";
                }

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar RefreshToken1 para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.GenerarRefreshToken1",
                    "Tokens",
                    $"Error al generar RefreshToken1 para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al generar el token. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostGenerarToken2Async(bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando RefreshToken2 para usuario {UsuarioId}, enviarPorEmail={EnviarPorEmail}",
                    Id, enviarPorEmail);

                ResultadoGeneracionToken resultado = await _servicioTokens.GenerarRefreshToken2Async(Id, enviarPorEmail);

                if (resultado.Exito)
                {
                    await _servicioAuditoria.RegistrarAccionAsync(
                        User,
                        "Tokens.GenerarRefreshToken2",
                        "Tokens",
                        $"RefreshToken2 generado para usuario ID {Id}" +
                        (enviarPorEmail ? " (enviado por email)" : ""),
                        "Exito"
                    );

                    string mensajeEmail = enviarPorEmail && resultado.EmailEnviado
                        ? " El token ha sido enviado al email del usuario."
                        : (enviarPorEmail && !resultado.EmailEnviado
                            ? " ADVERTENCIA: No se pudo enviar el email."
                            : "");

                    MensajeExito = $"RefreshToken2 generado exitosamente.{mensajeEmail}";

                    if (!enviarPorEmail || !resultado.EmailEnviado)
                    {
                        TokenGenerado = resultado.RefreshToken2;
                        TipoTokenGenerado = "RefreshToken2";
                    }

                    _logger.LogInformation("RefreshToken2 generado exitosamente para usuario {UsuarioId}", Id);
                }
                else
                {
                    MensajeError = $"Error al generar RefreshToken2: {resultado.Mensaje}";
                }

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar RefreshToken2 para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.GenerarRefreshToken2",
                    "Tokens",
                    $"Error al generar RefreshToken2 para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al generar el token. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostGenerarAmbosAsync(bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando ambos RefreshTokens para usuario {UsuarioId}, enviarPorEmail={EnviarPorEmail}",
                    Id, enviarPorEmail);

                ResultadoGeneracionToken resultado = await _servicioTokens.GenerarAmbosTokensAsync(Id, enviarPorEmail);

                if (resultado.Exito)
                {
                    await _servicioAuditoria.RegistrarAccionAsync(
                        User,
                        "Tokens.GenerarRefreshTokens",
                        "Tokens",
                        $"Ambos RefreshTokens generados para usuario ID {Id}" +
                        (enviarPorEmail ? " (enviados por email)" : ""),
                        "Exito"
                    );

                    string mensajeEmail = enviarPorEmail && resultado.EmailEnviado
                        ? " Los tokens han sido enviados al email del usuario."
                        : (enviarPorEmail && !resultado.EmailEnviado
                            ? " ADVERTENCIA: No se pudo enviar el email."
                            : "");

                    MensajeExito = $"Ambos RefreshTokens generados exitosamente.{mensajeEmail}";

                    _logger.LogInformation("Ambos RefreshTokens generados exitosamente para usuario {UsuarioId}", Id);
                }
                else
                {
                    MensajeError = $"Error al generar los tokens: {resultado.Mensaje}";
                }

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar ambos RefreshTokens para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.GenerarRefreshTokens",
                    "Tokens",
                    $"Error al generar ambos RefreshTokens para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al generar los tokens. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostRevocarToken1Async()
        {
            try
            {
                _logger.LogInformation("Revocando RefreshToken1 para usuario {UsuarioId}", Id);

                await _servicioTokens.RevocarTokensAsync(Id, revocarToken1: true, revocarToken2: false, revocarAccessToken: false);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"RefreshToken1 revocado para usuario ID {Id}",
                    "Exito"
                );

                MensajeExito = "RefreshToken1 revocado exitosamente.";
                _logger.LogInformation("RefreshToken1 revocado para usuario {UsuarioId}", Id);

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar RefreshToken1 para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"Error al revocar RefreshToken1 para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al revocar el token. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostRevocarToken2Async()
        {
            try
            {
                _logger.LogInformation("Revocando RefreshToken2 para usuario {UsuarioId}", Id);

                await _servicioTokens.RevocarTokensAsync(Id, revocarToken1: false, revocarToken2: true, revocarAccessToken: false);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"RefreshToken2 revocado para usuario ID {Id}",
                    "Exito"
                );

                MensajeExito = "RefreshToken2 revocado exitosamente.";
                _logger.LogInformation("RefreshToken2 revocado para usuario {UsuarioId}", Id);

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar RefreshToken2 para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"Error al revocar RefreshToken2 para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al revocar el token. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }

        public async Task<IActionResult> OnPostRevocarTodosAsync()
        {
            try
            {
                _logger.LogInformation("Revocando todos los tokens para usuario {UsuarioId}", Id);

                await _servicioTokens.RevocarTokensAsync(Id, revocarToken1: true, revocarToken2: true, revocarAccessToken: true);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"Todos los tokens revocados para usuario ID {Id}",
                    "Exito"
                );

                MensajeExito = "Todos los tokens han sido revocados exitosamente.";
                _logger.LogInformation("Todos los tokens revocados para usuario {UsuarioId}", Id);

                return RedirectToPage(new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar todos los tokens para usuario {UsuarioId}", Id);

                await _servicioAuditoria.RegistrarAccionAsync(
                    User,
                    "Tokens.RevocarTokens",
                    "Tokens",
                    $"Error al revocar todos los tokens para usuario ID {Id}",
                    "Error",
                    null,
                    ex.Message
                );

                MensajeError = "Error al revocar los tokens. Por favor, intenta nuevamente.";
                return RedirectToPage(new { id = Id });
            }
        }
    }
}
