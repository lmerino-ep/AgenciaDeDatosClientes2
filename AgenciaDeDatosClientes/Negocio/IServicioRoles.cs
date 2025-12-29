using Modelos.DTO.Roles;
using Modelos.Entidades;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de roles
    /// </summary>
    public interface IServicioRoles
    {
        Task<List<Modelos.Entidades.Rol>> ObtenerTodosLosRolesAsync();
        Task<List<Modelos.Entidades.Rol>> ObtenerRolesUsuarioAsync(int idUsuario);
        Task<List<Modelos.Entidades.Rol>> ObtenerRolesDisponiblesAsync(int idUsuario);
        Task<UsuarioRoles> ObtenerUsuarioConRolesAsync(int idUsuario);
        Task AsignarRolesAsync(int idUsuario, List<int> idsRoles);
    }
}

