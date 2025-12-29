namespace Utils.Middlewares.CabecerasDeSeguridad
{
    public class CabecerasDeSeguridadPolicy
    {
        public IDictionary<string, string> SetHeaders { get; } = new Dictionary<string, string>();
        public ISet<string> RemoveHeaders { get; } = new HashSet<string>();
    }
}
