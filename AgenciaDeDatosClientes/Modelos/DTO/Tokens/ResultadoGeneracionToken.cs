namespace Modelos.DTO.Tokens
{
    /// <summary>
    /// DTO para el resultado de generación de tokens
    /// </summary>
    public class ResultadoGeneracionToken
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string RefreshToken1 { get; set; }
        public string RefreshToken2 { get; set; }
        public bool EmailEnviado { get; set; }
    }
}
