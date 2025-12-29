using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelos.Autenticacion
{
    public class Usuario
    {
        public int Id { get; set; }
        public int FK_UsuarioTipo { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Foto { get; set; }
        public string Password { get; set; }
        public string Empresa { get; set; }
        public DateTime? FechaDeNacimiento { get; set; }
        //////////////////////////////////////
        public string GmailId { get; set; }
        //////////////////////////////////////
        public bool AccesoAPI { get; set; }
        public string RefreshToken1EpDataApi { get; set; }
        public string RefreshToken2EpDataApi { get; set; }
        public string AccessTokenActivoEpDataApi { get; set; }
        public DateTime? CaducidadAccessTokenActivoEpDataApi { get; set; }
        //////////////////////////////////////
        public bool Activo { get; set; }
        //////////////////////////////////////
        public virtual List<AgenciaDeDatosBD.Rol> Roles { get; set; } = new List<AgenciaDeDatosBD.Rol>();
    }
}
