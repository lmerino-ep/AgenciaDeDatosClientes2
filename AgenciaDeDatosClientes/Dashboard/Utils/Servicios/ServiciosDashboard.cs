using Datos;
using Negocio;
using Negocio.PagosResultados;
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
            services.AddScoped<IRepositorioMetricasProveedor, RepositorioMetricasProveedor>();
            services.AddScoped<IRepositorioProveedores, RepositorioProveedores>();
            services.AddScoped<IRepositorioSuscripciones, RepositorioSuscripciones>();
            services.AddScoped<IRepositorioComprasResultados, RepositorioComprasResultados>();
            services.AddScoped<IRepositorioWebhooksStripe, RepositorioWebhooksStripe>();

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
            services.AddScoped<IServicioContextoDashboard, ServicioContextoDashboard>();
            services.AddScoped<IServicioDocumentacionS3, ServicioDocumentacionS3>();
            services.AddScoped<IServicioSuscripciones, ServicioSuscripciones>();
            services.AddScoped<IServicioComprasResultados, ServicioComprasResultados>();
            services.AddScoped<IServicioComprasStripe, ServicioComprasStripe>();
            services.AddScoped<IServicioWebhookStripe, ServicioWebhookStripe>();
            services.AddHttpClient("LemonSqueezy");

            // Pasarela de pago de Resultados Empresariales intercambiable por configuración
            // (Configuracion.ProveedorPagoResultados): Stripe es el sustituto temporal de LemonSqueezy
            // mientras esta última tiene la tienda sin verificar.
            services.AddScoped<IPasarelaPagoResultados>(sp =>
            {
                var proveedor = sp.GetRequiredService<Modelos.InyeccionDeDependencias.Configuracion>().ProveedorPagoResultados;
                return string.Equals(proveedor, "LemonSqueezy", StringComparison.OrdinalIgnoreCase)
                    ? ActivatorUtilities.CreateInstance<PasarelaPagoLemonSqueezy>(sp)
                    : (IPasarelaPagoResultados)ActivatorUtilities.CreateInstance<PasarelaPagoStripe>(sp);
            });

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

