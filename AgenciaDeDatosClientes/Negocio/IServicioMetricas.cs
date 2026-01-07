using Modelos.DTO.Metricas;

namespace Negocio
{
    public interface IServicioMetricas
    {
        Task<ResultadoKpi> ObtenerKpiAsync(string kpiId, Dictionary<string, object> parametros, bool forzarRecalculo = false);
        List<Kpi> ObtenerKpisDisponibles();
        Task<ResultadoKpi> ExportarKpiAsync(string kpiId, Dictionary<string, object> parametros, string formato);
        Task RegenerarKpisPrincipalesAsync();
        
        // Métricas del dashboard principal (con caché)
        Task<int> ObtenerUsuariosActivosAsync();
        Task<int> ObtenerActividadMensualAsync();
        Task<int> ObtenerAccionesHoyAsync();
        Task<int> ObtenerTotalUsuariosAsync();
        
        // Métodos para gráficos del dashboard
        Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync();
        Task<Dictionary<string, int>> ObtenerActividadDiariaUltimos7DiasAsync();
    }
}

