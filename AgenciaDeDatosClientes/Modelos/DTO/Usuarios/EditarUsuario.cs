using System.ComponentModel.DataAnnotations;

namespace Modelos.DTO.Usuarios
{
    /// <summary>
    /// DTO para editar un usuario existente
    /// </summary>
    public class EditarUsuario
    {
        public int Id { get; set; }

        public string Email { get; set; } // Solo lectura

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        public string Apellidos { get; set; }

        public string Empresa { get; set; }

        [Required(ErrorMessage = "El tipo de usuario es obligatorio")]
        public int FK_UsuarioTipo { get; set; }

        public bool Activo { get; set; }
    }
}

