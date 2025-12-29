using AgenciaDeDatosBD;
using Microsoft.AspNetCore.Identity;
using Modelos.Autenticacion;
using Modelos.InyeccionDeDependencias;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Utils.Autenticacion
{
    public class UserStore : IUserSecurityStampStore<Modelos.Autenticacion.Usuario>, // implementamos IUserSecurityStampStore para evitar que la sesi�n expire cada 30 minutos
        IUserPasswordStore<Modelos.Autenticacion.Usuario>,
        IPasswordHasher<Modelos.Autenticacion.Usuario>,
        IUserRoleStore<Modelos.Autenticacion.Usuario>,
        IUserLoginStore<Modelos.Autenticacion.Usuario>
    {
        private readonly Negocio.Autenticacion _negocioAutenticacion;

        public UserStore(Aurora aurora)
        {
            _negocioAutenticacion = new Negocio.Autenticacion(aurora);
        }
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string> GetUserNameAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task SetUserNameAsync(Modelos.Autenticacion.Usuario user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task SetNormalizedUserNameAsync(Modelos.Autenticacion.Usuario user, string normalizedName, CancellationToken cancellationToken)
        {
            // no seteamos nada
            return Task.FromResult(true);
        }

        public async Task<IdentityResult> CreateAsync(Modelos.Autenticacion.Usuario usuario, CancellationToken cancellationToken)
        {
            try
            {
                usuario.Id = await _negocioAutenticacion.CrearUsuarioAsync(usuario);                
                foreach (var rol in usuario.Roles)
                {
                    await AddToRoleAsync(usuario, rol.Nombre, cancellationToken);
                }
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError() { Description = ex.Message });
            }
        }

        public async Task<IdentityResult> UpdateAsync(Modelos.Autenticacion.Usuario usuario, CancellationToken cancellationToken)
        {
            try
            {
                await _negocioAutenticacion.ModificarUsuarioAsync(usuario);
                List<Rol> rolesActuales = await _negocioAutenticacion.GetRolesDeUsuarioAsync(usuario.Id);

                foreach (var rolActual in rolesActuales)
                {
                    if (usuario.Roles.FirstOrDefault(r => r.Id == rolActual.Id) == null)
                    {
                        await RemoveFromRoleAsync(usuario, rolActual.Nombre, cancellationToken);
                    }
                }
                foreach (var rol in usuario.Roles)
                {
                    if (rolesActuales.FirstOrDefault(r => r.Id == rol.Id) == null)
                    {
                        await AddToRoleAsync(usuario, rol.Nombre, cancellationToken);
                    }
                }

                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                return IdentityResult.Failed();
            }
        }

        public Task<IdentityResult> DeleteAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Modelos.Autenticacion.Usuario> FindByIdAsync(string fk_Usuario, CancellationToken cancellationToken)
        {
            Modelos.Autenticacion.Usuario usuario = await _negocioAutenticacion.GetUsuarioAsync(Convert.ToInt32(fk_Usuario), true);
            if (usuario == null || !usuario.Activo)
            {
                return null;
            }
            else
            {
                return usuario;
            }
        }

        public async Task<Modelos.Autenticacion.Usuario> FindByNameAsync(string nombreUsuario, CancellationToken cancellationToken)
        {
            Modelos.Autenticacion.Usuario usuario = await _negocioAutenticacion.GetUsuarioPorEmailAsync(nombreUsuario, true);
            if (usuario == null || !usuario.Activo)
            {
                return null;
            }
            else
            {
                return usuario;
            }
        }

        public Task SetPasswordHashAsync(Modelos.Autenticacion.Usuario user, string passwordHash, CancellationToken cancellationToken)
        {
            user.Password = passwordHash;
            return Task.FromResult(true);
        }

        public Task<string> GetPasswordHashAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Password);
        }

        public Task<bool> HasPasswordAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public string HashPassword(Modelos.Autenticacion.Usuario user, string password)
        {
            return Hash.HashText(password);
        }

        public PasswordVerificationResult VerifyHashedPassword(Modelos.Autenticacion.Usuario user, string hashedPassword, string providedPassword)
        {
            string providedPwdHashed = Utils.Hash.HashText(providedPassword);
            if (hashedPassword == providedPwdHashed)
            {
                return PasswordVerificationResult.Success;
            }

            return PasswordVerificationResult.Failed;
        }

        public Task SetSecurityStampAsync(Modelos.Autenticacion.Usuario user, string stamp, CancellationToken cancellationToken)
        {
            return Task.FromResult(stamp);
        }

        public Task<string> GetSecurityStampAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Guid().ToString());
        }

        public async Task AddToRoleAsync(Modelos.Autenticacion.Usuario user, string roleName, CancellationToken cancellationToken)
        {
            List<AgenciaDeDatosBD.Rol> roles = await _negocioAutenticacion.GetRolesAsync();
            AgenciaDeDatosBD.Rol rol = roles.FirstOrDefault(x => x.Nombre == roleName);
            if (rol != null)
            {
                await _negocioAutenticacion.CrearRolAsync(user.Id, rol.Id);
            }
        }

        public async Task RemoveFromRoleAsync(Modelos.Autenticacion.Usuario user, string roleName, CancellationToken cancellationToken)
        {
            List<AgenciaDeDatosBD.Rol> roles = await _negocioAutenticacion.GetRolesAsync();
            AgenciaDeDatosBD.Rol rol = roles.FirstOrDefault(x => x.Nombre == roleName);
            if (rol != null)
            {
                await _negocioAutenticacion.EliminarRolAsync(user.Id, rol.Id);
            }
        }

        public async Task<IList<string>> GetRolesAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            List<AgenciaDeDatosBD.Rol> roles = await _negocioAutenticacion.GetRolesByUserAsync(user.Id);
            return roles.Select(x => x.Nombre).ToList();
        }

        public async Task<bool> IsInRoleAsync(Modelos.Autenticacion.Usuario user, string roleName, CancellationToken cancellationToken)
        {
            return user.Roles.Any(x => x.Nombre == roleName);
        }

        public Task<IList<Modelos.Autenticacion.Usuario>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #region IUserLoginStore
        public Task RemoveLoginAsync(Modelos.Autenticacion.Usuario user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(Modelos.Autenticacion.Usuario user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Modelos.Autenticacion.Usuario> FindByLoginAsync(string proveedor, string accessToken, CancellationToken cancellationToken)
        {
            //Aqu� llegamos mediante el _signInManager.ExternalLoginSignInAsync
            switch (proveedor)
            {
                case "Google":
                    //Obtenemos los datos del usuario.ini
                    var httpClient = new HttpClient { BaseAddress = new Uri("https://www.googleapis.com") };
                    var respuesta = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token={accessToken}");
                    String json = await respuesta.Content.ReadAsStringAsync();
                    UsuarioGoogle usuario = JsonSerializer.Deserialize<UsuarioGoogle>(json);
                    //Obtenemos los datos del usuario.fin
                    Modelos.Autenticacion.Usuario usuarioRegistrado = await _negocioAutenticacion.GetUsuarioPorEmailAsync(usuario.Email, true);

                    if (usuarioRegistrado == null)
                    {
                        //Registramos:
                        usuarioRegistrado = new Modelos.Autenticacion.Usuario
                        {
                            FK_UsuarioTipo = 1,
                            Email = usuario.Email,
                            Nombre = usuario.Gmail_NombreDado,
                            Apellidos = usuario.Gmail_NombreFamilia,
                            Foto = usuario.Gmail_UrlFoto,
                            Empresa = usuario.Gmail_Empresa,
                            Password = Hash.HashText(usuario.Gmail_Id.ToString()),
                            FechaDeNacimiento = usuario.FechaDeNacimiento,
                            GmailId = usuario.Gmail_Id,
                            Activo = true
                        };

                        usuarioRegistrado.Id = await _negocioAutenticacion.CrearUsuarioAsync(usuarioRegistrado);
                    }
                    else
                    {
                        if (usuarioRegistrado.Foto == null || usuarioRegistrado.GmailId == null)
                        {
                            await _negocioAutenticacion.GuardarInformacionDeUsuarioDeGmailAsync(usuario.Email, usuario.Gmail_UrlFoto, usuario.Gmail_Id);

                            usuarioRegistrado.Foto = usuario.Gmail_UrlFoto;
                            usuarioRegistrado.GmailId = usuario.Gmail_Id;
                        }
                    }
                    return usuarioRegistrado;
                default:
                    throw new NotImplementedException();
            }
        }

        public Task AddLoginAsync(Modelos.Autenticacion.Usuario user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
