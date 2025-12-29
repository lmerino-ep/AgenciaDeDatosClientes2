using Modelos.DTO.Metricas;

namespace Negocio
{
    public interface IServicioMetricas
    {
        Task<ResultadoKpi> ObtenerKpiAsync(string kpiId, Dictionary<string, object> parametros, bool forzarRecalculo = false);
        List<Kpi> ObtenerKpisDisponibles();
        Task<ResultadoKpi> ExportarKpiAsync(string kpiId, Dictionary<string, object> parametros, string formato);
        Task RegenerarKpisPrincipalesAsync();
    }
}

