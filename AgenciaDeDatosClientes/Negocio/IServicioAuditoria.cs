using Modelos;
using Modelos.DTO;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de auditoría
    /// </summary>
    public interface IServicioAuditoria
    {
        Task<long> RegistrarAccionAsync(AuditoriaDTO auditoria);
        Task<long> RegistrarAccionAsync(System.Security.Claims.ClaimsPrincipal usuario, AuditoriaAccionEnum accion, string descripcion);
        Task<List<AuditoriaConUsuario>> ObtenerAuditoriasAsync(int? usuarioId = null, string accion = null, string modulo = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, int limit = 100, int offset = 0);
        Task<AuditoriaDetalle> ObtenerAuditoriaPorIdAsync(long id);
        Task<int> ObtenerTotalAuditoriasAsync(int? usuarioId = null, string accion = null, string modulo = null, DateTime? fechaInicio = null, DateTime? fechaFin = null);
        
        // Métodos para gráficos del dashboard
        Task<Dictionary<string, int>> ObtenerAccionesPorModuloHoyAsync();
        Task<Dictionary<string, int>> ObtenerTendenciaAccionesUltimos7DiasAsync();
        
        // Métodos para obtener opciones de filtros
        Task<List<string>> ObtenerAccionesDisponiblesAsync();
        Task<List<string>> ObtenerCategoriasDisponiblesAsync();
    }
}

