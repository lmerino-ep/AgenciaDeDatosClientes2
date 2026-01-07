namespace Modelos.DTO.Herramientas
{
    public class DropdownFiltrable
    {
        public Dictionary<string, List<ElementoDropdownFiltrable>> Elementos { get; set; }
        public string Placeholder { get; set; }
        public string Valor { get; set; }
        public bool Habilitado { get; set; }
    }
}
