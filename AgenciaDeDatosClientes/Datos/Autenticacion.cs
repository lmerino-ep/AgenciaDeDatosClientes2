using AgenciaDeDatosBD;
using AgenciaDeDatosBD.Herramientas;
using Dapper;
using Modelos.Autenticacion;
using Modelos.InyeccionDeDependencias;

namespace Datos
{
    public class Autenticacion
    {
        private readonly Aurora _aurora;

        public Autenticacion(Aurora aurora)
        {
            _aurora = aurora;
        }

        public async Task<AgenciaDeDatosBD.Herramientas.Retorno> CrearRolAsync(int fk_Usuario, int fk_Rol)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno;
            using (var bd = new AgenciaDeDatosBD.BD(_aurora.Lectura, _aurora.Escritura))
            {
                retorno = await bd.UsuarioRol().Crear(fk_Usuario, fk_Rol);
            }
            return retorno;
        }

        public async Task<AgenciaDeDatosBD.Herramientas.Retorno> CrearUsuarioAsync(Modelos.Autenticacion.Usuario usuario)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno;

            using (var bd = new AgenciaDeDatosBD.BD(_aurora.Lectura, _aurora.Escritura))
            {
                retorno = await bd.Usuario().Crear(
                    usuario.FK_UsuarioTipo,
                    usuario.Email,
                    usuario.Nombre,
                    usuario.Apellidos,
                    usuario.Foto,
                    usuario.Empresa,
                    usuario.Password,
                    usuario.FechaDeNacimiento,
                    usuario.GmailId
                );
            }

            return retorno;
        }

        public async Task<Retorno> EliminarRolAsync(int fk_Usuario, int fk_Rol)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno;
            using (var bd = new AgenciaDeDatosBD.BD(_aurora.Lectura, _aurora.Escritura))
            {
                retorno = await bd.UsuarioRol().Eliminar(fk_Usuario, fk_Rol);
            }
            return retorno;
        }

        public async Task<List<Rol>> GetRolesAsync()
        {
            const string sql = @"
                SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                select 
	                R.* 
                from Rol R
                ;
                SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;                
            ";

            List<Rol> roles;
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                roles = (await bd.ConexionLectura().QueryAsync<Rol>(sql)).ToList();
            }

            return roles;
        }

        public async Task<List<AgenciaDeDatosBD.Rol>> GetRolesByUserAsync(int fk_Usuario)
        {
            const string sql = @"
                SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                select 
	                R.* 
                from Rol R
                inner join UsuarioRol UR
                    on R.Id = UR.FK_Rol
                    and UR.FK_Usuario = ?FK_Usuario
				union
				select 
	                R.* 
                from Rol R
                inner join Usuario U
                    on U.Id = ?FK_Usuario
				inner join UsuarioTipoRol UTR
					on UTR.FK_UsuarioTipo = U.FK_UsuarioTipo
                    and UTR.FK_Rol = R.Id
                ;
                SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
            ";

            List<AgenciaDeDatosBD.Rol> roles;
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                roles = (await bd.ConexionLectura().QueryAsync<AgenciaDeDatosBD.Rol>(sql, new
                {
                    FK_Usuario = fk_Usuario
                })).ToList();
            }

            return roles;
        }

        public async Task<List<AgenciaDeDatosBD.Rol>> GetRolesDeUsuarioAsync(int fk_Usuario)
        {
            const string sql = @"
                SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                select 
	                R.* 
                from UsuarioRol UR
                inner join Rol R
	                on UR.FK_Rol = R.Id
                    and UR.FK_Usuario = ?FK_Usuario
                ;
                SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
            ";
            List<Rol> roles;
            using (var oBd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                roles = (await oBd.ConexionLectura().QueryAsync<Rol>(sql, new { FK_Usuario = fk_Usuario })).ToList();
            }
            return roles;
        }

        public async Task<Modelos.Autenticacion.Usuario> GetUsuarioAsync(int fk_Usuario, bool soloActivos)
        {
            string sql = @$"
                SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                select
	                U.*
                from Usuario U 
                where
	                U.Id = ?Id
                    {(soloActivos ? "and U.Activo = true" : String.Empty)}
                ;
                select
	                R.*
                from Rol R
                inner join UsuarioRol UR 
	                on R.Id = UR.FK_Rol
                where
	                UR.FK_Usuario = ?Id
                ;
                SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;                
            ";

            Modelos.Autenticacion.Usuario usuario;
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                using (var multi = bd.ConexionLectura().QueryMultiple(sql, new { Id = fk_Usuario }))
                {
                    usuario = await multi.ReadFirstOrDefaultAsync<Modelos.Autenticacion.Usuario>();
                    var roles = (await multi.ReadAsync<Rol>()).ToList();
                    if (usuario != null)
                    {
                        usuario.Roles = roles;
                    }
                }
            }
            return usuario;
        }

        public async Task<Modelos.Autenticacion.Usuario> GetUsuarioPorEmailAsync(string email, bool soloActivos)
        {
            string sql = @$"
                SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
                select
	                U.*
                from Usuario U 
                where
	                UPPER(U.Email) = ?Email
                    {(soloActivos ? "and U.Activo = true" : String.Empty)}
                ;
                select
	                R.*
                from Rol R
                inner join UsuarioRol UR 
	                on R.Id = UR.FK_Rol
                inner join Usuario U
                    on UR.FK_Usuario = U.Id
                where
	                UPPER(U.Email) = ?Email
                ;
                SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;                
            ";

            Modelos.Autenticacion.Usuario usuario;
            //Ojo: no utlizamos conexión de lectura porque este método  lo utiliza el _signInManager, y podemos llegar aquí justo después de restablecer password y un pelín antes de que la réplica de lectura reciba el cambio.
            using (var bd = new BD(_aurora.Lectura, _aurora.Escritura))
            {
                using (var multi = bd.ConexionLectura().QueryMultiple(sql, new { Email = email }))
                {
                    usuario = await multi.ReadFirstOrDefaultAsync<Modelos.Autenticacion.Usuario>();
                    var roles = (await multi.ReadAsync<Rol>()).ToList();
                    if (usuario != null)
                    {
                        usuario.Roles = roles;
                    }
                }
            }
            return usuario;
        }

        public async Task<AgenciaDeDatosBD.Herramientas.Retorno> GuardarInformacionDeUsuarioDeGmailAsync(string email, string gmail_UrlFoto, string gmail_Id)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno;
            using (var bd = new AgenciaDeDatosBD.BD(_aurora.Lectura, _aurora.Escritura))
            {
                retorno = await bd.Usuario().GuardarInformacionDeUsuarioDeGmail(email, gmail_UrlFoto, gmail_Id);
            }
            return retorno;
        }

        public async Task<AgenciaDeDatosBD.Herramientas.Retorno> ModificarUsuarioAsync(Modelos.Autenticacion.Usuario usuario)
        {
            AgenciaDeDatosBD.Herramientas.Retorno retorno;
            using (var bd = new AgenciaDeDatosBD.BD(_aurora.Lectura, _aurora.Escritura))
            {
                retorno = await bd.Usuario().Modificar(
                    usuario.Id,
                    usuario.Email,
                    usuario.Nombre,
                    usuario.Apellidos,
                    usuario.Empresa,
                    usuario.Password,
                    usuario.FechaDeNacimiento,
                    usuario.Activo
                );
            }
            return retorno;
        }
    }
}
