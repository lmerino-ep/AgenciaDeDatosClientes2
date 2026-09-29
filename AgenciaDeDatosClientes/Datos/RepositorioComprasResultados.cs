using Dapper;
using AgenciaDeDatosBD;
using Modelos.DTO.ResultadosEmpresariales;
using Modelos.InyeccionDeDependencias;

namespace Datos
{
    public class RepositorioComprasResultados : BaseRepositorio, IRepositorioComprasResultados
    {
        public RepositorioComprasResultados(Aurora aurora) : base(aurora)
        {
        }

        // Lista cerrada: el ORDER BY no admite parámetros. Desempata por R.Id para que la paginación sea estable.
        private static string TraducirOrden(OrdenCatalogoResultados orden) => orden switch
        {
            OrdenCatalogoResultados.EmpresaDescendente => "E.Nombre DESC, R.Anio DESC, R.Id ASC",
            OrdenCatalogoResultados.SectorAscendente => "S.Nombre ASC, E.Nombre ASC, R.Id ASC",
            OrdenCatalogoResultados.MasRecientes => "R.FechaResultado DESC, R.Id DESC",
            _ => "E.Nombre ASC, R.Anio DESC, R.Id ASC"
        };

        public async Task<IReadOnlyList<SectorCatalogoItem>> ObtenerSectoresAsync()
        {
            const string sql = @"
                SELECT DISTINCT
                    S.Id,
                    S.Nombre
                FROM Sector S
                INNER JOIN Empresa E ON E.FK_Sector = S.Id
                INNER JOIN Resultado R ON R.FK_Empresa = E.Id
                WHERE R.Publicado = 1
                ORDER BY S.Nombre
            ;";

            using var connection = ObtenerConexionLectura();
            return (await connection.QueryAsync<SectorCatalogoItem>(sql)).ToList();
        }

        public async Task<(int Total, IReadOnlyList<CatalogoResultadoItem> Items)> ObtenerCatalogoAsync(int fkUsuario, int pagina, int tamPagina, FiltroCatalogoResultados filtro)
        {
            string condicionFiltros = @"
                AND (@TextoBusqueda IS NULL OR E.Nombre LIKE CONCAT('%', @TextoBusqueda, '%'))
                AND (@FkSector IS NULL OR E.FK_Sector = @FkSector)
                AND (@FechaDesde IS NULL OR R.FechaResultado >= @FechaDesde)
                AND (@FechaHasta IS NULL OR R.FechaResultado <= @FechaHasta)
            ";

            string sqlTotal = $@"
                SELECT COUNT(1)
                FROM Resultado R
                INNER JOIN Empresa E ON E.Id = R.FK_Empresa
                WHERE R.Publicado = 1
                {condicionFiltros}
            ;";

            string sqlPagina = $@"
                SELECT
                    R.Id AS ResultadoId,
                    E.Nombre AS EmpresaNombre,
                    S.Nombre AS SectorNombre,
                    P.Nombre AS PeriodoNombre,
                    R.Anio,
                    R.FechaResultado,
                    R.PrecioCentimos,
                    CASE WHEN C.Id IS NOT NULL THEN 1 ELSE 0 END AS YaComprado
                FROM Resultado R
                INNER JOIN Empresa E ON E.Id = R.FK_Empresa
                INNER JOIN Sector S ON S.Id = E.FK_Sector
                INNER JOIN Periodo P ON P.Id = R.FK_Periodo
                LEFT JOIN Compra C ON C.FK_Resultado = R.Id
                    AND C.FK_Usuario = @FkUsuario
                    AND C.Estado = 'Pagada'
                WHERE R.Publicado = 1
                {condicionFiltros}
                ORDER BY {TraducirOrden(filtro.Orden)}
                LIMIT @Offset, @TamPagina
            ;";

            var parametros = new
            {
                FkUsuario = fkUsuario,
                Offset = (pagina - 1) * tamPagina,
                TamPagina = tamPagina,
                filtro.TextoBusqueda,
                filtro.FkSector,
                filtro.FechaDesde,
                filtro.FechaHasta
            };

            using var connection = ObtenerConexionLectura();
            var total = await connection.ExecuteScalarAsync<int>(sqlTotal, parametros);
            var items = await connection.QueryAsync<CatalogoResultadoItem>(sqlPagina, parametros);

            return (total, items.ToList());
        }

