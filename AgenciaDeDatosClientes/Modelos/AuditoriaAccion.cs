namespace Modelos
{
    /// <summary>
    /// Entidad que representa un registro de auditoría de acciones administrativas en el dashboard
    /// </summary>
    public class AuditoriaAccion
    {
        /// <summary>
        /// Identificador único del registro de auditoría
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// ID del usuario que realizó la acción
        /// </summary>
        public int FK_Usuario { get; set; }

        /// <summary>
        /// Tipo de acción realizada (ej: Usuario.Crear, Tokens.Generar)
        /// </summary>
        public string Accion { get; set; }

        /// <summary>
        /// Módulo del sistema (Usuarios, Roles, Tokens, Metricas, Documentacion)
        /// </summary>
        public string Modulo { get; set; }

        /// <summary>
        /// Descripción legible de la acción
        /// </summary>
        public string Descripcion { get; set; }

        /// <summary>
        /// Nombre de la entidad afectada (ej: Usuario, Rol, Token)
        /// </summary>
        public string EntidadAfectada { get; set; }

        /// <summary>
        /// ID de la entidad afectada
        /// </summary>
        public int? IdEntidadAfectada { get; set; }

        /// <summary>
        /// JSON con diff antes/después de los cambios
        /// Formato: { "antes": {...}, "despues": {...} }
        /// </summary>
        public string CambiosRealizados { get; set; }

        /// <summary>
        /// Dirección IP del usuario (IPv4 o IPv6)
        /// </summary>
        public string DireccionIP { get; set; }

        /// <summary>
        /// User Agent del navegador
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Fecha y hora de la acción con precisión de milisegundos
        /// </summary>
        public DateTime FechaHora { get; set; }

        /// <summary>
        /// Duración de la operación en milisegundos
        /// </summary>
        public int? DuracionMs { get; set; }

        /// <summary>
        /// Resultado de la acción: Exito, Error, NoAutorizado
        /// </summary>
        public string Resultado { get; set; }

        /// <summary>
        /// Mensaje de error si el resultado fue Error
        /// </summary>
        public string MensajeError { get; set; }
    }

    /// <summary>
    /// Tipos de acciones de auditoría
    /// </summary>
    public static class TiposAccionAuditoria
    {
        // Usuarios
        public const string UsuarioCrear = "Usuario.Crear";
        public const string UsuarioEditar = "Usuario.Editar";
        public const string UsuarioEliminar = "Usuario.Eliminar";
        public const string UsuarioCambiarContrasena = "Usuario.CambiarContrasena";
        public const string UsuarioCambiarTipo = "Usuario.CambiarTipo";

        // Roles
        public const string RolesCrear = "Roles.Crear";
        public const string RolesEditar = "Roles.Editar";
        public const string RolesAsignar = "Roles.Asignar";
        public const string RolesRemover = "Roles.Remover";

        // Tokens
        public const string TokensGenerarRefreshToken1 = "Tokens.GenerarRefreshToken1";
        public const string TokensGenerarRefreshToken2 = "Tokens.GenerarRefreshToken2";
        public const string TokensRevocarTokens = "Tokens.RevocarTokens";

        // Métricas
        public const string MetricasConsultar = "Metricas.Consultar";
        public const string MetricasExportar = "Metricas.Exportar";

        // Documentación
        public const string DocumentacionSubir = "Documentacion.Subir";
        public const string DocumentacionDescargar = "Documentacion.Descargar";
        public const string DocumentacionEliminar = "Documentacion.Eliminar";
        public const string DocumentacionSobrescribir = "Documentacion.Sobrescribir";
    }

    /// <summary>
    /// Módulos del dashboard
    /// </summary>
    public static class ModulosDashboard
    {
        public const string Usuarios = "Usuarios";
        public const string Roles = "Roles";
        public const string Tokens = "Tokens";
        public const string Metricas = "Metricas";
        public const string Documentacion = "Documentacion";
    }

    /// <summary>
    /// Resultados de acciones de auditoría
    /// </summary>
    public static class ResultadosAuditoria
    {
        public const string Exito = "Exito";
        public const string Error = "Error";
        public const string NoAutorizado = "NoAutorizado";
    }
}
