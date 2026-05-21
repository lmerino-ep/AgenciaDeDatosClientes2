namespace Modelos.DTO
{
    /// <summary>
    /// DTO para consultar auditoría con información de usuario
    /// </summary>
    public class AuditoriaConUsuario
    {
        public long Id { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UsuarioEmail { get; set; }
        public string UsuarioNombre { get; set; }
        public string AccionNombre { get; set; }
        public string CategoriaNombre { get; set; }
        public string Descripcion { get; set; }
    }
}

