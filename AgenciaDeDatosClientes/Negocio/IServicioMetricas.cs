using Modelos.Dashboard;
using Modelos.DTO.Metricas;

namespace Negocio;

public interface IServicioMetricas
{
    Task<ResultadoKpi> ObtenerKpiAsync(
        string kpiId,
        Dictionary<string, object> parametros,
        bool forzarRecalculo = false,
        int? fkFuenteProveedor = null);

    Task<List<Kpi>> ObtenerKpisDisponiblesAsync();

    Task<ResultadoKpi> ExportarKpiAsync(
        string kpiId,
        Dictionary<string, object> parametros,
        string formato,
        int? fkFuenteProveedor = null);

    Task RegenerarKpisPrincipalesAsync();

    Task<ResumenDashboardProveedor> ObtenerResumenDashboardProveedorAsync(int fkFuente);

    Task<Dictionary<string, int>> ObtenerActividadMensualProveedorAsync(int fkFuente);

    Task<Dictionary<string, int>> ObtenerActividadDiariaProveedorAsync(int fkFuente);

    Task<Dictionary<string, int>> ObtenerDescargasPorFormatoProveedorAsync(int fkFuente);

    Task<int> ObtenerUsuariosActivosAsync();

    Task<int> ObtenerActividadMensualAsync();

    Task<int> ObtenerAccionesHoyAsync();

    Task<int> ObtenerTotalUsuariosAsync();

    Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync();

    Task<Dictionary<string, int>> ObtenerActividadDiariaUltimos7DiasAsync();
}
