using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Modelos.Autenticacion
{
    public class UsuarioGoogle
    {
        [JsonPropertyName("fk_usuario")]
        public int FK_Usuario { get; set; }
        [JsonPropertyName("fk_usuariotipo")]
        public int FK_UsuarioTipo { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }
        [JsonPropertyName("apellidos")]
        public string Apellidos { get; set; }
        [JsonPropertyName("empresa")]
        public string Empresa { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        [JsonPropertyName("fechadenacimiento")]
        public DateTime? FechaDeNacimiento { get; set; }
        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        //@@@@@@@@@@@@@

        [JsonPropertyName("token")]
        public string Token { get; set; }

        //@@@@@@@@@@@@@

        [JsonPropertyName("id")]
        public string Gmail_Id { get; set; }

        [JsonPropertyName("verified_email")]
        public bool Gmail_EmailVerificado { get; set; }

        [JsonPropertyName("name")]
        public string Gmail_Nombre { get; set; }

        [JsonPropertyName("given_name")]
        public string Gmail_NombreDado { get; set; }

        [JsonPropertyName("family_name")]
        public string Gmail_NombreFamilia { get; set; }

        [JsonPropertyName("link")]
        public string Gmail_Enlace { get; set; }

        [JsonPropertyName("picture")]
        public string Gmail_UrlFoto { get; set; }

        [JsonPropertyName("gender")]
        public string Gmail_Sexo { get; set; }

        [JsonPropertyName("locale")]
        public string Gmail_Local { get; set; }

        [JsonPropertyName("hd")]
        public string Gmail_Empresa { get; set; }
    }
}
