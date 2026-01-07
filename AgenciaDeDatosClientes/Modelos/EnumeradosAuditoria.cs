namespace Modelos
{
    /// <summary>
    /// Enumerado para las categorías de auditoría (módulos del dashboard)
    /// Los valores se buscan en la BD por nombre, no por ID
    /// </summary>
    public enum AuditoriaCategoriaEnum
    {
        Usuarios,
        Roles,
        Tokens,
        Metricas,
        Documentacion
    }

    /// <summary>
    /// Enumerado para las acciones de auditoría por categoría
    /// Los valores se buscan en la BD por nombre, no por ID
    /// Para añadir una nueva acción: añádela aquí y en la BD con el mismo nombre
    /// </summary>
    public enum AuditoriaAccionEnum
    {
        // Usuarios
        UsuarioCrear,
        UsuarioEditar,
        UsuarioEliminar,
        UsuarioCambiarContrasena,
        UsuarioCambiarTipo,

        // Roles
        RolCrear,
        RolEditar,
        RolAsignar,
        RolRemover,

        // Tokens
        TokenGenerarRefreshToken1,
        TokenGenerarRefreshToken2,
        TokenRevocarTokens,

        // Métricas
        MetricaConsultar,
        MetricaExportar,

        // Documentación
        DocumentacionSubir,
        DocumentacionDescargar,
        DocumentacionEliminar,
        DocumentacionSobrescribir
    }

    /// <summary>
    /// Clase estática con métodos de ayuda para los enumerados de auditoría
    /// </summary>
    public static class EnumeradosAuditoriaHelper
    {
        /// <summary>
        /// Convierte el enum de categoría a su nombre (string)
        /// </summary>
        public static string ObtenerNombreCategoria(AuditoriaCategoriaEnum categoria)
        {
            return categoria.ToString();
        }

        /// <summary>
        /// Convierte el enum de acción a su nombre (string)
        /// </summary>
        public static string ObtenerNombreAccion(AuditoriaAccionEnum accion)
        {
            return accion.ToString();
        }
    }
}

