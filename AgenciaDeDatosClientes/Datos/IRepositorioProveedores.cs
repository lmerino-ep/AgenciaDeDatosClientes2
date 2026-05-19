using Modelos.Dashboard;

namespace Datos;

public interface IRepositorioProveedores
{
    Task<IReadOnlyList<FuenteProveedorVinculo>> ObtenerFuentesVinculadasAsync(int fkUsuario);

    Task<bool> UsuarioTieneAccesoAFuenteAsync(int fkUsuario, int fkFuente);
}
