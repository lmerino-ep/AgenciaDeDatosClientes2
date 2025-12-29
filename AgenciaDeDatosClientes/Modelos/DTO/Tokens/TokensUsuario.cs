namespace Modelos.DTO.Tokens
{
    /// <summary>
    /// DTO para mostrar información de tokens de un usuario
    /// </summary>
    public class TokensUsuario
    {
        public int IdUsuario { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }

        // Tokens mostrados parcialmente para seguridad
        public string RefreshToken1Parcial { get; set; }
        public string RefreshToken2Parcial { get; set; }
        public string AccessTokenParcial { get; set; }

        // Tokens completos (solo para generación, NO se devuelven en consultas)
        public string RefreshToken1Completo { get; set; }
        public string RefreshToken2Completo { get; set; }

        public DateTime? CaducidadAccessToken { get; set; }
        public bool AccessTokenValido => CaducidadAccessToken.HasValue && CaducidadAccessToken.Value > DateTime.UtcNow;

        // Estados de tokens
        public bool TieneRefreshToken1 => !string.IsNullOrEmpty(RefreshToken1Parcial);
        public bool TieneRefreshToken2 => !string.IsNullOrEmpty(RefreshToken2Parcial);
        public bool TieneAccessToken => !string.IsNullOrEmpty(AccessTokenParcial);
    }
}
