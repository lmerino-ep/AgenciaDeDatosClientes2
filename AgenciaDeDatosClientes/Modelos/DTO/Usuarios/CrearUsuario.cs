using System.ComponentModel.DataAnnotations;

namespace Modelos.DTO.Usuarios
{
    /// <summary>
    /// DTO para crear un nuevo usuario
    /// </summary>
    public class CrearUsuario
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        public string Apellidos { get; set; }

        public string Empresa { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; }

        [Required(ErrorMessage = "El tipo de usuario es obligatorio")]
        public int FK_UsuarioTipo { get; set; }

        public bool Activo { get; set; } = true;
    }
}

