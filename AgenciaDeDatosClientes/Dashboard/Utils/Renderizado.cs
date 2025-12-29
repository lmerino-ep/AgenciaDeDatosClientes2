using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Utils
{
    public static class Renderizado
    {
        public static async Task<string> GenerarHtml<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }

        public static int GetTotalPaginas(int iTotalElementos, int iTamPagina)
        {
            int iTotalPaginas;
            if (iTotalElementos == 0)
            {
                iTotalPaginas = 1;
            }
            else
            {
                int iResto;
                int iCoeficiente = Math.DivRem(iTotalElementos, iTamPagina, out iResto);
                iTotalPaginas = iResto == 0 ? iCoeficiente : iCoeficiente + 1;
            }
            return iTotalPaginas;
        }
    }
}
