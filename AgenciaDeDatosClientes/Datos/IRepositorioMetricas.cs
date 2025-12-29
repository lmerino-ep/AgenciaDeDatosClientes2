using Modelos.DTO.Metricas;

namespace Datos
{
    public interface IRepositorioMetricas
    {
        Task<ResultadoKpi> EjecutarKpiAsync(string kpiId, Dictionary<string, object> parametros);
        List<Kpi> ObtenerKpisDisponibles();
    }
}

