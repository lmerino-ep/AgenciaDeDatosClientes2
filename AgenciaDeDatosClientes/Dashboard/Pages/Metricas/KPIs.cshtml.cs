using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO.Metricas;
using Modelos.InyeccionDeDependencias;
using Negocio;

namespace Pages.Metricas
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.ConsultaMetricas)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class KPIsModel : PageModel
    {
        private readonly IServicioMetricas _servicioMetricas;
        private readonly ILogger<KPIsModel> _logger;

        public KPIsModel(IServicioMetricas servicioMetricas, ILogger<KPIsModel> logger)
        {
            _servicioMetricas = servicioMetricas;
            _logger = logger;
        }

        // Propiedades de la página
        public List<Kpi> KpisDisponibles { get; set; } = new List<Kpi>();

        [BindProperty(SupportsGet = true)]
        public string? KpiSeleccionado { get; set; }

        [BindProperty(SupportsGet = true)]
        public Dictionary<string, string> Parametros { get; set; } = new Dictionary<string, string>();

        [BindProperty(SupportsGet = true)]
        public bool ForzarRecalculo { get; set; }

        public ResultadoKpi? Resultado { get; set; }

        public string? MensajeError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Obtener lista de KPIs disponibles
                KpisDisponibles = _servicioMetricas.ObtenerKpisDisponibles();

                // Si hay un KPI seleccionado, ejecutarlo
                if (!string.IsNullOrEmpty(KpiSeleccionado))
                {
                    // Convertir parámetros string a Dictionary<string, object>
                    var parametrosObject = new Dictionary<string, object>();
                    foreach (var param in Parametros)
                    {
                        if (!string.IsNullOrWhiteSpace(param.Value))
                        {
                            parametrosObject[param.Key] = param.Value;
                        }
                    }

                    Resultado = await _servicioMetricas.ObtenerKpiAsync(
                        KpiSeleccionado,
                        parametrosObject,
                        ForzarRecalculo
                    );

                    _logger.LogInformation(
                        "KPI {KpiId} ejecutado correctamente. Registros: {TotalRegistros}, Duración: {DuracionMs}ms, Desde caché: {DesdCache}",
                        KpiSeleccionado,
                        Resultado.TotalRegistros,
                        Resultado.DuracionMs,
                        Resultado.DesdCache
                    );
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar KPI {KpiId}", KpiSeleccionado);
                MensajeError = $"Error al ejecutar el KPI: {ex.Message}";
                KpisDisponibles = _servicioMetricas.ObtenerKpisDisponibles();
                return Page();
            }
        }

        /// <summary>
        /// Exporta el KPI actual a JSON
        /// </summary>
        public async Task<IActionResult> OnGetExportarJsonAsync(string kpiId, string parametrosJson)
        {
            try
            {
                var parametros = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametrosJson)
                    ?? new Dictionary<string, object>();

                var resultado = await _servicioMetricas.ExportarKpiAsync(kpiId, parametros, "json");

                var nombreArchivo = $"kpi_{kpiId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                return File(
                    System.Text.Encoding.UTF8.GetBytes(resultado.ToJson()),
                    "application/json",
                    nombreArchivo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar KPI {KpiId} a JSON", kpiId);
                TempData["Error"] = $"Error al exportar a JSON: {ex.Message}";
                return RedirectToPage(new { kpiSeleccionado = kpiId });
            }
        }

        /// <summary>
        /// Exporta el KPI actual a CSV
        /// </summary>
        public async Task<IActionResult> OnGetExportarCsvAsync(string kpiId, string parametrosJson)
        {
            try
            {
                var parametros = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametrosJson)
                    ?? new Dictionary<string, object>();

                var resultado = await _servicioMetricas.ExportarKpiAsync(kpiId, parametros, "csv");

                var nombreArchivo = $"kpi_{kpiId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                // UTF-8 con BOM para compatibilidad con Excel español
                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var csv = System.Text.Encoding.UTF8.GetBytes(resultado.ToCsv());
                var csvConBom = bom.Concat(csv).ToArray();

                return File(
                    csvConBom,
                    "text/csv",
                    nombreArchivo
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar KPI {KpiId} a CSV", kpiId);
                TempData["Error"] = $"Error al exportar a CSV: {ex.Message}";
                return RedirectToPage(new { kpiSeleccionado = kpiId });
            }
        }
    }
}
