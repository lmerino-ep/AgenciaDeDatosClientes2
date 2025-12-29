namespace Modelos.DTO.Roles
{
    /// <summary>
    /// DTO para mostrar información de usuario con sus roles
    /// </summary>
    public class UsuarioRoles
    {
        public int IdUsuario { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string TipoUsuario { get; set; }
        public List<Rol> RolesAsignados { get; set; } = new List<Rol>();
        public List<Rol> RolesDisponibles { get; set; } = new List<Rol>();
    }
}
