using Modelos.Dashboard;
using Modelos.DTO.Metricas;

namespace Datos;

public interface IRepositorioMetricasProveedor
{
    List<Kpi> ObtenerKpisDisponibles();

    Task<ResultadoKpi> EjecutarKpiAsync(string kpiId, int fkFuente, Dictionary<string, object> parametros);

    Task<ResumenDashboardProveedor> ObtenerResumenDashboardAsync(int fkFuente);

    Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync(int fkFuente);

    Task<Dictionary<string, int>> ObtenerActividadDiariaAsync(int fkFuente, int dias);

    Task<Dictionary<string, int>> ObtenerDescargasPorFormatoAsync(int fkFuente);
}
