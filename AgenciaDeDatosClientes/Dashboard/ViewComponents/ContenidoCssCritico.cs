using Dashboard.Utils.InyeccionDeDependencias;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.ViewComponents
{
    public class ContenidoCssCritico : ViewComponent
    {
        private readonly CriticalCss _criticalCss;

        public enum Archivo
        {
            Cabecera = 1,
            Home = 2
        }

        public ContenidoCssCritico(CriticalCss criticalCss)
        {
            _criticalCss = criticalCss;
        }

        public async Task<IViewComponentResult> InvokeAsync(List<Archivo> archivos)
        {
            return View("Default", archivos.Select(archivo => _criticalCss.ContenidoArchivoCssCriticoPorNombre[archivo]).ToList());
        }
    }
}
