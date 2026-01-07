using Modelos;
using Modelos.InyeccionDeDependencias;
using Dapper;
using MySql.Data.MySqlClient;
using Modelos.DTO;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones de auditoría
    /// </summary>
    public class RepositorioAuditoria : BaseRepositorio
    {
        public RepositorioAuditoria(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Registra una acción de auditoría
        /// </summary>
        public async Task<long> RegistrarAccionAsync(AuditoriaDTO auditoria)
        {
            return await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    INSERT INTO Auditoria (
                        FK_Usuario,
                        FK_AuditoriaAccion,
                        Descripcion
                    ) VALUES (
                        @FK_Usuario,
                        @FK_AuditoriaAccion,
                        @Descripcion
                    );
                    SELECT LAST_INSERT_ID();
                ";

                return await conexion.QuerySingleAsync<long>(consulta, auditoria, transaccion);
            });
        }

        /// <summary>
        /// Obtiene registros de auditoría con filtros
        /// </summary>
        public async Task<List<AuditoriaConUsuario>> ObtenerAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            int limit = 100,
            int offset = 0)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        A.Id,
                        A.LastUpdate,
                        U.Email as UsuarioEmail,
                        U.Nombre as UsuarioNombre,
                        AA.Nombre as AccionNombre,
                        AC.Nombre as CategoriaNombre,
                        A.Descripcion
                    FROM Auditoria A
                    INNER JOIN Usuario U ON A.FK_Usuario = U.Id
                    INNER JOIN AuditoriaAccion AA ON A.FK_AuditoriaAccion = AA.Id
                    INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                    WHERE
                        (A.FK_Usuario = @UsuarioId OR @UsuarioId IS NULL)
                        AND (AA.Nombre = @Accion OR @Accion IS NULL)
                        AND (AC.Nombre = @Modulo OR @Modulo IS NULL)
                        AND (A.LastUpdate >= @FechaInicio OR @FechaInicio IS NULL)
                        AND (A.LastUpdate <= @FechaFin OR @FechaFin IS NULL)
                    ORDER BY A.LastUpdate DESC
                    LIMIT @Limit OFFSET @Offset;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<AuditoriaConUsuario>(consulta, new
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Modulo = modulo,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Limit = limit,
                    Offset = offset
                })).ToList();
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de una acción de auditoría
        /// </summary>
        public async Task<AuditoriaDetalle> ObtenerAuditoriaPorIdAsync(long id)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        A.Id,
                        A.FK_Usuario,
                        AA.Nombre as AccionNombre,
                        AC.Nombre as CategoriaNombre,
                        A.Descripcion,
                        A.LastUpdate
                    FROM Auditoria A
                    INNER JOIN AuditoriaAccion AA ON A.FK_AuditoriaAccion = AA.Id
                    INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                    WHERE A.Id = @Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleOrDefaultAsync<AuditoriaDetalle>(consulta, new { Id = id });
            }
        }

        /// <summary>
        /// Obtiene el ID de una acción de auditoría por su nombre
        /// </summary>
        public async Task<int?> ObtenerIdAccionPorNombreAsync(string nombreAccion)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT Id
                    FROM AuditoriaAccion
                    WHERE Nombre = @Nombre
                    LIMIT 1;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleOrDefaultAsync<int?>(consulta, new { Nombre = nombreAccion });
            }
        }

        /// <summary>
        /// Obtiene el nombre de la acción y categoría por ID de acción
        /// </summary>
        public async Task<(string AccionNombre, string CategoriaNombre)> ObtenerInfoAccionPorIdAsync(int idAccion)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT 
                        AA.Nombre as AccionNombre,
                        AC.Nombre as CategoriaNombre
                    FROM AuditoriaAccion AA
                    INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                    WHERE AA.Id = @Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleOrDefaultAsync<(string AccionNombre, string CategoriaNombre)>(
                    consulta, 
                    new { Id = idAccion }
                );
            }
        }

        /// <summary>
        /// Obtiene el total de registros de auditoría con filtros
        /// </summary>
        public async Task<int> ObtenerTotalAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT COUNT(*)
                    FROM Auditoria A
                    INNER JOIN AuditoriaAccion AA ON A.FK_AuditoriaAccion = AA.Id
                    INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                    WHERE
                        (A.FK_Usuario = @UsuarioId OR @UsuarioId IS NULL)
                        AND (AA.Nombre = @Accion OR @Accion IS NULL)
                        AND (AC.Nombre = @Modulo OR @Modulo IS NULL)
                        AND (A.LastUpdate >= @FechaInicio OR @FechaInicio IS NULL)
                        AND (A.LastUpdate <= @FechaFin OR @FechaFin IS NULL);

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleAsync<int>(consulta, new
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Modulo = modulo,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });
            }
        }

        /// <summary>
        /// Obtiene acciones agrupadas por módulo de hoy
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerAccionesPorModuloHoyAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        AC.Nombre as Modulo,
                        COUNT(*) as Total
                    FROM Auditoria A
                    INNER JOIN AuditoriaAccion AA ON A.FK_AuditoriaAccion = AA.Id
                    INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                    WHERE DATE(A.LastUpdate) = CURDATE()
                    GROUP BY AC.Nombre
                    ORDER BY Total DESC;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var resultado = await conexion.QueryAsync<(string Modulo, int Total)>(consulta);
                return resultado.ToDictionary(r => r.Modulo, r => r.Total);
            }
        }

        /// <summary>
        /// Obtiene tendencia de acciones de los últimos 7 días
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerTendenciaAccionesUltimos7DiasAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        DATE(A.LastUpdate) as Fecha,
                        COUNT(*) as Total
                    FROM Auditoria A
                    WHERE A.LastUpdate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                    GROUP BY DATE(A.LastUpdate)
                    ORDER BY Fecha ASC;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var resultado = await conexion.QueryAsync<(DateTime Fecha, int Total)>(consulta);
                return resultado.ToDictionary(
                    r => r.Fecha.ToString("yyyy-MM-dd"),
                    r => r.Total
                );
            }
        }

        /// <summary>
        /// Obtiene todas las acciones de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerAccionesDisponiblesAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT DISTINCT AA.Nombre
                    FROM AuditoriaAccion AA
                    ORDER BY AA.Nombre;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var resultado = await conexion.QueryAsync<string>(consulta);
                return resultado.ToList();
            }
        }

        /// <summary>
        /// Obtiene todas las categorías de auditoría disponibles
        /// </summary>
        public async Task<List<string>> ObtenerCategoriasDisponiblesAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT DISTINCT AC.Nombre
                    FROM AuditoriaCategoria AC
                    ORDER BY AC.Nombre;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var resultado = await conexion.QueryAsync<string>(consulta);
                return resultado.ToList();
            }
        }
    }
}
