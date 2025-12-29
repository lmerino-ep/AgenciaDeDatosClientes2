namespace Modelos.Entidades
{
    /// <summary>
    /// Entidad que representa la relación many-to-many entre Usuario y Rol
    /// </summary>
    public class UsuarioRol
    {
        /// <summary>
        /// ID del usuario
        /// </summary>
        public int FK_Usuario { get; set; }

        /// <summary>
        /// ID del rol
        /// </summary>
        public int FK_Rol { get; set; }
    }
}
