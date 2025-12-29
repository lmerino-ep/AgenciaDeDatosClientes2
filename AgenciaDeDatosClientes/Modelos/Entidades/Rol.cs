namespace Modelos.Entidades
{
    /// <summary>
    /// Entidad que representa un rol del sistema
    /// </summary>
    public class Rol
    {
        /// <summary>
        /// Identificador único del rol
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre del rol
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Descripción del rol
        /// </summary>
        public string Descripcion { get; set; }
    }
}
