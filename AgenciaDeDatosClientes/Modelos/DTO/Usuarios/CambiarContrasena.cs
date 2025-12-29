using System.ComponentModel.DataAnnotations;

namespace Modelos.DTO.Usuarios
{
    /// <summary>
    /// DTO para cambiar la contraseña de un usuario
    /// </summary>
    public class CambiarContrasena
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string NuevaPassword { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }

        public bool EnviarPorEmail { get; set; }
    }
}

