using Modelos.DTO.Usuarios;
using Modelos.Entidades;

namespace Negocio
{
    /// <summary>
    /// Interfaz para el servicio de gestión de usuarios
    /// </summary>
    public interface IServicioUsuarios
    {
        Task<(List<UsuarioListado> usuarios, int total)> ListarUsuariosAsync(
            int? fkUsuarioTipo = null,
            bool? activo = null,
            string busqueda = null,
            int pagina = 0,
            int tamanoPagina = 25);

        Task<UsuarioDetalle> ObtenerUsuarioAsync(int idUsuario);
        Task<int> CrearUsuarioAsync(CrearUsuario dto);
        Task ActualizarUsuarioAsync(EditarUsuario dto);
        Task CambiarContrasenaAsync(CambiarContrasena dto);
        Task CambiarTipoUsuarioAsync(CambiarTipoUsuario dto);
        Task EliminarUsuarioAsync(int idUsuario);
        Task<List<UsuarioTipo>> ObtenerTiposUsuarioAsync();
        Task<bool> ExisteEmailAsync(string email, int? idUsuarioExcluir = null);
    }
}

