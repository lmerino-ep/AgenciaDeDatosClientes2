using Modelos.Entidades;
using Modelos.InyeccionDeDependencias;
using Dapper;
using MySql.Data.MySqlClient;
using Modelos.DTO.Roles;

namespace Datos
{
    /// <summary>
    /// Repositorio para operaciones con roles de usuario
    /// </summary>
    public class RepositorioRoles : BaseRepositorio
    {
        public RepositorioRoles(Aurora aurora) : base(aurora)
        {
        }

        /// <summary>
        /// Obtiene todos los roles del sistema
        /// </summary>
        public async Task<List<Modelos.Entidades.Rol>> ObtenerTodosLosRolesAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT Id, Nombre, Descripcion
                    FROM Rol
                    ORDER BY Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<Modelos.Entidades.Rol>(consulta)).ToList();
            }
        }

        /// <summary>
        /// Obtiene los roles asignados a un usuario
        /// </summary>
        public async Task<List<Modelos.Entidades.Rol>> ObtenerRolesUsuarioAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        R.Id,
                        R.Nombre,
                        R.Descripcion
                    FROM UsuarioRol UR
                    INNER JOIN Rol R ON UR.FK_Rol = R.Id
                    WHERE UR.FK_Usuario = @UsuarioId
                    ORDER BY R.Nombre;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<Modelos.Entidades.Rol>(consulta, new { UsuarioId = idUsuario })).ToList();
            }
        }

        /// <summary>
        /// Obtiene los roles NO asignados a un usuario
        /// </summary>
        public async Task<List<Modelos.Entidades.Rol>> ObtenerRolesDisponiblesAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        R.Id,
                        R.Nombre,
                        R.Descripcion
                    FROM Rol R
                    WHERE R.Id NOT IN (
                        SELECT FK_Rol
                        FROM UsuarioRol
                        WHERE FK_Usuario = @UsuarioId
                    )
                    ORDER BY R.Nombre;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return (await conexion.QueryAsync<Modelos.Entidades.Rol>(consulta, new { UsuarioId = idUsuario })).ToList();
            }
        }

        /// <summary>
        /// Asigna roles a un usuario (reemplaza todos los roles existentes)
        /// </summary>
        public async Task AsignarRolesAsync(int idUsuario, List<int> idsRoles)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                // Eliminar roles actuales
                const string consultaEliminar = @"
                    DELETE FROM UsuarioRol
                    WHERE FK_Usuario = @UsuarioId;
                ";

                await conexion.ExecuteAsync(consultaEliminar, new { UsuarioId = idUsuario }, transaccion);

                // Insertar nuevos roles
                if (idsRoles != null && idsRoles.Any())
                {
                    const string consultaInsertar = @"
                        INSERT INTO UsuarioRol (FK_Usuario, FK_Rol)
                        VALUES (@UsuarioId, @RolId);
                    ";

                    foreach (int rolId in idsRoles)
                    {
                        await conexion.ExecuteAsync(consultaInsertar, new { UsuarioId = idUsuario, RolId = rolId }, transaccion);
                    }
                }
            });
        }

        /// <summary>
        /// Obtiene la información de usuario con sus roles
        /// </summary>
        public async Task<UsuarioRoles> ObtenerUsuarioConRolesAsync(int idUsuario)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        U.Id as IdUsuario,
                        U.Email,
                        U.Nombre,
                        U.Apellidos,
                        UT.Nombre as TipoUsuario
                    FROM Usuario U
                    INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                    WHERE U.Id = @UsuarioId;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                UsuarioRoles usuario = await conexion.QuerySingleOrDefaultAsync<UsuarioRoles>(consulta, new { UsuarioId = idUsuario });

                if (usuario == null)
                {
                    return null;
                }

                // Obtener roles asignados
                List<Modelos.Entidades.Rol> rolesAsignados = await ObtenerRolesUsuarioAsync(idUsuario);
                usuario.RolesAsignados = rolesAsignados.Select(r => new Modelos.DTO.Roles.Rol
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Asignado = true
                }).ToList();

                // Obtener roles disponibles
                List<Modelos.Entidades.Rol> rolesDisponibles = await ObtenerRolesDisponiblesAsync(idUsuario);
                usuario.RolesDisponibles = rolesDisponibles.Select(r => new Modelos.DTO.Roles.Rol
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Asignado = false
                }).ToList();

                return usuario;
            }
        }

        /// <summary>
        /// Crea un nuevo rol en el sistema
        /// </summary>
        public async Task<int> CrearRolAsync(string nombre, string descripcion)
        {
            return await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    INSERT INTO Rol (Nombre, Descripcion)
                    VALUES (@Nombre, @Descripcion);
                    SELECT LAST_INSERT_ID();
                ";

                return await conexion.QuerySingleAsync<int>(consulta, new { Nombre = nombre, Descripcion = descripcion }, transaccion);
            });
        }

        /// <summary>
        /// Verifica si existe un rol con el mismo nombre
        /// </summary>
        public async Task<bool> ExisteRolConNombreAsync(string nombre)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SELECT COUNT(*) > 0
                    FROM Rol
                    WHERE Nombre = @Nombre;
                ";

                return await conexion.QuerySingleAsync<bool>(consulta, new { Nombre = nombre });
            }
        }

        /// <summary>
        /// Obtiene el conteo de usuarios por rol
        /// </summary>
        public async Task<Dictionary<int, int>> ObtenerConteoUsuariosPorRolAsync()
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT
                        FK_Rol as RolId,
                        COUNT(DISTINCT FK_Usuario) as TotalUsuarios
                    FROM UsuarioRol
                    GROUP BY FK_Rol;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                var resultado = await conexion.QueryAsync<(int RolId, int TotalUsuarios)>(consulta);
                return resultado.ToDictionary(r => r.RolId, r => r.TotalUsuarios);
            }
        }

        /// <summary>
        /// Obtiene un rol por su ID
        /// </summary>
        public async Task<Modelos.Entidades.Rol> ObtenerRolPorIdAsync(int id)
        {
            using (MySqlConnection conexion = ObtenerConexionLectura())
            {
                const string consulta = @"
                    SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

                    SELECT Id, Nombre, Descripcion
                    FROM Rol
                    WHERE Id = @Id;

                    SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
                ";

                return await conexion.QuerySingleOrDefaultAsync<Modelos.Entidades.Rol>(consulta, new { Id = id });
            }
        }

        /// <summary>
        /// Actualiza solo la descripción de un rol (el nombre no se puede modificar)
        /// </summary>
        public async Task ActualizarDescripcionRolAsync(int id, string descripcion)
        {
            await EjecutarConTransaccionAsync(async (conexion, transaccion) =>
            {
                const string consulta = @"
                    UPDATE Rol
                    SET Descripcion = @Descripcion
                    WHERE Id = @Id;
                ";

                await conexion.ExecuteAsync(consulta, new { Id = id, Descripcion = descripcion }, transaccion);
            });
        }
    }
}
