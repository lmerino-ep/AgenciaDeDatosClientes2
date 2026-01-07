using Modelos.Entidades;
using Modelos.InyeccionDeDependencias;
using Dapper;
using MySql.Data.MySqlClient;
using Modelos.DTO.Usuarios;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones con usuarios
    /// </summary>
    public class RepositorioUsuarios : BaseRepositorio
    {
        public RepositorioUsuarios(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Lista usuarios con filtros y paginación
        /// </summary>
        public async Task<(List<UsuarioListado> usuarios, int total)> ListarUsuariosAsync(
            int? fkUsuarioTipo = null,
            bool? activo = null,
            string busqueda = null,
            int pagina = 0,
            int tamanoPagina = 25)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                int offset = pagina * tamanoPagina;

                // Construir WHERE dinámicamente
                var condiciones = new List<string>();
                var parametros = new DynamicParameters();

                if (fkUsuarioTipo.HasValue)
                {
                    condiciones.Add("U.FK_UsuarioTipo = @FK_UsuarioTipo");
                    parametros.Add("FK_UsuarioTipo", fkUsuarioTipo.Value);
                }

                if (activo.HasValue)
                {
                    condiciones.Add("U.Activo = @Activo");
                    parametros.Add("Activo", activo.Value);
                }

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    condiciones.Add("(U.Email LIKE @Busqueda OR U.Nombre LIKE @Busqueda OR U.Apellidos LIKE @Busqueda)");
                    parametros.Add("Busqueda", $"%{busqueda}%");
                }

                string whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";

                // Consulta para obtener usuarios
                string consultaUsuarios = $@"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        U.Id,
                        U.Email,
                        U.Nombre,
                        U.Apellidos,
                        U.Empresa,
                        U.Activo,
                        U.FK_UsuarioTipo,
                        UT.Nombre as TipoUsuarioNombre
                    FROM Usuario U
                    INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                    {whereClause}
                    ORDER BY U.Id DESC
                    LIMIT @Offset, @TamanoPagina;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                parametros.Add("Offset", offset);
                parametros.Add("TamanoPagina", tamanoPagina);

                // Consulta para contar total
                string consultaTotal = $@"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT COUNT(*)
                    FROM Usuario U
                    INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                    {whereClause};

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var usuarios = (await conexion.QueryAsync<UsuarioListado>(consultaUsuarios, parametros)).ToList();
                int total = await conexion.QueryFirstOrDefaultAsync<int>(consultaTotal, parametros);

                return (usuarios, total);
            }
        }

        /// <summary>
        /// Obtiene un usuario por ID
        /// </summary>
        public async Task<UsuarioDetalle> ObtenerUsuarioAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        U.Id,
                        U.Email,
                        U.Nombre,
                        U.Apellidos,
                        U.Empresa,
                        U.Activo,
                        U.FK_UsuarioTipo,
                        UT.Nombre as TipoUsuarioNombre
                    FROM Usuario U
                    INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                    WHERE U.Id = @IdUsuario;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QueryFirstOrDefaultAsync<UsuarioDetalle>(consulta, new { IdUsuario = idUsuario });
            }
        }

        /// <summary>
        /// Verifica si un email ya existe
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email, int? idUsuarioExcluir = null)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT COUNT(*)
                    FROM Usuario
                    WHERE UPPER(Email) = UPPER(@Email)
                ";

                var parametros = new DynamicParameters();
                parametros.Add("Email", email);

                if (idUsuarioExcluir.HasValue)
                {
                    consulta += " AND Id != @IdUsuarioExcluir";
                    parametros.Add("IdUsuarioExcluir", idUsuarioExcluir.Value);
                }

                consulta += @";
                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                int count = await conexion.QueryFirstOrDefaultAsync<int>(consulta, parametros);
                return count > 0;
            }
        }

        /// <summary>
        /// Actualiza la contraseña de un usuario
        /// </summary>
        public async Task ActualizarContrasenaAsync(int idUsuario, string passwordHash)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    UPDATE Usuario
                    SET Password = @Password
                    WHERE Id = @IdUsuario
                ";

                await conexion.ExecuteAsync(consulta, new { IdUsuario = idUsuario, Password = passwordHash }, transaccion);
            });
        }

        /// <summary>
        /// Actualiza el tipo de usuario
        /// </summary>
        public async Task ActualizarTipoUsuarioAsync(int idUsuario, int fkUsuarioTipo)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    UPDATE Usuario
                    SET FK_UsuarioTipo = @FK_UsuarioTipo
                    WHERE Id = @IdUsuario
                ";

                await conexion.ExecuteAsync(consulta, new { IdUsuario = idUsuario, FK_UsuarioTipo = fkUsuarioTipo }, transaccion);
            });
        }

        /// <summary>
        /// Elimina un usuario (soft delete)
        /// </summary>
        public async Task EliminarUsuarioAsync(int idUsuario)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    UPDATE Usuario
                    SET Activo = 0
                    WHERE Id = @IdUsuario
                ";

                await conexion.ExecuteAsync(consulta, new { IdUsuario = idUsuario }, transaccion);
            });
        }

        /// <summary>
        /// Obtiene todos los tipos de usuario
        /// </summary>
        public async Task<List<Modelos.Entidades.UsuarioTipo>> ObtenerTiposUsuarioAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT Id, Nombre
                    FROM UsuarioTipo
                    ORDER BY Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<Modelos.Entidades.UsuarioTipo>(consulta)).ToList();
            }
        }
    }
}

