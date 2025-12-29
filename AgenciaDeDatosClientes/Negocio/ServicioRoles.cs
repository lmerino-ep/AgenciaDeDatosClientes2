using Datos;
using Modelos.Entidades;
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
        public async Task<List<Modelos.Entidades.Rol>> ObtenerTodosLosRolesAsync()
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los roles del sistema");
                return await _repositorio.ObtenerTodosLosRolesAsync();
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
        public async Task<List<Modelos.Entidades.Rol>> ObtenerRolesUsuarioAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles del usuario {UsuarioId}", idUsuario);
                return await _repositorio.ObtenerRolesUsuarioAsync(idUsuario);
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
        public async Task<List<Modelos.Entidades.Rol>> ObtenerRolesDisponiblesAsync(int idUsuario)
        {
            try
            {
                _logger.LogInformation("Obteniendo roles disponibles para usuario {UsuarioId}", idUsuario);
                return await _repositorio.ObtenerRolesDisponiblesAsync(idUsuario);
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
                    List<Modelos.Entidades.Rol> todosLosRoles = await _repositorio.ObtenerTodosLosRolesAsync();
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
    }
}
