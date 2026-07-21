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

        // Los claims de rol ya los añade la clase base vía UserManager.GetRolesAsync; no se repiten aquí.
        public async override Task<ClaimsPrincipal> CreateAsync(Usuario user)
        {
            var principal = await base.CreateAsync(user);

            if (user.FK_UsuarioTipo != null && principal.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim("FK_UsuarioTipo", user.FK_UsuarioTipo.ToString()));
            }

            return principal;
        }
    }
}
