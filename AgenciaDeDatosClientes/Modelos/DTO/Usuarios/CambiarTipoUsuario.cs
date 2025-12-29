using System.ComponentModel.DataAnnotations;

namespace Modelos.DTO.Usuarios
{
    /// <summary>
    /// DTO para cambiar el tipo de usuario
    /// </summary>
    public class CambiarTipoUsuario
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El tipo de usuario es obligatorio")]
        public int FK_UsuarioTipo { get; set; }
    }
}

