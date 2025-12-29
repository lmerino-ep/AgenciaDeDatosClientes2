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
                    string accion = DeterminarAccion(ruta, contexto.Request.Method);
                    string modulo = DeterminarModulo(ruta);
                    string descripcion = $"{contexto.Request.Method} {contexto.Request.Path}";
                    string resultado = excepcion != null ? "Error" : (contexto.Response.StatusCode < 400 ? "Exito" : "Error");
                    string mensajeError = excepcion?.Message;

                    await servicioAuditoria.RegistrarAccionAsync(
                        contexto.User,
                        accion,
                        modulo,
                        descripcion,
                        resultado,
                        (int)cronometro.ElapsedMilliseconds,
                        mensajeError
                    );
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
        private string DeterminarAccion(string ruta, string metodoHttp)
        {
            if (ruta.Contains("/usuarios/crear"))
                return "Usuario.Crear";

            if (ruta.Contains("/usuarios/editar"))
                return "Usuario.Editar";

            if (ruta.Contains("/usuarios/eliminar"))
                return "Usuario.Eliminar";

            if (ruta.Contains("/usuarios/cambiarcontrasena"))
                return "Usuario.CambiarContrasena";

            if (ruta.Contains("/usuarios/cambiartipo"))
                return "Usuario.CambiarTipo";

            if (ruta.Contains("/usuarios/roles/editar"))
                return "Roles.Asignar";

            if (ruta.Contains("/usuarios/tokens/generar"))
            {
                if (ruta.Contains("token1"))
                    return "Tokens.GenerarRefreshToken1";
                if (ruta.Contains("token2"))
                    return "Tokens.GenerarRefreshToken2";
                return "Tokens.GenerarRefreshTokens";
            }

            if (ruta.Contains("/usuarios/tokens/revocar"))
                return "Tokens.RevocarTokens";

            if (ruta.Contains("/metricas/exportar"))
                return "Metricas.Exportar";

            if (ruta.Contains("/documentacion/subir"))
                return "Documentacion.Subir";

            if (ruta.Contains("/documentacion/eliminar"))
                return "Documentacion.Eliminar";

            if (ruta.Contains("/documentacion/sobrescribir"))
                return "Documentacion.Sobrescribir";

            return $"{metodoHttp}.Desconocido";
        }

        /// <summary>
        /// Determina el módulo basándose en la ruta
        /// </summary>
        private string DeterminarModulo(string ruta)
        {
            if (ruta.Contains("/usuarios/roles"))
                return "Roles";

            if (ruta.Contains("/usuarios/tokens"))
                return "Tokens";

            if (ruta.Contains("/usuarios"))
                return "Usuarios";

            if (ruta.Contains("/metricas"))
                return "Metricas";

            if (ruta.Contains("/documentacion"))
                return "Documentacion";

            if (ruta.Contains("/auditoria"))
                return "Auditoria";

            return "Desconocido";
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

