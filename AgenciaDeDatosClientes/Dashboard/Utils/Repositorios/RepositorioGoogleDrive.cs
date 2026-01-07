using Microsoft.AspNetCore.Http;
using Datos;

namespace Utils.Repositorios
{
    /// <summary>
    /// Repositorio para operaciones con tokens de Google Drive del usuario usando sesiones
    /// Los tokens se almacenan en la sesión del usuario y se pierden al cerrar sesión
    /// </summary>
    public class RepositorioGoogleDrive : IRepositorioGoogleDrive
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKeyRefreshToken = "GoogleDrive_RefreshToken";
        private const string SessionKeyAccessToken = "GoogleDrive_AccessToken";
        private const string SessionKeyExpiresAt = "GoogleDrive_ExpiresAt";
        private const string SessionKeyUsuarioId = "GoogleDrive_UsuarioId";

        public RepositorioGoogleDrive(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        /// <summary>
        /// Obtiene los tokens de Google Drive de la sesión actual
        /// </summary>
        public Task<(string? RefreshToken, string? AccessToken, DateTime? ExpiresAt)> ObtenerTokensAsync(int idUsuario)
        {
            if (Session == null)
            {
                return Task.FromResult<(string?, string?, DateTime?)>((null, null, null));
            }

            // Verificar si el usuario cambió
            string? usuarioIdGuardado = Session.GetString(SessionKeyUsuarioId);
            if (!string.IsNullOrEmpty(usuarioIdGuardado) && usuarioIdGuardado != idUsuario.ToString())
            {
                // El usuario cambió, limpiar tokens
                LimpiarTokensAsync().Wait();
                return Task.FromResult<(string?, string?, DateTime?)>((null, null, null));
            }

            string? refreshToken = Session.GetString(SessionKeyRefreshToken);
            string? accessToken = Session.GetString(SessionKeyAccessToken);
            
            DateTime? expiresAt = null;
            string? expiresAtStr = Session.GetString(SessionKeyExpiresAt);
            if (!string.IsNullOrEmpty(expiresAtStr) && DateTime.TryParse(expiresAtStr, out DateTime parsedDate))
            {
                expiresAt = parsedDate;
            }

            return Task.FromResult((refreshToken, accessToken, expiresAt));
        }

        /// <summary>
        /// Guarda los tokens de Google Drive en la sesión actual
        /// </summary>
        public Task GuardarTokensAsync(int idUsuario, string refreshToken, string? accessToken = null, DateTime? expiresAt = null)
        {
            if (Session == null)
            {
                throw new InvalidOperationException("No hay sesión disponible para guardar tokens");
            }

            // Guardar el ID del usuario para verificar cambios
            Session.SetString(SessionKeyUsuarioId, idUsuario.ToString());
            Session.SetString(SessionKeyRefreshToken, refreshToken);
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                Session.SetString(SessionKeyAccessToken, accessToken);
            }
            
            if (expiresAt.HasValue)
            {
                Session.SetString(SessionKeyExpiresAt, expiresAt.Value.ToString("O")); // ISO 8601 format
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Actualiza el access token de Google Drive en la sesión actual
        /// </summary>
        public Task ActualizarAccessTokenAsync(int idUsuario, string accessToken, DateTime expiresAt)
        {
            if (Session == null)
            {
                throw new InvalidOperationException("No hay sesión disponible para actualizar tokens");
            }

            Session.SetString(SessionKeyAccessToken, accessToken);
            Session.SetString(SessionKeyExpiresAt, expiresAt.ToString("O")); // ISO 8601 format

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtiene el email de un usuario desde la sesión (si está disponible) o desde los claims
        /// </summary>
        public Task<string?> ObtenerEmailUsuarioAsync(int idUsuario)
        {
            if (_httpContextAccessor.HttpContext?.User?.Identity?.Name != null)
            {
                return Task.FromResult<string?>(_httpContextAccessor.HttpContext.User.Identity.Name);
            }

            return Task.FromResult<string?>(null);
        }

        /// <summary>
        /// Limpia todos los tokens de Google Drive de la sesión
        /// </summary>
        public Task LimpiarTokensAsync()
        {
            if (Session == null)
            {
                return Task.CompletedTask;
            }

            Session.Remove(SessionKeyRefreshToken);
            Session.Remove(SessionKeyAccessToken);
            Session.Remove(SessionKeyExpiresAt);
            Session.Remove(SessionKeyUsuarioId);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Verifica si el usuario cambió y limpia los tokens si es necesario
        /// </summary>
        public Task VerificarYLimpiarTokensSiUsuarioCambioAsync(int idUsuario)
        {
            if (Session == null)
            {
                return Task.CompletedTask;
            }

            string? usuarioIdGuardado = Session.GetString(SessionKeyUsuarioId);
            if (!string.IsNullOrEmpty(usuarioIdGuardado) && usuarioIdGuardado != idUsuario.ToString())
            {
                // El usuario cambió, limpiar tokens
                return LimpiarTokensAsync();
            }

            return Task.CompletedTask;
        }
    }
}

