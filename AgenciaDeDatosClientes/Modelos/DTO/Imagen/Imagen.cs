namespace Modelos.DTO.Imagen
{
    public class Imagen
    {
        public bool MostrandoEnPrimerPantallazo { get; set; } //Above the fold or below the fold
        public Dictionary<int, string> UrlPorIntrinsicWidthFormatoAvif { get; set; } //El intrinsic width es el ancho en pixeles real de la foto, no el usado al pintar
        public Dictionary<int, string> UrlPorIntrinsicWidthFormatoWebp { get; set; } //El intrinsic width es el ancho en pixeles real de la foto, no el usado al pintar
        public Dictionary<int, string> UrlPorIntrinsicWidthFormatoFallback { get; set; } //El intrinsic width es el ancho en pixeles real de la foto, no el usado al pintar
        public Dictionary<string, string> AnchoPorMediaQueriesParaSizes { get; set; } //no usar porcentajes, como mucho vw
        public string Alt { get; set; }
        public int Ancho { get; set; }
        public int Alto { get; set; }
        public string ClasesCss { get; set; }

        public void GenerarPropiedades()
        {
            SrcSetAvif = GetSrcset(UrlPorIntrinsicWidthFormatoAvif);
            SrcSetWebp = GetSrcset(UrlPorIntrinsicWidthFormatoWebp);
            SrcSetFallback = GetSrcset(UrlPorIntrinsicWidthFormatoFallback);

            Sizes = GetSizes();

            UrlFallback = UrlPorIntrinsicWidthFormatoFallback != null && UrlPorIntrinsicWidthFormatoFallback.Any() ? UrlPorIntrinsicWidthFormatoFallback[UrlPorIntrinsicWidthFormatoFallback.Keys.Max()] : throw new Exception("La propiedad UrlPorIntrinsicWidthFormatoFallback es obligatoria");
        }

        public string SrcSetAvif;
        public string SrcSetWebp;
        public string SrcSetFallback;

        public string Sizes;
        public string UrlFallback;

        private string GetSrcset(Dictionary<int, string> urlPorIntrinsicWidth)
        {
            List<string> srcsets = new List<string>();
            if (urlPorIntrinsicWidth != null)
            {
                foreach (int intrinsicWidth in urlPorIntrinsicWidth.Keys)
                {
                    srcsets.Add($"{urlPorIntrinsicWidth[intrinsicWidth]} {intrinsicWidth}w");
                }
            }
            return string.Join(", ", srcsets);
        }

        private string GetSizes()
        {
            List<string> sizes = new List<string>();
            if (AnchoPorMediaQueriesParaSizes != null)
            {
                foreach (string mediaQuery in AnchoPorMediaQueriesParaSizes.Keys)
                {
                    string mediaQueryLimpia = mediaQuery;
                    if (!mediaQueryLimpia.StartsWith("("))
                    {
                        mediaQueryLimpia = $"({mediaQueryLimpia}";
                    }
                    if (!mediaQueryLimpia.EndsWith(")"))
                    {
                        mediaQueryLimpia = $"{mediaQueryLimpia})";
                    }
                    sizes.Add($"{mediaQueryLimpia} {AnchoPorMediaQueriesParaSizes[mediaQuery]}"); //Ej: (max-width: 735px) 100vw
                }
            }
            else
            {
                sizes.Add($"{Ancho}px");
            }

            return string.Join(", ", sizes);
        }

        public static string GetUrlAbsoluta(string rutaImagen)
        {
            return $"https://file.epdata.es/{rutaImagen}";
        }
    }
}
