namespace Modelos.Dashboard;

/// <summary>
/// Fuente de datos vinculada a un usuario como titular o representante en Proveedor.
/// </summary>
public class FuenteProveedorVinculo
{
    public int ProveedorId { get; set; }

    public int FkFuente { get; set; }

    public string NombreFuente { get; set; } = string.Empty;

    public string NombreProveedor { get; set; } = string.Empty;
}
