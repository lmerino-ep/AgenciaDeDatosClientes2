namespace Utils.Middlewares.CdnHeaders
{
    /// <summary>
    /// Estrategia de caché para diferentes tipos de contenido
    /// </summary>
    public class CacheStrategy
    {
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public TimeSpan? SharedMaxAge { get; set; }
        public bool MustRevalidate { get; set; }
        public bool NoCache { get; set; }
        public bool NoStore { get; set; }
        public bool IncludeETag { get; set; }
        public bool IncludeLastModified { get; set; }
        public string[]? VaryHeaders { get; set; }
    }
}

