using Modelos;
using Modelos.DTO;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de auditoría
    /// </summary>
    public interface IServicioAuditoria
    {
        Task<long> RegistrarAccionAsync(Auditoria auditoria);
        Task<long> RegistrarAccionAsync(System.Security.Claims.ClaimsPrincipal usuario, string accion, string modulo, string descripcion, string resultado = "Exito", int? duracionMs = null, string mensajeError = null);
        Task<List<AuditoriaConUsuario>> ObtenerAuditoriasAsync(int? usuarioId = null, string accion = null, string modulo = null, string resultado = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, int limit = 100, int offset = 0);
        Task<AuditoriaDetalle> ObtenerAuditoriaPorIdAsync(long id);
        Task<int> ObtenerTotalAuditoriasAsync(int? usuarioId = null, string accion = null, string modulo = null, string resultado = null, DateTime? fechaInicio = null, DateTime? fechaFin = null);
        
        // Métodos para gráficos del dashboard
        Task<Dictionary<string, int>> ObtenerAccionesPorModuloHoyAsync();
        Task<Dictionary<string, int>> ObtenerTendenciaAccionesUltimos7DiasAsync();
    }
}

