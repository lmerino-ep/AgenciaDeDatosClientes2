using Modelos.DTO.Roles;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de roles
    /// </summary>
    public interface IServicioRoles
    {
        Task<List<Rol>> ObtenerTodosLosRolesAsync();
        Task<List<Rol>> ObtenerRolesUsuarioAsync(int idUsuario);
        Task<List<Rol>> ObtenerRolesDisponiblesAsync(int idUsuario);
        Task<UsuarioRoles> ObtenerUsuarioConRolesAsync(int idUsuario);
        Task AsignarRolesAsync(int idUsuario, List<int> idsRoles);
        
        // Gestión de roles
        Task<int> CrearRolAsync(string nombre, string descripcion);
        Task<bool> ExisteRolConNombreAsync(string nombre);
        Task<Dictionary<int, int>> ObtenerConteoUsuariosPorRolAsync();
        Task<Modelos.DTO.Roles.Rol> ObtenerRolPorIdAsync(int id);
        Task ActualizarDescripcionRolAsync(int id, string descripcion);
    }
}

