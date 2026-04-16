using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Pages.Suscripcion
{
    [Authorize]
    public class GraciasModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
