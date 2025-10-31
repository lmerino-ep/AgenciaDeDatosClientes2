using Microsoft.AspNetCore.Mvc;

namespace Dashboard.ViewComponents
{
    public class LayoutHead : ViewComponent
    {
        public LayoutHead()
        {

        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var request = HttpContext.Request;
            ViewBag.CannonicalUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";

            return View();
        }
    }
}
