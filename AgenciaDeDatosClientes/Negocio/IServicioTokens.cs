using Modelos.DTO.Tokens;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de tokens
    /// </summary>
    public interface IServicioTokens
    {
        Task<TokensUsuario> ObtenerTokensUsuarioAsync(int idUsuario);
        Task<ResultadoGeneracionToken> GenerarRefreshToken1Async(int idUsuario, bool enviarPorEmail = false);
        Task<ResultadoGeneracionToken> GenerarRefreshToken2Async(int idUsuario, bool enviarPorEmail = false);
        Task<ResultadoGeneracionToken> GenerarAmbosTokensAsync(int idUsuario, bool enviarPorEmail = false);
        Task RevocarTokensAsync(int idUsuario, bool revocarToken1, bool revocarToken2, bool revocarAccessToken);
    }
}

