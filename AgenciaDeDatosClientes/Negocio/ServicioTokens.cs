using Datos;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Tokens;
// using NugetPortable.Helper; // TODO: Habilitar cuando NugetPortable.Helper esté disponible

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de tokens de usuario
    /// </summary>
    public class ServicioTokens : IServicioTokens
    {
        private readonly RepositorioTokens _repositorio;
        private readonly ILogger<ServicioTokens> _logger;

        public ServicioTokens(
            RepositorioTokens repositorio,
            ILogger<ServicioTokens> logger)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene los tokens de un usuario (con ocultación parcial)
        /// </summary>
        public async Task<TokensUsuario> ObtenerTokensUsuarioAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo tokens del usuario {UsuarioId}", idUsuario);
                TokensUsuario resultado = await _repositorio.ObtenerTokensUsuarioAsync(idUsuario);

                if (resultado == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", idUsuario);
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tokens del usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Genera un nuevo RefreshToken1 para un usuario
        /// </summary>
        public async Task<ResultadoGeneracionToken> GenerarRefreshToken1Async(int idUsuario, bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando RefreshToken1 para usuario {UsuarioId}", idUsuario);

                string nuevoToken = await _repositorio.GenerarRefreshToken1Async(idUsuario);

                ResultadoGeneracionToken resultado = new ResultadoGeneracionToken
                {
                    Exito = true,
                    Mensaje = "RefreshToken1 generado exitosamente",
                    RefreshToken1 = nuevoToken,
                    EmailEnviado = false
                };

                // Enviar por email si se solicita
                if (enviarPorEmail)
                {
                    TokensUsuario datosUsuario = await _repositorio.ObtenerTokensUsuarioAsync(idUsuario);
                    bool emailEnviado = await EnviarTokensPorEmailAsync(datosUsuario.Email, nuevoToken, null);
                    resultado.EmailEnviado = emailEnviado;
                }

                _logger.LogInformation("RefreshToken1 generado exitosamente para usuario {UsuarioId}", idUsuario);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar RefreshToken1 para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Genera un nuevo RefreshToken2 para un usuario
        /// </summary>
        public async Task<ResultadoGeneracionToken> GenerarRefreshToken2Async(int idUsuario, bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando RefreshToken2 para usuario {UsuarioId}", idUsuario);

                string nuevoToken = await _repositorio.GenerarRefreshToken2Async(idUsuario);

                ResultadoGeneracionToken resultado = new ResultadoGeneracionToken
                {
                    Exito = true,
                    Mensaje = "RefreshToken2 generado exitosamente",
                    RefreshToken2 = nuevoToken,
                    EmailEnviado = false
                };

                // Enviar por email si se solicita
                if (enviarPorEmail)
                {
                    TokensUsuario datosUsuario = await _repositorio.ObtenerTokensUsuarioAsync(idUsuario);
                    bool emailEnviado = await EnviarTokensPorEmailAsync(datosUsuario.Email, null, nuevoToken);
                    resultado.EmailEnviado = emailEnviado;
                }

                _logger.LogInformation("RefreshToken2 generado exitosamente para usuario {UsuarioId}", idUsuario);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar RefreshToken2 para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Genera ambos refresh tokens para un usuario
        /// </summary>
        public async Task<ResultadoGeneracionToken> GenerarAmbosTokensAsync(int idUsuario, bool enviarPorEmail = false)
        {
            try
            {
                _logger.LogInformation("Generando ambos RefreshTokens para usuario {UsuarioId}", idUsuario);

                string token1 = await _repositorio.GenerarRefreshToken1Async(idUsuario);
                string token2 = await _repositorio.GenerarRefreshToken2Async(idUsuario);

                ResultadoGeneracionToken resultado = new ResultadoGeneracionToken
                {
                    Exito = true,
                    Mensaje = "Ambos RefreshTokens generados exitosamente",
                    RefreshToken1 = token1,
                    RefreshToken2 = token2,
                    EmailEnviado = false
                };

                // Enviar por email si se solicita
                if (enviarPorEmail)
                {
                    TokensUsuario datosUsuario = await _repositorio.ObtenerTokensUsuarioAsync(idUsuario);
                    bool emailEnviado = await EnviarTokensPorEmailAsync(datosUsuario.Email, token1, token2);
                    resultado.EmailEnviado = emailEnviado;
                }

                _logger.LogInformation("Ambos RefreshTokens generados exitosamente para usuario {UsuarioId}", idUsuario);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar ambos RefreshTokens para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Revoca tokens de un usuario
        /// </summary>
        public async Task RevocarTokensAsync(int idUsuario, bool revocarToken1, bool revocarToken2, bool revocarAccessToken)
        {
            try
            {
                _logger.LogInformation("Revocando tokens del usuario {UsuarioId}: Token1={RevocarToken1}, Token2={RevocarToken2}, AccessToken={RevocarAccessToken}",
                    idUsuario, revocarToken1, revocarToken2, revocarAccessToken);

                await _repositorio.RevocarTokensAsync(idUsuario, revocarToken1, revocarToken2, revocarAccessToken);

                _logger.LogInformation("Tokens revocados exitosamente para usuario {UsuarioId}", idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar tokens del usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Envía tokens por email al usuario
        /// TODO: Implementar cuando NugetPortable.Helper esté disponible
        /// </summary>
        private async Task<bool> EnviarTokensPorEmailAsync(string emailDestino, string refreshToken1, string refreshToken2)
        {
            // TODO: Descomentar cuando NugetPortable.Helper esté disponible
            /*
            try
            {
                string tokensHtml = "";
                string tokensTexto = "";

                if (!string.IsNullOrEmpty(refreshToken1))
                {
                    tokensHtml += $"<p><strong>RefreshToken1:</strong> <code>{refreshToken1}</code></p>";
                    tokensTexto += $"RefreshToken1: {refreshToken1}\n";
                }

                if (!string.IsNullOrEmpty(refreshToken2))
                {
                    tokensHtml += $"<p><strong>RefreshToken2:</strong> <code>{refreshToken2}</code></p>";
                    tokensTexto += $"RefreshToken2: {refreshToken2}\n";
                }

                string cuerpoHtml = $@"
                    <html>
                    <body>
                        <h2>Tokens de Acceso a la API de EpData</h2>
                        <p>Estimado usuario,</p>
                        <p>Se han generado nuevos tokens de acceso para su cuenta:</p>
                        {tokensHtml}
                        <p><strong>IMPORTANTE:</strong> Guarde estos tokens de forma segura. No los comparta con nadie. Estos tokens le permiten acceder a la API de EpData.</p>
                        <p>Para más información sobre cómo usar estos tokens, consulte la documentación de la API.</p>
                        <p>Atentamente,<br/>El equipo de EpData</p>
                    </body>
                    </html>
                ";

                string cuerpoTexto = $@"
                    Tokens de Acceso a la API de EpData

                    Estimado usuario,

                    Se han generado nuevos tokens de acceso para su cuenta:

                    {tokensTexto}

                    IMPORTANTE: Guarde estos tokens de forma segura. No los comparta con nadie. Estos tokens le permiten acceder a la API de EpData.

                    Para más información sobre cómo usar estos tokens, consulte la documentación de la API.

                    Atentamente,
                    El equipo de EpData
                ";

                HelperEmail helperEmail = new HelperEmail();
                string resultado = await helperEmail.Enviar(
                    asunto: "Tokens de Acceso a la API de EpData",
                    cuerpoHtml: cuerpoHtml,
                    cuerpoTexto: cuerpoTexto,
                    adjuntos: null,
                    nombreOrigen: "EpData Dashboard",
                    emailOrigen: "noreply@epdata.es",
                    destinatarios: new List<string> { emailDestino },
                    destinatariosCC: null,
                    destinatariosBCC: null
                );

                _logger.LogInformation("Email con tokens enviado a {Email}: {Resultado}", emailDestino, resultado);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email con tokens a {Email}", emailDestino);
                return false;
            }
            */

            // Implementación temporal: solo registrar en log
            _logger.LogWarning("Envío de email deshabilitado. Tokens solicitados para {Email}", emailDestino);
            _logger.LogInformation("RefreshToken1: {Token1}", refreshToken1 ?? "null");
            _logger.LogInformation("RefreshToken2: {Token2}", refreshToken2 ?? "null");

            await Task.CompletedTask; // Para evitar warning de async
            return false; // Indicar que el email no se envió
        }
    }
}
