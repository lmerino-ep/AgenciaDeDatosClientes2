using Negocio;
using System.Diagnostics;
using System.Security.Claims;

namespace Utils.Middlewares.Auditoria
{
    /// <summary>
    /// Middleware para auditoría automática de acciones administrativas
    /// </summary>
    public class MiddlewareAuditoria
    {
        private readonly RequestDelegate _siguiente;
        private readonly ILogger<MiddlewareAuditoria> _logger;

        public MiddlewareAuditoria(RequestDelegate siguiente, ILogger<MiddlewareAuditoria> logger)
        {
            _siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext contexto, IServicioAuditoria servicioAuditoria)
        {
            // Solo auditar peticiones de modificación (POST, PUT, DELETE, PATCH)
            if (contexto.Request.Method == "GET" || contexto.Request.Method == "HEAD" || contexto.Request.Method == "OPTIONS")
            {
                await _siguiente(contexto);
                return;
            }

            // Solo auditar rutas específicas del dashboard
            string ruta = contexto.Request.Path.Value?.ToLowerInvariant() ?? "";
            bool debeAuditar = RutaDebeSerAuditada(ruta);

            if (!debeAuditar)
            {
                await _siguiente(contexto);
                return;
            }

            Stopwatch cronometro = Stopwatch.StartNew();
            Exception excepcion = null;

            try
            {
                await _siguiente(contexto);
                cronometro.Stop();
            }
            catch (Exception ex)
            {
                cronometro.Stop();
                excepcion = ex;
            }

            // Registrar en auditoría
            try
            {
                if (contexto.User?.Identity?.IsAuthenticated == true)
                {
                    Modelos.AuditoriaAccionEnum? accion = DeterminarAccion(ruta, contexto.Request.Method);
                    
                    if (accion.HasValue)
                    {
                        string descripcion = $"{contexto.Request.Method} {contexto.Request.Path}";
                        string resultado = excepcion != null ? "Error" : (contexto.Response.StatusCode < 400 ? "Exito" : "Error");
                        string mensajeError = excepcion?.Message;

                        await servicioAuditoria.RegistrarAccionAsync(
                            contexto.User,
                            accion.Value,
                            $"{descripcion} - {resultado}" + (mensajeError != null ? $" - {mensajeError}" : "")
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar acción de auditoría");
                // No lanzar excepción para no romper el flujo principal
            }

            // Re-lanzar excepción original si hubo error
            if (excepcion != null)
            {
                throw excepcion;
            }
        }

        /// <summary>
        /// Determina si una ruta debe ser auditada
        /// </summary>
        private bool RutaDebeSerAuditada(string ruta)
        {
            string[] rutasAuditadas = new[]
            {
                "/usuarios/crear",
                "/usuarios/editar",
                "/usuarios/eliminar",
                "/usuarios/cambiarcontrasena",
                "/usuarios/cambiartipo",
                "/usuarios/roles/editar",
                "/usuarios/tokens/generar",
                "/usuarios/tokens/revocar",
                "/metricas/exportar",
                "/documentacion/subir",
                "/documentacion/eliminar",
                "/documentacion/sobrescribir"
            };

            return rutasAuditadas.Any(r => ruta.Contains(r));
        }

        /// <summary>
        /// Determina la acción basándose en la ruta
        /// </summary>
        private Modelos.AuditoriaAccionEnum? DeterminarAccion(string ruta, string metodoHttp)
        {
            if (ruta.Contains("/usuarios/crear"))
                return Modelos.AuditoriaAccionEnum.UsuarioCrear;

            if (ruta.Contains("/usuarios/editar"))
                return Modelos.AuditoriaAccionEnum.UsuarioEditar;

            if (ruta.Contains("/usuarios/eliminar"))
                return Modelos.AuditoriaAccionEnum.UsuarioEliminar;

            if (ruta.Contains("/usuarios/cambiarcontrasena"))
                return Modelos.AuditoriaAccionEnum.UsuarioCambiarContrasena;

            if (ruta.Contains("/usuarios/cambiartipo"))
                return Modelos.AuditoriaAccionEnum.UsuarioCambiarTipo;

            if (ruta.Contains("/usuarios/roles/editar"))
                return Modelos.AuditoriaAccionEnum.RolAsignar;

            if (ruta.Contains("/usuarios/tokens/generar"))
            {
                if (ruta.Contains("token1"))
                    return Modelos.AuditoriaAccionEnum.TokenGenerarRefreshToken1;
                if (ruta.Contains("token2"))
                    return Modelos.AuditoriaAccionEnum.TokenGenerarRefreshToken2;
                return Modelos.AuditoriaAccionEnum.TokenGenerarRefreshToken1; // Por defecto usar Token1 cuando se generan ambos
            }

            if (ruta.Contains("/usuarios/tokens/revocar"))
                return Modelos.AuditoriaAccionEnum.TokenRevocarTokens;

            if (ruta.Contains("/metricas/exportar"))
                return Modelos.AuditoriaAccionEnum.MetricaExportar;

            if (ruta.Contains("/documentacion/subir"))
                return Modelos.AuditoriaAccionEnum.DocumentacionSubir;

            if (ruta.Contains("/documentacion/eliminar"))
                return Modelos.AuditoriaAccionEnum.DocumentacionEliminar;

            if (ruta.Contains("/documentacion/sobrescribir"))
                return Modelos.AuditoriaAccionEnum.DocumentacionSobrescribir;

            return null; // Acción desconocida, no se audita
        }
    }

    /// <summary>
    /// Extensión para registrar el middleware de auditoría
    /// </summary>
    public static class MiddlewareAuditoriaExtensions
    {
        public static IApplicationBuilder UseMiddlewareAuditoria(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MiddlewareAuditoria>();
        }
    }
}

