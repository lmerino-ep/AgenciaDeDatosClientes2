namespace Datos
{
    /// <summary>
    /// Interfaz para operaciones con tokens de Google Drive del usuario
    /// </summary>
    public interface IRepositorioGoogleDrive
    {
        /// <summary>
        /// Obtiene los tokens de Google Drive del usuario
        /// </summary>
        Task<(string? RefreshToken, string? AccessToken, DateTime? ExpiresAt)> ObtenerTokensAsync(int idUsuario);

        /// <summary>
        /// Guarda los tokens de Google Drive del usuario
        /// </summary>
        Task GuardarTokensAsync(int idUsuario, string refreshToken, string? accessToken = null, DateTime? expiresAt = null);

        /// <summary>
        /// Actualiza el access token de Google Drive del usuario
        /// </summary>
        Task ActualizarAccessTokenAsync(int idUsuario, string accessToken, DateTime expiresAt);

        /// <summary>
        /// Obtiene el email de un usuario
        /// </summary>
        Task<string?> ObtenerEmailUsuarioAsync(int idUsuario);

        /// <summary>
        /// Limpia todos los tokens de Google Drive de la sesión
        /// </summary>
        Task LimpiarTokensAsync();

        /// <summary>
        /// Verifica si el usuario cambió y limpia los tokens si es necesario
        /// </summary>
        Task VerificarYLimpiarTokensSiUsuarioCambioAsync(int idUsuario);
    }
}