        public async Task<IReadOnlyList<DocumentoDeProducto>> ObtenerDocumentosDeProductoAsync(int resultadoId)
        {
            // Solo PDF: es lo unico que se puede descargar en EpData, asi que es lo unico que se anuncia.
            const string sql = @"
                SELECT
                    D.NombreVisible,
                    D.TipoDocumento
                FROM DocumentoResultado D
                INNER JOIN Resultado R ON R.Id = D.FK_Resultado
                WHERE
                    D.FK_Resultado = @ResultadoId
                    AND R.Publicado = 1
                    AND D.TipoDocumento = 'PDF'
                ORDER BY D.NombreVisible, D.Id
            ;";

            using var connection = ObtenerConexionLectura();
            return (await connection.QueryAsync<DocumentoDeProducto>(sql, new { ResultadoId = resultadoId })).ToList();
        }

        public async Task<ResultadoParaCompra?> ObtenerResultadoParaCompraAsync(int resultadoId, int fkUsuario)
        {
            const string sql = @"
                SELECT
                    R.Id AS ResultadoId,
                    E.Nombre AS EmpresaNombre,
                    S.Nombre AS SectorNombre,
                    P.Nombre AS PeriodoNombre,
                    R.Anio,
                    R.PrecioCentimos,
                    R.Publicado,
                    CASE WHEN C.Id IS NOT NULL THEN 1 ELSE 0 END AS YaComprado
                FROM Resultado R
                INNER JOIN Empresa E ON E.Id = R.FK_Empresa
                INNER JOIN Sector S ON S.Id = E.FK_Sector
                INNER JOIN Periodo P ON P.Id = R.FK_Periodo
                LEFT JOIN Compra C ON C.FK_Resultado = R.Id
                    AND C.FK_Usuario = @FkUsuario
                    AND C.Estado = 'Pagada'
                WHERE R.Id = @ResultadoId
                LIMIT 1
            ;";

            using var connection = ObtenerConexionLectura();
            return await connection.QueryFirstOrDefaultAsync<ResultadoParaCompra>(sql, new { ResultadoId = resultadoId, FkUsuario = fkUsuario });
        }

        public async Task<bool> YaCompradoAsync(int fkUsuario, int resultadoId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Compra C
                WHERE C.FK_Usuario = @FkUsuario
                    AND C.FK_Resultado = @ResultadoId
                    AND C.Estado = 'Pagada'
            ;";

            using var connection = ObtenerConexionLectura();
            var total = await connection.ExecuteScalarAsync<int>(sql, new { FkUsuario = fkUsuario, ResultadoId = resultadoId });
            return total > 0;
        }

        public async Task RegistrarCompraPendienteAsync(int fkUsuario, int resultadoId, string checkoutId)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.Compra().CrearOActualizarPendiente(fkUsuario, resultadoId, checkoutId);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task ConfirmarCompraAsync(
            int fkUsuario,
            int resultadoId,
            string identificadorPedido,
            int? numeroPedido,
            int? importeCentimos,
            string moneda,
            string emailComprador,
            bool esTest,
            DateTime fechaCompra)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.Compra().ConfirmarPago(
                fkUsuario,
                resultadoId,
                identificadorPedido,
                numeroPedido,
                importeCentimos,
                moneda,
                emailComprador,
                esTest,
                fechaCompra);

            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task MarcarReembolsadaAsync(string identificadorPedido)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.Compra().MarcarReembolsada(identificadorPedido);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task MarcarFallidaAsync(int fkUsuario, int resultadoId, string checkoutId)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.Compra().MarcarFallida(fkUsuario, resultadoId, checkoutId);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task<bool> RegistrarEventoWebhookAsync(string nombreEvento, string identificadorPedido)
        {
            using var bd = new BD(_aurora.Lectura, _aurora.Escritura);
            var retorno = await bd.LemonSqueezyWebhookEvento().Crear(nombreEvento, identificadorPedido);
            if (retorno.Correcto)
            {
                return true;
            }

            if (retorno.Mensaje != null && retorno.Mensaje.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new Exception(retorno.Mensaje);
        }
    }
}
