using Microsoft.AspNetCore.Mvc;

namespace ViewComponents
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
