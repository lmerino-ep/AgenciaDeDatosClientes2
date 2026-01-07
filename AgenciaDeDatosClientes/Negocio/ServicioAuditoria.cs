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
        public async Task<long> RegistrarAccionAsync(AuditoriaDTO auditoria)
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
        /// Registra una acción de auditoría usando enumerados
        /// </summary>
        public async Task<long> RegistrarAccionAsync(
            ClaimsPrincipal usuario,
            AuditoriaAccionEnum accion,
            string descripcion)
        {
            try
            {
                // Obtener ID de usuario del claim
                string usuarioIdClaim = usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(usuarioIdClaim, out int usuarioId))
                {
                    _logger.LogWarning("No se pudo obtener ID de usuario para auditoría");
                    return 0;
                }

                // Obtener el nombre de la acción desde el enum
                string nombreAccion = EnumeradosAuditoriaHelper.ObtenerNombreAccion(accion);
                
                // Buscar el ID de la acción en la BD por nombre
                int? idAccion = await _repositorio.ObtenerIdAccionPorNombreAsync(nombreAccion);
                
                if (!idAccion.HasValue)
                {
                    _logger.LogWarning("No se encontró la acción de auditoría '{NombreAccion}' en la base de datos", nombreAccion);
                    return 0;
                }

                AuditoriaDTO auditoria = new AuditoriaDTO
                {
                    FK_Usuario = usuarioId,
                    FK_AuditoriaAccion = idAccion.Value,
                    Descripcion = descripcion
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
        public async Task<AuditoriaDetalle> ObtenerAuditoriaPorIdAsync(long id)
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
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            try
            {
                return await _repositorio.ObtenerTotalAuditoriasAsync(
                    usuarioId,
                    accion,
                    modulo,
                    fechaInicio,
                    fechaFin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener total de auditorías");
                throw;
            }
        }

        /// <summary>
        /// Obtiene acciones agrupadas por módulo de hoy
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerAccionesPorModuloHoyAsync()
        {
            try
            {
                return await _repositorio.ObtenerAccionesPorModuloHoyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener acciones por módulo de hoy");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Obtiene tendencia de acciones de los últimos 7 días
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerTendenciaAccionesUltimos7DiasAsync()
        {
            try
            {
                return await _repositorio.ObtenerTendenciaAccionesUltimos7DiasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tendencia de acciones");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Obtiene todas las acciones de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerAccionesDisponiblesAsync()
        {
            try
            {
                return await _repositorio.ObtenerAccionesDisponiblesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener acciones disponibles");
                return new List<string>();
            }
        }

        /// <summary>
        /// Obtiene todas las categorías de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerCategoriasDisponiblesAsync()
        {
            try
            {
                return await _repositorio.ObtenerCategoriasDisponiblesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías disponibles");
                return new List<string>();
            }
        }
    }
}
