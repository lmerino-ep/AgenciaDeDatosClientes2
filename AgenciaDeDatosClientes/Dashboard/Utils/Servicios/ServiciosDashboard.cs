using Datos;
using Negocio;
using Utils.BackgroundServices;

namespace Utils.Servicios
{
    /// <summary>
    /// Configuración de servicios y repositorios del Dashboard
    /// </summary>
    public static class ServiciosDashboard
    {
        /// <summary>
        /// Registra los repositorios del Dashboard
        /// </summary>
        public static IServiceCollection AddRepositoriosDashboard(this IServiceCollection services)
        {
            // Repositorios - Fase 1 (Usuarios)
            services.AddScoped<RepositorioUsuarios>();

            // Repositorios - Fase 2
            services.AddScoped<RepositorioAuditoria>();
            services.AddScoped<RepositorioRoles>();
            services.AddScoped<RepositorioTokens>();

            // Repositorios - Fase 3 (Métricas)
            services.AddScoped<IRepositorioMetricas, RepositorioMetricas>();

            // Repositorios - Fase 4 (Documentación)
            services.AddScoped<RepositorioGoogleDrive>();

            return services;
        }

        /// <summary>
        /// Registra los servicios del Dashboard
        /// </summary>
        public static IServiceCollection AddServiciosDashboard(this IServiceCollection services)
        {
            // Servicios - Fase 1 (Usuarios)
            services.AddScoped<IServicioUsuarios, ServicioUsuarios>();

            // Servicios - Fase 2
            services.AddScoped<IServicioAuditoria, ServicioAuditoria>();
            services.AddScoped<IServicioRoles, ServicioRoles>();
            services.AddScoped<IServicioTokens, ServicioTokens>();

            // Servicios - Fase 3 (Métricas)
            services.AddScoped<IServicioMetricas, ServicioMetricas>();

            // Servicios - Fase 4 (Documentación)
            services.AddScoped<IServicioGoogleDrive, ServicioGoogleDrive>();

            // HttpContextAccessor para acceder al contexto HTTP en servicios
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Registra los background services del Dashboard
        /// </summary>
        public static IServiceCollection AddBackgroundServicesDashboard(this IServiceCollection services)
        {
            // Background Service de regeneración de métricas (Fase 3)
            services.AddHostedService<ServicioRegeneracionMetricas>();

            return services;
        }
    }
}

