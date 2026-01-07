using Modelos;

namespace Modelos.DTO
{
    /// <summary>
    /// DTO para registrar una acción de auditoría
    /// </summary>
    public class AuditoriaDTO
    {
        public int FK_Usuario { get; set; }
        public int FK_AuditoriaAccion { get; set; }
        public string Descripcion { get; set; }
    }
}
