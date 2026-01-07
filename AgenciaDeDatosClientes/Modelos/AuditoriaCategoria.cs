namespace Modelos
{
    /// <summary>
    /// Entidad que representa una categoría de auditoría (módulo del dashboard)
    /// </summary>
    public class AuditoriaCategoria
    {
        /// <summary>
        /// Identificador único de la categoría
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre de la categoría (ej: Usuarios, Roles, Tokens)
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Descripción de la categoría
        /// </summary>
        public string Descripcion { get; set; }
    }
}

