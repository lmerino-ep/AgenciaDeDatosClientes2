using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Modelos.Autenticacion;

namespace Utils.Autenticacion
{
    public class ClaimsPersonalizados : UserClaimsPrincipalFactory<Usuario>
    {
        public ClaimsPersonalizados(UserManager<Usuario> userManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, optionsAccessor)
        {
        }

        public async override Task<ClaimsPrincipal> CreateAsync(Usuario user)
        {
            var principal = await base.CreateAsync(user);

            if (user.FK_UsuarioTipo != null && principal.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim("FK_UsuarioTipo", user.FK_UsuarioTipo.ToString()));

                foreach (var rol in user.Roles)
                {
                    identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, rol.Nombre));
                }
            }

            return principal;
        }
    }
}
