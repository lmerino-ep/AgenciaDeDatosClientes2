using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO;
using Negocio;
using System.Text.Json;

namespace Pages.Auditoria
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.Auditoria)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class DetalleModel : PageModel
    {
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<DetalleModel> _logger;

        public DetalleModel(
            IServicioAuditoria servicioAuditoria,
            ILogger<DetalleModel> logger)
        {
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public AuditoriaDetalle Auditoria { get; set; }
        public List<CambioRealizado> Cambios { get; set; } = new List<CambioRealizado>();
        public string? MensajeError { get; set; }

        public class CambioRealizado
        {
            public string Campo { get; set; }
            public string Antes { get; set; }
            public string Despues { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (Id <= 0)
                {
                    MensajeError = "ID de auditoría no válido.";
                    return Page();
                }

                Auditoria = await _servicioAuditoria.ObtenerAuditoriaPorIdAsync(Id);

                if (Auditoria == null)
                {
                    MensajeError = "No se encontró el registro de auditoría solicitado.";
                    return Page();
                }


                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalle de auditoría {Id}", Id);
                MensajeError = "Error al cargar el detalle de auditoría. Por favor, intenta nuevamente.";
                return Page();
            }
        }

    }
}

