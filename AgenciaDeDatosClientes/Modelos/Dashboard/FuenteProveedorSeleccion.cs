namespace Modelos.Dashboard;

/// <summary>
/// Fuente de datos elegida por un proveedor en Home o KPIs (parámetro de consulta).
/// </summary>
public class FuenteProveedorSeleccion
{
    public int FkFuente { get; init; }

    public string NombreFuente { get; init; } = string.Empty;
}
