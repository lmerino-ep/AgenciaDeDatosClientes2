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

        private const string DateTimeFormatString = "yyyy-MM-ddTHH:mm:sszzz";
        public static string SerializaFecha(DateTime? fecha)
        {
            return fecha == null ? string.Empty : ((DateTime)fecha).ToString(DateTimeFormatString);
        }
        public static DateTime? DeserializaFecha(string fecha)
        {
            return string.IsNullOrEmpty(fecha) ? null : DateTime.Parse(fecha);
        }

        // This method is called only when login. It means that "the drawback   
        // of calling the database with each HTTP request" never happen.  
        public async override Task<ClaimsPrincipal> CreateAsync(Usuario user)
        {
            var principal = await base.CreateAsync(user);

            if (user.FK_UsuarioTipo != null)
            {
                if (principal.Identity != null)
                {
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("FK_UsuarioTipo", user.FK_UsuarioTipo.ToString())
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Nombre", user.Nombre ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Apellidos", user.Apellidos ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Email", user.Email ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Empresa", user.Empresa ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("GmailId", user.GmailId ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Password", user.Password ?? String.Empty)
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("FechaDeNacimiento", SerializaFecha(user.FechaDeNacimiento))
                    });
                    ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                        new Claim("Foto", user.Foto ?? String.Empty)
                    });
                    foreach (var rol in user.Roles)
                    {
                        //https://benfoster.io/blog/asp-net-identity-role-claims/
                        ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                            new Claim(Options.ClaimsIdentity.RoleClaimType, rol.Nombre)
                        });
                    }
                }
            }

            return principal;
        }
    }
}
