namespace Modelos.DTO.Usuarios
{
    /// <summary>
    /// DTO para mostrar detalles de un usuario
    /// </summary>
    public class UsuarioDetalle
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Empresa { get; set; }
        public bool Activo { get; set; }
        public int FK_UsuarioTipo { get; set; }
        public string TipoUsuarioNombre { get; set; }
    }
}

