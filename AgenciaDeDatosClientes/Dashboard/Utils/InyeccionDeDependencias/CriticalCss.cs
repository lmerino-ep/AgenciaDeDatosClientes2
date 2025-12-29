using ViewComponents;

namespace Utils.InyeccionDeDependencias
{
    public class CriticalCss
    {
        public Dictionary<ContenidoCssCritico.Archivo, string> ContenidoArchivoCssCriticoPorNombre { get; set; }

        public CriticalCss(string sRutaWwwroot)
        {
            //TODO
            /*
            ContenidoArchivoCssCriticoPorNombre = new Dictionary<ContenidoCssCritico.Archivo, string>();

            // Lista de rutas posibles a probar
            string[] rutasPosibles = {
                Path.Combine(sRutaWwwroot, "css/critical"),
                Path.Combine(sRutaWwwroot.Replace("wwwroot", "epdata/wwwroot"), "css/critical"),
                Path.Combine("/app/wwwroot", "css/critical"),
                Path.Combine("/app/epdata/wwwroot", "css/critical")
            };

            string ruta = null;
            int i = 0;
            while(i < rutasPosibles.Count() && String.IsNullOrEmpty(ruta))
            {
                if (Directory.Exists(rutasPosibles[i]))
                {
                    ruta = rutasPosibles[i];
                }
                i++;
            }
            if (String.IsNullOrEmpty(ruta))
            {
                throw new DirectoryNotFoundException($"No se pudo encontrar la carpeta de CSS crítico en ninguna de las rutas: {string.Join(", ", rutasPosibles)}");
            }

            List<string> aNombreArchivoCssCritico = Directory.GetFiles(ruta, "*.min.css").Select(Path.GetFileName).ToList();

            foreach (ContenidoCssCritico.Archivo iArchivo in Enum.GetValues(typeof(ContenidoCssCritico.Archivo)))
            {
                string sNombreArchivoCssCritico;
                switch (iArchivo)
                {
                    case ContenidoCssCritico.Archivo.Cabecera:
                        sNombreArchivoCssCritico = "cabecera.min.css";
                        break;
                    case ContenidoCssCritico.Archivo.Home:
                        sNombreArchivoCssCritico = "home.min.css";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (aNombreArchivoCssCritico.Exists(x => x == sNombreArchivoCssCritico))
                {
                    string sRuta = Path.Combine(ruta, sNombreArchivoCssCritico);
                    if (File.Exists(sRuta))
                    {
                        string sContenidoArchivo = File.ReadAllText(sRuta);
                        ContenidoArchivoCssCriticoPorNombre.Add(iArchivo, sContenidoArchivo);
                    }
                    else
                    {
                        throw new FileNotFoundException($"No se pudo encontrar el archivo CSS crítico: {sRuta}");
                    }
                }
                else
                {
                    throw new FileNotFoundException($"No se pudo encontrar el archivo CSS crítico: {sNombreArchivoCssCritico}");
                }
            }
            */
        }
    }
}
