namespace Modelos.DTO.Cabecera
{
    /// <summary>
    /// Representa una opción individual del menú
    /// </summary>
    public class OpcionMiCuenta
    {
        public string Icono { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public string? Controlador { get; set; }
        public string? Accion { get; set; }
        public string? Pagina { get; set; }
        public string? Url { get; set; }
    }
}

