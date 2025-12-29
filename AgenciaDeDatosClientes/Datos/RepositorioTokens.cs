using Modelos.InyeccionDeDependencias;
using Dapper;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using Modelos.DTO.Tokens;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones con tokens de usuario
    /// </summary>
    public class RepositorioTokens : BaseRepositorio
    {
        public RepositorioTokens(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Obtiene los tokens de un usuario (con ocultación parcial)
        /// </summary>
        public async Task<TokensUsuario> ObtenerTokensUsuarioAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        U.Id as IdUsuario,
                        U.Email,
                        U.Nombre,
                        U.Apellidos,
                        U.RefreshToken1EpDataApi,
                        U.RefreshToken2EpDataApi,
                        U.AccessTokenActivoEpDataApi,
                        U.CaducidadAccessTokenActivoEpDataApi
                    FROM Usuario U
                    WHERE U.Id = @UsuarioId;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                dynamic resultado = await conexion.QuerySingleOrDefaultAsync(consulta, new { UsuarioId = idUsuario });

                if (resultado == null)
                {
                    return null;
                }

                return new TokensUsuario
                {
                    IdUsuario = resultado.IdUsuario,
                    Email = resultado.Email,
                    Nombre = resultado.Nombre,
                    Apellidos = resultado.Apellidos,
                    RefreshToken1Parcial = OcultarToken(resultado.RefreshToken1EpDataApi),
                    RefreshToken2Parcial = OcultarToken(resultado.RefreshToken2EpDataApi),
                    AccessTokenParcial = OcultarToken(resultado.AccessTokenActivoEpDataApi),
                    CaducidadAccessToken = resultado.CaducidadAccessTokenActivoEpDataApi
                };
            }
        }

        /// <summary>
        /// Genera un nuevo RefreshToken1 para un usuario
        /// </summary>
        public async Task<string> GenerarRefreshToken1Async(int idUsuario)
        {
            return await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                string nuevoToken = GenerarTokenSeguro();

                const string consulta = @"
                    UPDATE Usuario
                    SET RefreshToken1EpDataApi = @Token
                    WHERE Id = @UsuarioId;
                ";

                await conexion.ExecuteAsync(consulta, new { UsuarioId = idUsuario, Token = nuevoToken }, transaccion);

                return nuevoToken;
            });
        }

        /// <summary>
        /// Genera un nuevo RefreshToken2 para un usuario
        /// </summary>
        public async Task<string> GenerarRefreshToken2Async(int idUsuario)
        {
            return await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                string nuevoToken = GenerarTokenSeguro();

                const string consulta = @"
                    UPDATE Usuario
                    SET RefreshToken2EpDataApi = @Token
                    WHERE Id = @UsuarioId;
                ";

                await conexion.ExecuteAsync(consulta, new { UsuarioId = idUsuario, Token = nuevoToken }, transaccion);

                return nuevoToken;
            });
        }

        /// <summary>
        /// Revoca tokens de un usuario
        /// </summary>
        public async Task RevocarTokensAsync(int idUsuario, bool revocarToken1, bool revocarToken2, bool revocarAccessToken)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                List<string> actualizaciones = new List<string>();

                if (revocarToken1)
                {
                    actualizaciones.Add("RefreshToken1EpDataApi = NULL");
                }

                if (revocarToken2)
                {
                    actualizaciones.Add("RefreshToken2EpDataApi = NULL");
                }

                if (revocarAccessToken)
                {
                    actualizaciones.Add("AccessTokenActivoEpDataApi = NULL");
                    actualizaciones.Add("CaducidadAccessTokenActivoEpDataApi = NULL");
                }

                if (actualizaciones.Any())
                {
                    string consulta = $@"
                        UPDATE Usuario
                        SET {string.Join(", ", actualizaciones)}
                        WHERE Id = @UsuarioId;
                    ";

                    await conexion.ExecuteAsync(consulta, new { UsuarioId = idUsuario }, transaccion);
                }
            });
        }

        /// <summary>
        /// Genera un token criptográficamente seguro
        /// Genera 32 bytes aleatorios y los convierte a Base64
        /// </summary>
        private string GenerarTokenSeguro()
        {
            byte[] randomBytes = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Oculta parcialmente un token mostrando solo los primeros 6 y últimos 6 caracteres
        /// </summary>
        private string OcultarToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (token.Length <= 12)
            {
                return "***";
            }

            return $"{token.Substring(0, 6)}...{token.Substring(token.Length - 6)}";
        }
    }
}
