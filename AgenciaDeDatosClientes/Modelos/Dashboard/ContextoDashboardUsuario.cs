namespace Modelos.Dashboard;

/// <summary>
/// Contexto resuelto del usuario autenticado para Home, menú y métricas.
/// </summary>
public class ContextoDashboardUsuario
{
    public PerfilDashboard Perfil { get; init; }

    public int FkUsuario { get; init; }

    public IReadOnlyList<FuenteProveedorVinculo> FuentesVinculadas { get; init; } = [];

    public bool PuedeConsultarMetricasGlobales { get; init; }

    public bool PuedeConsultarMetricasProveedor { get; init; }

    public bool PuedeExportarMetricasGlobales { get; init; }

    public bool PuedeExportarMetricasProveedor { get; init; }
}
