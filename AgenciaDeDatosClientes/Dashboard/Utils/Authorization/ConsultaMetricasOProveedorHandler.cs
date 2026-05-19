using Microsoft.AspNetCore.Authorization;
using Negocio;

namespace Utils.Authorization;

public class ConsultaMetricasOProveedorHandler : AuthorizationHandler<ConsultaMetricasOProveedorRequirement>
{
    private readonly IServicioContextoDashboard _contextoDashboard;

    public ConsultaMetricasOProveedorHandler(IServicioContextoDashboard contextoDashboard)
    {
        _contextoDashboard = contextoDashboard;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConsultaMetricasOProveedorRequirement requirement)
    {
        if (context.User.IsInRole(PoliticasAutorizacion.Roles.ConsultaMetricas)
            || context.User.IsInRole(PoliticasAutorizacion.Roles.ExportacionMetricas)
            || context.User.IsInRole(PoliticasAutorizacion.Roles.AccesoTotal))
        {
            context.Succeed(requirement);
            return;
        }

        var dashboardContexto = await _contextoDashboard.ObtenerContextoAsync();
        if (dashboardContexto.PuedeConsultarMetricasProveedor)
        {
            context.Succeed(requirement);
        }
    }
}
