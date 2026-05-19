using Modelos.Dashboard;

namespace Negocio;

public interface IServicioContextoDashboard
{
    Task<ContextoDashboardUsuario> ObtenerContextoAsync();

    FuenteProveedorSeleccion ResolverFuenteProveedor(ContextoDashboardUsuario contexto, int? fkFuenteSolicitada);

    bool EsKpiPermitidoParaContexto(ContextoDashboardUsuario contexto, string kpiId);
}
