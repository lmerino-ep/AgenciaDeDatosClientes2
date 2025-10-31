using Microsoft.AspNetCore.Mvc;

namespace Dashboard.ViewComponents
{
    public class LayoutFooter : ViewComponent
    {
        public LayoutFooter()
        {
            
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View();
        }
    }
}
