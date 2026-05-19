namespace Utils.Authorization
{
    /// <summary>
    /// Configuración de políticas de autorización para el Dashboard
    /// </summary>
    public static class PoliticasAutorizacion
    {
        /// <summary>
        /// Nombres de las políticas de autorización
        /// </summary>
        public static class Politicas
        {
            // Gestión de usuarios
            public const string GestionUsuarios = "Dashboard.GestionUsuarios";
            public const string ConsultaUsuarios = "Dashboard.ConsultaUsuarios";

            // Gestión de roles y tokens
            public const string GestionRoles = "Dashboard.GestionRoles";
            public const string GestionTokens = "Dashboard.GestionTokens";

            // Métricas
            public const string ConsultaMetricas = "Dashboard.ConsultaMetricas";
            public const string ConsultaMetricasOProveedor = "Dashboard.ConsultaMetricasOProveedor";
            public const string ExportacionMetricas = "Dashboard.ExportacionMetricas";

            // Documentación
            public const string GestionDocumentacion = "Dashboard.GestionDocumentacion";

            // Auditoría
            public const string Auditoria = "Dashboard.Auditoria";

            // Acceso total
            public const string AccesoTotal = "Dashboard.AccesoTotal";
        }

        /// <summary>
        /// Nombres de los roles del Dashboard
        /// </summary>
        public static class Roles
        {
            public const string GestionUsuarios = "Dashboard - Gestión de Usuarios";
            public const string ConsultaUsuarios = "Dashboard - Consulta de Usuarios";
            public const string GestionRoles = "Dashboard - Gestión de Roles";
            public const string GestionTokens = "Dashboard - Gestión de Tokens";
            public const string ConsultaMetricas = "Dashboard - Consulta de Métricas";
            public const string ExportacionMetricas = "Dashboard - Exportación de Métricas";
            public const string GestionDocumentacion = "Dashboard - Gestión de Documentación";
            public const string Auditoria = "Dashboard - Auditoría";
            public const string AccesoTotal = "Dashboard - Acceso total";
        }
    }
}

