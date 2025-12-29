using Microsoft.Extensions.DependencyInjection;
using Datos;
using Negocio;

namespace Utils.Servicios
{
    /// <summary>
    /// Extension methods para configurar servicios específicos del Dashboard
    /// </summary>
    public static class ServiceCollectionExtensionsDashboard
    {
        /// <summary>
        /// Configura los repositorios y servicios específicos del Dashboard
        /// </summary>
        public static IServiceCollection AddDashboardServices(this IServiceCollection services)
        {
            // ===== REPOSITORIOS =====
            // Registrar repositorios como Scoped (una instancia por request HTTP)
            services.AddScoped<RepositorioAuditoria>();
            services.AddScoped<RepositorioRoles>();
            services.AddScoped<RepositorioTokens>();

            // ===== SERVICIOS DE NEGOCIO =====
            // Registrar servicios de negocio como Scoped con sus interfaces
            services.AddScoped<IServicioAuditoria, ServicioAuditoria>();
            services.AddScoped<IServicioRoles, ServicioRoles>();
            services.AddScoped<IServicioTokens, ServicioTokens>();

            // HttpContextAccessor para acceder al contexto HTTP en servicios
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Configura las políticas de autorización específicas del Dashboard
        /// </summary>
        public static IServiceCollection AddDashboardAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Políticas del Dashboard (roles 37-45)
                options.AddPolicy("DashboardGestionUsuarios", policy =>
                    policy.RequireRole("Dashboard - Gestión de Usuarios"));

                options.AddPolicy("DashboardConsultaUsuarios", policy =>
                    policy.RequireRole("Dashboard - Consulta de Usuarios"));

                options.AddPolicy("DashboardGestionRoles", policy =>
                    policy.RequireRole("Dashboard - Gestión de Roles"));

                options.AddPolicy("DashboardGestionTokens", policy =>
                    policy.RequireRole("Dashboard - Gestión de Tokens"));

                options.AddPolicy("DashboardConsultaMetricas", policy =>
                    policy.RequireRole("Dashboard - Consulta de Métricas"));

                options.AddPolicy("DashboardExportacionMetricas", policy =>
                    policy.RequireRole("Dashboard - Exportación de Métricas"));

                options.AddPolicy("DashboardGestionDocumentacion", policy =>
                    policy.RequireRole("Dashboard - Gestión de Documentación"));

                options.AddPolicy("DashboardAuditoria", policy =>
                    policy.RequireRole("Dashboard - Auditoría"));

                options.AddPolicy("DashboardAccesoTotal", policy =>
                    policy.RequireRole("Dashboard - Acceso total"));
            });

            return services;
        }
    }
}
