using Microsoft.AspNetCore.Mvc;

namespace ViewComponents
{
    public class LayoutMenu : ViewComponent
    {
        public LayoutMenu()
        {
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            LayoutMenuModel modelo = new LayoutMenuModel();
            return View(modelo);
        }
    }
}
