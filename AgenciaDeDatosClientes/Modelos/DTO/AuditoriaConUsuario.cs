namespace Modelos.DTO
{
    /// <summary>
    /// DTO para consultar auditoría con información de usuario
    /// </summary>
    public class AuditoriaConUsuario
    {
        public long Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string UsuarioEmail { get; set; }
        public string UsuarioNombre { get; set; }
        public string Accion { get; set; }
        public string Modulo { get; set; }
        public string Descripcion { get; set; }
        public string Resultado { get; set; }
        public string DireccionIP { get; set; }
    }
}

