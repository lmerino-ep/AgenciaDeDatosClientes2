using Modelos.DTO.Metricas;

namespace Datos
{
    public interface IRepositorioMetricas
    {
        Task<ResultadoKpi> EjecutarKpiAsync(string kpiId, Dictionary<string, object> parametros);
        List<Kpi> ObtenerKpisDisponibles();
        
        // Métricas del dashboard principal
        Task<int> ObtenerUsuariosActivosAsync();
        Task<int> ObtenerActividadMensualAsync();
        Task<int> ObtenerAccionesHoyAsync();
        Task<int> ObtenerTotalUsuariosAsync();
        
        // Métodos para gráficos del dashboard
        Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync();
        Task<Dictionary<string, int>> ObtenerActividadDiariaUltimos7DiasAsync();
    }
}

