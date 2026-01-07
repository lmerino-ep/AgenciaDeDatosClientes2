namespace Modelos
{
    /// <summary>
    /// Entidad que representa un tipo de acción de auditoría
    /// </summary>
    public class AuditoriaAccionEntidad
    {
        /// <summary>
        /// Identificador único de la acción
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre de la acción (ej: Crear, Editar, Eliminar)
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Descripción de la acción
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// ID de la categoría a la que pertenece esta acción
        /// </summary>
        public int FK_AuditoriaCategoria { get; set; }
    }
}

