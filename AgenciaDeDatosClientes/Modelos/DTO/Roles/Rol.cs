namespace Modelos.DTO.Roles
{
    /// <summary>
    /// DTO para transferir información de un rol
    /// </summary>
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Asignado { get; set; } // Indica si el rol está asignado al usuario
    }
}
