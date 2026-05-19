using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.Dashboard;
using Modelos.DTO.Metricas;
using Negocio;
using Utils.Authorization;

namespace Pages.Metricas;

[Authorize(Policy = PoliticasAutorizacion.Politicas.ConsultaMetricasOProveedor)]
[ResponseCache(CacheProfileName = "NoCache")]
public class KPIsModel : PageModel
{
    private readonly IServicioMetricas _servicioMetricas;
    private readonly IServicioContextoDashboard _servicioContextoDashboard;
    private readonly ILogger<KPIsModel> _logger;
    private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;

    public KPIsModel(
        IServicioMetricas servicioMetricas,
        IServicioContextoDashboard servicioContextoDashboard,
        ILogger<KPIsModel> logger,
        UserManager<Modelos.Autenticacion.Usuario> userManager)
    {
        _servicioMetricas = servicioMetricas;
        _servicioContextoDashboard = servicioContextoDashboard;
        _logger = logger;
        _userManager = userManager;
    }

    public List<Kpi> KpisDisponibles { get; set; } = [];

    public ContextoDashboardUsuario? ContextoDashboard { get; set; }

    public bool EsProveedor { get; set; }

    public int? FkFuenteActiva { get; set; }

    public string? NombreFuenteActiva { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? KpiSeleccionado { get; set; }

    [BindProperty(SupportsGet = true)]
    public Dictionary<string, string> Parametros { get; set; } = new Dictionary<string, string>();

    [BindProperty(SupportsGet = true)]
    public bool ForzarRecalculo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FkFuenteProveedor { get; set; }

    public ResultadoKpi? Resultado { get; set; }

    public string? MensajeError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await CargarContextoYFuenteAsync();

            if (!ContextoDashboard!.PuedeConsultarMetricasGlobales && !ContextoDashboard.PuedeConsultarMetricasProveedor)
            {
                return Forbid();
            }

            KpisDisponibles = await _servicioMetricas.ObtenerKpisDisponiblesAsync();

            if (!string.IsNullOrEmpty(KpiSeleccionado))
            {
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
                    ForzarRecalculo,
                    FkFuenteActiva);

                _logger.LogInformation(
                    "KPI {KpiId} ejecutado. Registros: {TotalRegistros}, Duración: {DuracionMs}ms, Desde caché: {DesdCache}",
                    KpiSeleccionado,
                    Resultado.TotalRegistros,
                    Resultado.DuracionMs,
                    Resultado.DesdCache);
            }

            return Page();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso denegado al KPI {KpiId}", KpiSeleccionado);
            MensajeError = ex.Message;
            await CargarContextoYFuenteAsync();
            KpisDisponibles = await _servicioMetricas.ObtenerKpisDisponiblesAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar KPI {KpiId}", KpiSeleccionado);
            MensajeError = $"Error al ejecutar el KPI: {ex.Message}";
            await CargarContextoYFuenteAsync();
            KpisDisponibles = await _servicioMetricas.ObtenerKpisDisponiblesAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnGetExportarJsonAsync(string kpiId, string parametrosJson, int? fkFuenteProveedor)
    {
        if (!await PuedeExportarAsync(kpiId))
        {
            int fkUsuario = Convert.ToInt32(_userManager.GetUserId(HttpContext.User));
            _logger.LogInformation("El usuario {FkUsuario} no tiene permisos para exportar el KPI {KpiId} a JSON", fkUsuario, kpiId);
            TempData["Error"] = "ERROR: No tienes permisos para exportar métricas";
            return RedirectToPage(new { kpiSeleccionado = kpiId, fkFuenteProveedor });
        }

        try
        {
            await CargarContextoYFuenteAsync();
            var fkFuente = EsProveedor ? FkFuenteActiva : fkFuenteProveedor;

            var parametros = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametrosJson)
                ?? new Dictionary<string, object>();

            var resultado = await _servicioMetricas.ExportarKpiAsync(kpiId, parametros, "json", fkFuente);
            var nombreArchivo = $"kpi_{kpiId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            return File(
                System.Text.Encoding.UTF8.GetBytes(resultado.ToJson()),
                "application/json",
                nombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar KPI {KpiId} a JSON", kpiId);
            TempData["Error"] = $"Error al exportar a JSON: {ex.Message}";
            return RedirectToPage(new { kpiSeleccionado = kpiId, fkFuenteProveedor });
        }
    }

    public async Task<IActionResult> OnGetExportarCsvAsync(string kpiId, string parametrosJson, int? fkFuenteProveedor)
    {
        if (!await PuedeExportarAsync(kpiId))
        {
            TempData["Error"] = "ERROR: No tienes permisos para exportar métricas";
            return RedirectToPage(new { kpiSeleccionado = kpiId, fkFuenteProveedor });
        }

        try
        {
            await CargarContextoYFuenteAsync();
            var fkFuente = EsProveedor ? FkFuenteActiva : fkFuenteProveedor;

            var parametros = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametrosJson)
                ?? new Dictionary<string, object>();

            var resultado = await _servicioMetricas.ExportarKpiAsync(kpiId, parametros, "csv", fkFuente);
            var nombreArchivo = $"kpi_{kpiId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var csv = System.Text.Encoding.UTF8.GetBytes(resultado.ToCsv());
            var csvConBom = bom.Concat(csv).ToArray();

            return File(csvConBom, "text/csv", nombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar KPI {KpiId} a CSV", kpiId);
            TempData["Error"] = $"Error al exportar a CSV: {ex.Message}";
            return RedirectToPage(new { kpiSeleccionado = kpiId, fkFuenteProveedor });
        }
    }

    private async Task CargarContextoYFuenteAsync()
    {
        ContextoDashboard = await _servicioContextoDashboard.ObtenerContextoAsync();
        EsProveedor = ContextoDashboard.Perfil == PerfilDashboard.Proveedor;

        if (!EsProveedor)
        {
            return;
        }

        var fuente = _servicioContextoDashboard.ResolverFuenteProveedor(ContextoDashboard, FkFuenteProveedor);
        FkFuenteActiva = fuente.FkFuente;
        NombreFuenteActiva = fuente.NombreFuente;
    }

    private async Task<bool> PuedeExportarAsync(string kpiId)
    {
        var contexto = await _servicioContextoDashboard.ObtenerContextoAsync();

        if (contexto.PuedeConsultarMetricasProveedor
            && kpiId.StartsWith("proveedor-", StringComparison.Ordinal))
        {
            return true;
        }

        return contexto.PuedeExportarMetricasGlobales
            && (User.IsInRole(PoliticasAutorizacion.Roles.ExportacionMetricas)
                || User.IsInRole(PoliticasAutorizacion.Roles.AccesoTotal));
    }
}
