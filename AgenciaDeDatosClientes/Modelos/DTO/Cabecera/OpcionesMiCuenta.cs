namespace Modelos.DTO.Cabecera
{
    /// <summary>
    /// Modelo para las opciones del menú "Mi Cuenta"
    /// </summary>
    public class OpcionesMiCuenta
    {
        public List<OpcionMiCuenta> Opciones { get; set; } = new List<OpcionMiCuenta>();
        public bool HaySesion { get; set; }
    }
}
