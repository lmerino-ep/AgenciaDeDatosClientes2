using AgenciaDeDatosBD;
using Modelos.Autenticacion;
using Modelos.InyeccionDeDependencias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Negocio
{
    public class Autenticacion
    {
        private readonly Datos.Autenticacion _datosAutenticacion;

        public Autenticacion(Aurora aurora)
        {
            _datosAutenticacion = new Datos.Autenticacion(aurora);
        }

        public async Task CrearRolAsync(int fk_Usuario, int fk_Rol)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno = await _datosAutenticacion.CrearRolAsync(fk_Usuario, fk_Rol);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task<int> CrearUsuarioAsync(Modelos.Autenticacion.Usuario usuario)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno = await _datosAutenticacion.CrearUsuarioAsync(usuario);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
            return retorno.Resultado<AgenciaDeDatosBD.Usuario>().Single().Id;
        }

        public async Task EliminarRolAsync(int fk_Usuario, int fk_Rol)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno = await _datosAutenticacion.EliminarRolAsync(fk_Usuario, fk_Rol);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task<List<Rol>> GetRolesAsync()
        {
            return await _datosAutenticacion.GetRolesAsync();
        }

        public async Task<List<AgenciaDeDatosBD.Rol>> GetRolesByUserAsync(int fk_Usuario)
        {
            return await _datosAutenticacion.GetRolesByUserAsync(fk_Usuario);
        }

        public async Task<List<Rol>> GetRolesDeUsuarioAsync(int fk_Usuario)
        {
            return await _datosAutenticacion.GetRolesDeUsuarioAsync(fk_Usuario);
        }

        public async Task<Modelos.Autenticacion.Usuario> GetUsuarioAsync(int fk_Usuario, bool soloActivos)
        {
            return await _datosAutenticacion.GetUsuarioAsync(fk_Usuario, soloActivos);
        }

        public async Task<Modelos.Autenticacion.Usuario> GetUsuarioPorEmailAsync(string email, bool soloActivos)
        {
            return await _datosAutenticacion.GetUsuarioPorEmailAsync(email, soloActivos);
        }

        public async Task GuardarInformacionDeUsuarioDeGmailAsync(string email, string gmail_UrlFoto, string gmail_Id)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno = await _datosAutenticacion.GuardarInformacionDeUsuarioDeGmailAsync(email, gmail_UrlFoto, gmail_Id);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }

        public async Task ModificarUsuarioAsync(Modelos.Autenticacion.Usuario usuario)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno = await _datosAutenticacion.ModificarUsuarioAsync(usuario);
            if (!retorno.Correcto)
            {
                throw new Exception(retorno.Mensaje);
            }
        }
    }
}
