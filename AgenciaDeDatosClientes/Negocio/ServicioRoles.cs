using Datos;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Roles;

namespace Negocio
{
    /// <summary>
    /// Servicio para gestión de roles de usuario
    /// </summary>
    public class ServicioRoles : IServicioRoles
    {
        private readonly RepositorioRoles _repositorio;
        private readonly ILogger<ServicioRoles> _logger;

        public ServicioRoles(
            RepositorioRoles repositorio,
            ILogger<ServicioRoles> logger)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los roles del sistema
        /// </summary>
        public async Task<List<Rol>> ObtenerTodosLosRolesAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los roles del sistema");
                var entidades = await _repositorio.ObtenerTodosLosRolesAsync();
                // Mapear de entidad a DTO
                return entidades.Select(e => new Rol
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Asignado = false // No aplica en este contexto
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los roles");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los roles asignados a un usuario
        /// </summary>
        public async Task<List<Rol>> ObtenerRolesUsuarioAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles del usuario {UsuarioId}", idUsuario);
                var entidades = await _repositorio.ObtenerRolesUsuarioAsync(idUsuario);
                // Mapear de entidad a DTO
                return entidades.Select(e => new Rol
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Asignado = true
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles del usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene los roles NO asignados a un usuario
        /// </summary>
        public async Task<List<Rol>> ObtenerRolesDisponiblesAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles disponibles para usuario {UsuarioId}", idUsuario);
                var entidades = await _repositorio.ObtenerRolesDisponiblesAsync(idUsuario);
                // Mapear de entidad a DTO
                return entidades.Select(e => new Rol
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Asignado = false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles disponibles para usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Obtiene la información de usuario con sus roles
        /// </summary>
        public async Task<UsuarioRoles> ObtenerUsuarioConRolesAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo información completa de roles para usuario {UsuarioId}", idUsuario);
                UsuarioRoles resultado = await _repositorio.ObtenerUsuarioConRolesAsync(idUsuario);

                if (resultado == null)
                {
                    _logger.LogWarning("Usuario {UsuarioId} no encontrado", idUsuario);
                    throw new InvalidOperationException($"Usuario {idUsuario} no encontrado");
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario con roles {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Asigna roles a un usuario (reemplaza todos los roles existentes)
        /// </summary>
        public async Task AsignarRolesAsync(int idUsuario, List<int> idsRoles)
        {
            try
            {
                _logger.LogInformation("Asignando {CantidadRoles} roles al usuario {UsuarioId}", idsRoles?.Count ?? 0, idUsuario);

                // Validar que los roles existen
                if (idsRoles != null && idsRoles.Any())
                {
                    var todosLosRoles = await ObtenerTodosLosRolesAsync();
                    List<int> rolesInvalidos = idsRoles.Where(id => !todosLosRoles.Any(r => r.Id == id)).ToList();

                    if (rolesInvalidos.Any())
                    {
                        string rolesInvalidosStr = string.Join(", ", rolesInvalidos);
                        _logger.LogWarning("Intento de asignar roles inválidos: {RolesInvalidos}", rolesInvalidosStr);
                        throw new InvalidOperationException($"Los siguientes roles no existen: {rolesInvalidosStr}");
                    }
                }

                await _repositorio.AsignarRolesAsync(idUsuario, idsRoles);

                _logger.LogInformation("Roles asignados exitosamente al usuario {UsuarioId}", idUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar roles al usuario {UsuarioId}", idUsuario);
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo rol en el sistema
        /// </summary>
        public async Task<int> CrearRolAsync(string nombre, string descripcion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    throw new ArgumentException("El nombre del rol es requerido", nameof(nombre));
                }

                // Verificar que no existe un rol con el mismo nombre
                bool existe = await _repositorio.ExisteRolConNombreAsync(nombre);
                if (existe)
                {
                    _logger.LogWarning("Intento de crear rol con nombre duplicado: {Nombre}", nombre);
                    throw new InvalidOperationException($"Ya existe un rol con el nombre '{nombre}'");
                }

                _logger.LogInformation("Creando nuevo rol: {Nombre}", nombre);
                int idRol = await _repositorio.CrearRolAsync(nombre, descripcion ?? string.Empty);
                
                _logger.LogInformation("Rol creado exitosamente con ID: {RolId}", idRol);
                return idRol;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol: {Nombre}", nombre);
                throw;
            }
        }

        /// <summary>
        /// Verifica si existe un rol con el mismo nombre
        /// </summary>
        public async Task<bool> ExisteRolConNombreAsync(string nombre)
        {
            try
            {
                return await _repositorio.ExisteRolConNombreAsync(nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de rol: {Nombre}", nombre);
                throw;
            }
        }

        /// <summary>
        /// Obtiene el conteo de usuarios por rol
        /// </summary>
        public async Task<Dictionary<int, int>> ObtenerConteoUsuariosPorRolAsync()
        {
            try
            {
                return await _repositorio.ObtenerConteoUsuariosPorRolAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conteo de usuarios por rol");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un rol por su ID
        /// </summary>
        public async Task<Modelos.DTO.Roles.Rol> ObtenerRolPorIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Obteniendo rol con ID: {RolId}", id);
                var entidad = await _repositorio.ObtenerRolPorIdAsync(id);
                
                if (entidad == null)
                {
                    _logger.LogWarning("Rol {RolId} no encontrado", id);
                    return null;
                }

                return new Modelos.DTO.Roles.Rol
                {
                    Id = entidad.Id,
                    Nombre = entidad.Nombre,
                    Descripcion = entidad.Descripcion,
                    Asignado = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol {RolId}", id);
                throw;
            }
        }

        /// <summary>
        /// Actualiza solo la descripción de un rol (el nombre no se puede modificar)
        /// </summary>
        public async Task ActualizarDescripcionRolAsync(int id, string descripcion)
        {
            try
            {
                // Verificar que el rol existe
                var rolExistente = await _repositorio.ObtenerRolPorIdAsync(id);
                if (rolExistente == null)
                {
                    _logger.LogWarning("Intento de actualizar rol inexistente: {RolId}", id);
                    throw new InvalidOperationException($"El rol con ID {id} no existe");
                }

                _logger.LogInformation("Actualizando descripción del rol {RolId}: {Nombre}", id, rolExistente.Nombre);
                await _repositorio.ActualizarDescripcionRolAsync(id, descripcion ?? string.Empty);
                
                _logger.LogInformation("Descripción del rol actualizada exitosamente: {RolId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar descripción del rol {RolId}", id);
                throw;
            }
        }
    }
}
