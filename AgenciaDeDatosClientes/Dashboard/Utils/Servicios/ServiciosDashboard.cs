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
            services.AddScoped<RepositorioUsuarios>();
            services.AddScoped<RepositorioAuditoria>();
            services.AddScoped<RepositorioRoles>();
            services.AddScoped<RepositorioTokens>();
            services.AddScoped<IRepositorioMetricas, RepositorioMetricas>();
            services.AddScoped<IRepositorioSuscripciones, RepositorioSuscripciones>();

            return services;
        }

        /// <summary>
        /// Registra los servicios del Dashboard
        /// </summary>
        public static IServiceCollection AddServiciosDashboard(this IServiceCollection services)
        {
            services.AddScoped<IServicioUsuarios, ServicioUsuarios>();
            services.AddScoped<IServicioAuditoria, ServicioAuditoria>();
            services.AddScoped<IServicioRoles, ServicioRoles>();
            services.AddScoped<IServicioTokens, ServicioTokens>();
            services.AddScoped<IServicioMetricas, ServicioMetricas>();
            services.AddScoped<IServicioDocumentacionS3, ServicioDocumentacionS3>();
            services.AddScoped<IServicioSuscripciones, ServicioSuscripciones>();

            // HttpContextAccessor para acceder al contexto HTTP en servicios
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Registra los background services del Dashboard
        /// </summary>
        public static IServiceCollection AddBackgroundServicesDashboard(this IServiceCollection services)
        {
            // Background Service de regeneración de métricas
            services.AddHostedService<ServicioRegeneracionMetricas>();

            return services;
        }
    }
}

