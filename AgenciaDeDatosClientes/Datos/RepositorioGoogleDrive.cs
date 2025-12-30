using Dapper;
using MySql.Data.MySqlClient;
using Modelos.InyeccionDeDependencias;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones con tokens de Google Drive del usuario
    /// </summary>
    public class RepositorioGoogleDrive : BaseRepositorio
    {
        public RepositorioGoogleDrive(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Obtiene los tokens de Google Drive de un usuario
        /// </summary>
        public async Task<(string? RefreshToken, string? AccessToken, DateTime? ExpiresAt)> ObtenerTokensAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        GoogleDriveRefreshToken,
                        GoogleDriveAccessToken,
                        GoogleDriveAccessTokenExpiresAt
                    FROM Usuario
                    WHERE Id = @UsuarioId;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                dynamic resultado = await conexion.QuerySingleOrDefaultAsync(consulta, new { UsuarioId = idUsuario });

                if (resultado == null)
                {
                    return (null, null, null);
                }

                return (
                    (string?)resultado.GoogleDriveRefreshToken,
                    (string?)resultado.GoogleDriveAccessToken,
                    (DateTime?)resultado.GoogleDriveAccessTokenExpiresAt
                );
            }
        }

        /// <summary>
        /// Guarda los tokens de Google Drive de un usuario
        /// </summary>
        public async Task GuardarTokensAsync(int idUsuario, string refreshToken, string? accessToken = null, DateTime? expiresAt = null)
        {
            using (MySqlConnection conexion = ObtenerConexionEscritura())
            {
                string consulta;
                if (accessToken != null && expiresAt.HasValue)
                {
                    consulta = @"
                        UPDATE Usuario
                        SET 
                            GoogleDriveRefreshToken = @RefreshToken,
                            GoogleDriveAccessToken = @AccessToken,
                            GoogleDriveAccessTokenExpiresAt = @ExpiresAt
                        WHERE Id = @UsuarioId;
                    ";
                    await conexion.ExecuteAsync(consulta, new
                    {
                        UsuarioId = idUsuario,
                        RefreshToken = refreshToken,
                        AccessToken = accessToken,
                        ExpiresAt = expiresAt
                    });
                }
                else
                {
                    consulta = @"
                        UPDATE Usuario
                        SET GoogleDriveRefreshToken = @RefreshToken
                        WHERE Id = @UsuarioId;
                    ";
                    await conexion.ExecuteAsync(consulta, new
                    {
                        UsuarioId = idUsuario,
                        RefreshToken = refreshToken
                    });
                }
            }
        }

        /// <summary>
        /// Actualiza el access token de Google Drive de un usuario
        /// </summary>
        public async Task ActualizarAccessTokenAsync(int idUsuario, string accessToken, DateTime expiresAt)
        {
            using (MySqlConnection conexion = ObtenerConexionEscritura())
            {
                const string consulta = @"
                    UPDATE Usuario
                    SET 
                        GoogleDriveAccessToken = @AccessToken,
                        GoogleDriveAccessTokenExpiresAt = @ExpiresAt
                    WHERE Id = @UsuarioId;
                ";

                await conexion.ExecuteAsync(consulta, new
                {
                    UsuarioId = idUsuario,
                    AccessToken = accessToken,
                    ExpiresAt = expiresAt
                });
            }
        }

        /// <summary>
        /// Obtiene el email de un usuario
        /// </summary>
        public async Task<string?> ObtenerEmailUsuarioAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = "SELECT Email FROM Usuario WHERE Id = @UsuarioId";
                return await conexion.QuerySingleOrDefaultAsync<string>(consulta, new { UsuarioId = idUsuario });
            }
        }
    }
}

