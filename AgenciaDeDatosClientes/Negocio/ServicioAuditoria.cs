using Datos;
using Modelos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using Modelos.DTO;

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de auditoría
    /// </summary>
    public class ServicioAuditoria : IServicioAuditoria
    {
        private readonly RepositorioAuditoria _repositorio;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ServicioAuditoria> _logger;

        public ServicioAuditoria(
            RepositorioAuditoria repositorio,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ServicioAuditoria> logger)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registra una acción de auditoría con un DTO completo
        /// </summary>
        public async Task<long> RegistrarAccionAsync(Auditoria auditoria)
        {
            try
            {
                return await _repositorio.RegistrarAccionAsync(auditoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar acción de auditoría");
                throw;
            }
        }

        /// <summary>
        /// Registra una acción de auditoría extrayendo automáticamente datos del HttpContext
        /// </summary>
        public async Task<long> RegistrarAccionAsync(
            ClaimsPrincipal usuario,
            string accion,
            string modulo,
            string descripcion,
            string resultado = "Exito",
            int? duracionMs = null,
            string mensajeError = null)
        {
            try
            {
                HttpContext contexto = _httpContextAccessor.HttpContext;
                if (contexto == null)
                {
                    _logger.LogWarning("No se pudo obtener HttpContext para auditoría");
                    return 0;
                }

                // Obtener ID de usuario del claim
                string usuarioIdClaim = usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(usuarioIdClaim, out int usuarioId))
                {
                    _logger.LogWarning("No se pudo obtener ID de usuario para auditoría");
                    return 0;
                }

                Auditoria auditoria = new Auditoria
                {
                    FK_Usuario = usuarioId,
                    Accion = accion,
                    Modulo = modulo,
                    Descripcion = descripcion,
                    DireccionIP = contexto.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = contexto.Request.Headers["User-Agent"].ToString(),
                    DuracionMs = duracionMs,
                    Resultado = resultado,
                    MensajeError = mensajeError
                };

                return await _repositorio.RegistrarAccionAsync(auditoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar acción de auditoría");
                // No lanzar excepción para no romper el flujo principal
                return 0;
            }
        }

        /// <summary>
        /// Obtiene registros de auditoría con filtros
        /// </summary>
        public async Task<List<AuditoriaConUsuario>> ObtenerAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            string resultado = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            int limit = 100,
            int offset = 0)
        {
            try
            {
                return await _repositorio.ObtenerAuditoriasAsync(
                    usuarioId,
                    accion,
                    modulo,
                    resultado,
                    fechaInicio,
                    fechaFin,
                    limit,
                    offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener auditorías");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de una acción de auditoría
        /// </summary>
        public async Task<AuditoriaAccion> ObtenerAuditoriaPorIdAsync(long id)
        {
            try
            {
                return await _repositorio.ObtenerAuditoriaPorIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener auditoría por ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene el total de registros de auditoría con filtros
        /// </summary>
        public async Task<int> ObtenerTotalAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            string resultado = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            try
            {
                return await _repositorio.ObtenerTotalAuditoriasAsync(
                    usuarioId,
                    accion,
                    modulo,
                    resultado,
                    fechaInicio,
                    fechaFin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener total de auditorías");
                throw;
            }
        }
    }
}
