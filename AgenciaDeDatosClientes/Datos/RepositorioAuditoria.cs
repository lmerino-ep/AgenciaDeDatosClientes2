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
        public async Task<long> RegistrarAccionAsync(Auditoria auditoria)
        {
            return await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    INSERT INTO AuditoriaAccion (
                        FK_Usuario,
                        Accion,
                        Modulo,
                        Descripcion,
                        EntidadAfectada,
                        IdEntidadAfectada,
                        CambiosRealizados,
                        DireccionIP,
                        UserAgent,
                        DuracionMs,
                        Resultado,
                        MensajeError
                    ) VALUES (
                        @FK_Usuario,
                        @Accion,
                        @Modulo,
                        @Descripcion,
                        @EntidadAfectada,
                        @IdEntidadAfectada,
                        @CambiosRealizados,
                        @DireccionIP,
                        @UserAgent,
                        @DuracionMs,
                        @Resultado,
                        @MensajeError
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
            string resultado = null,
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
                        AA.Id,
                        AA.FechaHora,
                        U.Email as UsuarioEmail,
                        U.Nombre as UsuarioNombre,
                        AA.Accion,
                        AA.Modulo,
                        AA.Descripcion,
                        AA.Resultado,
                        AA.DireccionIP
                    FROM AuditoriaAccion AA
                    INNER JOIN Usuario U ON AA.FK_Usuario = U.Id
                    WHERE
                        (AA.FK_Usuario = @UsuarioId OR @UsuarioId IS NULL)
                        AND (AA.Accion = @Accion OR @Accion IS NULL)
                        AND (AA.Modulo = @Modulo OR @Modulo IS NULL)
                        AND (AA.Resultado = @Resultado OR @Resultado IS NULL)
                        AND (AA.FechaHora >= @FechaInicio OR @FechaInicio IS NULL)
                        AND (AA.FechaHora <= @FechaFin OR @FechaFin IS NULL)
                    ORDER BY AA.FechaHora DESC
                    LIMIT @Limit OFFSET @Offset;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<AuditoriaConUsuario>(consulta, new
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Modulo = modulo,
                    Resultado = resultado,
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
        public async Task<AuditoriaAccion> ObtenerAuditoriaPorIdAsync(long id)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT * FROM AuditoriaAccion WHERE Id = @Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleOrDefaultAsync<AuditoriaAccion>(consulta, new { Id = id });
            }
        }

        /// <summary>
        /// Obtiene el total de registros de auditoría con filtros
        /// </summary>
        public async Task<int> ObtenerTotalAuditoriasAsync(
            int? usuarioId = null,
            string accion = null,
            string modulo = null,
            string resultado = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT COUNT(*)
                    FROM AuditoriaAccion AA
                    WHERE
                        (AA.FK_Usuario = @UsuarioId OR @UsuarioId IS NULL)
                        AND (AA.Accion = @Accion OR @Accion IS NULL)
                        AND (AA.Modulo = @Modulo OR @Modulo IS NULL)
                        AND (AA.Resultado = @Resultado OR @Resultado IS NULL)
                        AND (AA.FechaHora >= @FechaInicio OR @FechaInicio IS NULL)
                        AND (AA.FechaHora <= @FechaFin OR @FechaFin IS NULL);

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleAsync<int>(consulta, new
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Modulo = modulo,
                    Resultado = resultado,
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
                        Modulo,
                        COUNT(*) as Total
                    FROM AuditoriaAccion
                    WHERE DATE(FechaHora) = CURDATE()
                    GROUP BY Modulo
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
                        DATE(FechaHora) as Fecha,
                        COUNT(*) as Total
                    FROM AuditoriaAccion
                    WHERE FechaHora >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                    GROUP BY DATE(FechaHora)
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
    }
}
