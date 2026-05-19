using Dapper;
using Modelos.Dashboard;
using Modelos.InyeccionDeDependencias;

namespace Datos;

public class RepositorioProveedores : BaseRepositorio, IRepositorioProveedores
{
    public RepositorioProveedores(Aurora aurora) : base(aurora)
    {
    }

    public async Task<IReadOnlyList<FuenteProveedorVinculo>> ObtenerFuentesVinculadasAsync(int fkUsuario)
    {
        const string sql = @"
            SELECT
                P.Id AS ProveedorId,
                P.FK_Fuente AS FkFuente,
                F.Nombre AS NombreFuente,
                P.Nombre AS NombreProveedor
            FROM Proveedor P
            INNER JOIN Fuente F ON F.Id = P.FK_Fuente
            WHERE P.FK_Usuario = @FkUsuario
               OR P.FK_UsuarioRepresentante = @FkUsuario
            ORDER BY F.Nombre ASC, P.Id ASC
        ;";

        using var connection = ObtenerConexionLectura();
        var filas = await connection.QueryAsync<FuenteProveedorVinculo>(sql, new { FkUsuario = fkUsuario });
        return filas.ToList();
    }

    public async Task<bool> UsuarioTieneAccesoAFuenteAsync(int fkUsuario, int fkFuente)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM Proveedor P
            WHERE P.FK_Fuente = @FkFuente
              AND (P.FK_Usuario = @FkUsuario OR P.FK_UsuarioRepresentante = @FkUsuario)
        ;";

        using var connection = ObtenerConexionLectura();
        var total = await connection.ExecuteScalarAsync<int>(sql, new { FkUsuario = fkUsuario, FkFuente = fkFuente });
        return total > 0;
    }
}
