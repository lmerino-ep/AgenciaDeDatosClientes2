using Datos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modelos.Dashboard;
using System.Security.Claims;

namespace Negocio;

public class ServicioContextoDashboard : IServicioContextoDashboard
{
    private static readonly HashSet<string> TiposUsuarioInternos = ["2", "3", "4", "5", "6"];

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepositorioProveedores _repositorioProveedores;
    private readonly IServicioSuscripciones _servicioSuscripciones;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServicioContextoDashboard> _logger;

    public ServicioContextoDashboard(
        IHttpContextAccessor httpContextAccessor,
        IRepositorioProveedores repositorioProveedores,
        IServicioSuscripciones servicioSuscripciones,
        IConfiguration configuration,
        ILogger<ServicioContextoDashboard> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _repositorioProveedores = repositorioProveedores;
        _servicioSuscripciones = servicioSuscripciones;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ContextoDashboardUsuario> ObtenerContextoAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return CrearContextoAnonimo();
        }

        return await ConstruirContextoAsync(httpContext.User);
    }

    public FuenteProveedorSeleccion ResolverFuenteProveedor(ContextoDashboardUsuario contexto, int? fkFuenteSolicitada)
    {
        if (contexto.Perfil != PerfilDashboard.Proveedor || contexto.FuentesVinculadas.Count == 0)
        {
            throw new InvalidOperationException("El usuario no es proveedor con fuentes vinculadas.");
        }

        if (fkFuenteSolicitada.HasValue)
        {
            var fuente = contexto.FuentesVinculadas.FirstOrDefault(f => f.FkFuente == fkFuenteSolicitada.Value);
            if (fuente == null)
            {
                throw new UnauthorizedAccessException("No tiene acceso a la fuente seleccionada.");
            }

            return new FuenteProveedorSeleccion
            {
                FkFuente = fuente.FkFuente,
                NombreFuente = fuente.NombreFuente
            };
        }

        var primera = contexto.FuentesVinculadas[0];
        return new FuenteProveedorSeleccion
        {
            FkFuente = primera.FkFuente,
            NombreFuente = primera.NombreFuente
        };
    }

    public bool EsKpiPermitidoParaContexto(ContextoDashboardUsuario contexto, string kpiId)
    {
        if (string.IsNullOrWhiteSpace(kpiId))
        {
            return false;
        }

        if (contexto.PuedeConsultarMetricasGlobales && !kpiId.StartsWith("proveedor-", StringComparison.Ordinal))
        {
            return true;
        }

        if (contexto.PuedeConsultarMetricasProveedor && kpiId.StartsWith("proveedor-", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private async Task<ContextoDashboardUsuario> ConstruirContextoAsync(ClaimsPrincipal user)
    {
        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            return CrearContextoAnonimo();
        }

        var fkUsuario = ObtenerFkUsuario(user);
        if (!fkUsuario.HasValue)
        {
            return CrearContextoAnonimo();
        }

        var usuarioTipo = user.FindFirst("FK_UsuarioTipo")?.Value ?? string.Empty;
        var esInterno = TiposUsuarioInternos.Contains(usuarioTipo);

        var puedeMetricasGlobales = esInterno && (
            user.IsInRole("Dashboard - Consulta de Métricas")
            || user.IsInRole("Dashboard - Exportación de Métricas")
            || user.IsInRole("Dashboard - Acceso total"));

        var puedeExportarGlobales = esInterno && (
            user.IsInRole("Dashboard - Exportación de Métricas")
            || user.IsInRole("Dashboard - Acceso total"));

        if (esInterno)
        {
            return new ContextoDashboardUsuario
            {
                Perfil = PerfilDashboard.Interno,
                FkUsuario = fkUsuario.Value,
                PuedeConsultarMetricasGlobales = puedeMetricasGlobales,
                PuedeExportarMetricasGlobales = puedeExportarGlobales,
                PuedeConsultarMetricasProveedor = false,
                PuedeExportarMetricasProveedor = false
            };
        }

        var fuentes = await ObtenerFuentesConDebugAsync(fkUsuario.Value);
        if (fuentes.Count > 0)
        {
            return new ContextoDashboardUsuario
            {
                Perfil = PerfilDashboard.Proveedor,
                FkUsuario = fkUsuario.Value,
                FuentesVinculadas = fuentes,
                PuedeConsultarMetricasGlobales = false,
                PuedeConsultarMetricasProveedor = true,
                PuedeExportarMetricasGlobales = false,
                PuedeExportarMetricasProveedor = true
            };
        }

        var contextoTarifa = await _servicioSuscripciones.ObtenerContextoTarifaAsync(fkUsuario.Value, usuarioTipo);
        var perfilCliente = contextoTarifa.TieneSuscripcionActiva
            ? PerfilDashboard.ClienteConSuscripcion
            : PerfilDashboard.ClienteSinSuscripcion;

        return new ContextoDashboardUsuario
        {
            Perfil = perfilCliente,
            FkUsuario = fkUsuario.Value,
            PuedeConsultarMetricasGlobales = false,
            PuedeConsultarMetricasProveedor = false,
            PuedeExportarMetricasGlobales = false,
            PuedeExportarMetricasProveedor = false
        };
    }

    private async Task<IReadOnlyList<FuenteProveedorVinculo>> ObtenerFuentesConDebugAsync(int fkUsuario)
    {
        var fuentes = await _repositorioProveedores.ObtenerFuentesVinculadasAsync(fkUsuario);
        if (fuentes.Count > 0)
        {
            return fuentes;
        }

        if (int.TryParse(_configuration["Dashboard:ForzarFkFuenteProveedor"], out var forzarFkFuente) && forzarFkFuente > 0)
        {
            _logger.LogWarning(
                "Modo debug: forzando fuente proveedor {FkFuente} para usuario {FkUsuario}",
                forzarFkFuente,
                fkUsuario);

            return
            [
                new FuenteProveedorVinculo
                {
                    ProveedorId = 0,
                    FkFuente = forzarFkFuente,
                    NombreFuente = $"Fuente debug #{forzarFkFuente}",
                    NombreProveedor = "Debug"
                }
            ];
        }

        return fuentes;
    }

    private static int? ObtenerFkUsuario(ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var fkUsuario) ? fkUsuario : null;
    }

    private static ContextoDashboardUsuario CrearContextoAnonimo() =>
        new() { Perfil = PerfilDashboard.ClienteSinSuscripcion };
}
