namespace Modelos.Dashboard;

/// <summary>
/// Métricas agregadas para la home del proveedor de datos (sin PII).
/// </summary>
public class ResumenDashboardProveedor
{
    public int DescargasUltimos30Dias { get; set; }

    public int ActividadMesActual { get; set; }

    public int UsuariosUnicosUltimos30Dias { get; set; }

    public int RepresentacionesActivasConDatos { get; set; }

    public int SeriesEnFuente { get; set; }
}
