using Datos;
using Microsoft.Extensions.Logging;
using Modelos.Dashboard;
using Modelos.DTO.Metricas;
using StackExchange.Redis;
using System.Text.Json;

namespace Negocio
{
    public class ServicioMetricas : IServicioMetricas
    {
        private readonly IRepositorioMetricas _repositorio;
        private readonly IRepositorioMetricasProveedor _repositorioProveedor;
        private readonly IServicioContextoDashboard _contextoDashboard;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ServicioMetricas> _logger;
        private const int TtlCacheMinutos = 10;

        // KPIs que se regeneran periódicamente (los más consultados)
        private static readonly List<string> KpisPrincipales = new List<string>
        {
            "actividad-total",
            "usuarios-por-tipo",
            "usuarios-activos-mes",
            "tokens-activos",
            "representaciones-estadisticas",
            "plantillas-estadisticas"
        };

        public ServicioMetricas(
            IRepositorioMetricas repositorio,
            IRepositorioMetricasProveedor repositorioProveedor,
            IServicioContextoDashboard contextoDashboard,
            IConnectionMultiplexer redis,
            ILogger<ServicioMetricas> logger)
        {
            _repositorio = repositorio;
            _repositorioProveedor = repositorioProveedor;
            _contextoDashboard = contextoDashboard;
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene un KPI, primero intentando desde caché y si no existe ejecutando la consulta
        /// </summary>
        public async Task<ResultadoKpi> ObtenerKpiAsync(
            string kpiId,
            Dictionary<string, object> parametros,
            bool forzarRecalculo = false,
            int? fkFuenteProveedor = null)
        {
            var contexto = await _contextoDashboard.ObtenerContextoAsync();
            if (!_contextoDashboard.EsKpiPermitidoParaContexto(contexto, kpiId))
            {
                throw new UnauthorizedAccessException($"No tiene permiso para ejecutar el KPI '{kpiId}'.");
            }

            int? fkFuente = null;
            if (kpiId.StartsWith("proveedor-", StringComparison.Ordinal))
            {
                fkFuente = _contextoDashboard
                    .ResolverFuenteProveedor(contexto, fkFuenteProveedor)
                    .FkFuente;
            }

            string claveCache = GenerarClaveCache(kpiId, parametros, fkFuente);

            if (!forzarRecalculo)
            {
                var resultadoCache = await ObtenerDeCache(claveCache);
                if (resultadoCache != null)
                {
                    _logger.LogInformation("KPI {KpiId} obtenido de caché Redis", kpiId);
                    resultadoCache.DesdCache = true;
                    return resultadoCache;
                }
            }

            _logger.LogInformation("Ejecutando KPI {KpiId} desde base de datos", kpiId);
            var resultado = await EjecutarKpiEnRepositorioAsync(kpiId, parametros, fkFuente);

            if (contexto.Perfil == PerfilDashboard.Proveedor && fkFuente.HasValue)
            {
                _logger.LogInformation(
                    "KPI proveedor ejecutado: usuario={FkUsuario}, fuente={FkFuente}, kpi={KpiId}, registros={TotalRegistros}",
                    contexto.FkUsuario,
                    fkFuente.Value,
                    kpiId,
                    resultado.TotalRegistros);
            }

            await GuardarEnCache(claveCache, resultado);

            return resultado;
        }

        public async Task<List<Kpi>> ObtenerKpisDisponiblesAsync()
        {
            var contexto = await _contextoDashboard.ObtenerContextoAsync();

            if (contexto.PuedeConsultarMetricasGlobales)
            {
                return _repositorio.ObtenerKpisDisponibles();
            }

            if (contexto.PuedeConsultarMetricasProveedor)
            {
                return _repositorioProveedor.ObtenerKpisDisponibles();
            }

            return [];
        }

        public Task<ResumenDashboardProveedor> ObtenerResumenDashboardProveedorAsync(int fkFuente) =>
            _repositorioProveedor.ObtenerResumenDashboardAsync(fkFuente);

        public Task<Dictionary<string, int>> ObtenerActividadMensualProveedorAsync(int fkFuente) =>
            _repositorioProveedor.ObtenerActividadMensualUltimos6MesesAsync(fkFuente);

        public Task<Dictionary<string, int>> ObtenerActividadDiariaProveedorAsync(int fkFuente) =>
            _repositorioProveedor.ObtenerActividadDiariaAsync(fkFuente, 7);

        public Task<Dictionary<string, int>> ObtenerDescargasPorFormatoProveedorAsync(int fkFuente) =>
            _repositorioProveedor.ObtenerDescargasPorFormatoAsync(fkFuente);

        private Task<ResultadoKpi> EjecutarKpiEnRepositorioAsync(
            string kpiId,
            Dictionary<string, object> parametros,
            int? fkFuenteProveedor)
        {
            if (kpiId.StartsWith("proveedor-", StringComparison.Ordinal))
            {
                if (!fkFuenteProveedor.HasValue)
                {
                    throw new InvalidOperationException("No se indicó la fuente para el KPI de proveedor.");
                }

                return _repositorioProveedor.EjecutarKpiAsync(kpiId, fkFuenteProveedor.Value, parametros);
            }

            return _repositorio.EjecutarKpiAsync(kpiId, parametros);
        }

        /// <summary>
        /// Exporta un KPI en el formato especificado (JSON o CSV)
        /// </summary>
        public async Task<ResultadoKpi> ExportarKpiAsync(
            string kpiId,
            Dictionary<string, object> parametros,
            string formato,
            int? fkFuenteProveedor = null)
        {
            if (formato != "json" && formato != "csv")
            {
                throw new ArgumentException("Formato de exportación no válido. Use 'json' o 'csv'");
            }

            var resultado = await ObtenerKpiAsync(kpiId, parametros, fkFuenteProveedor: fkFuenteProveedor);

            _logger.LogInformation("Exportando KPI {KpiId} en formato {Formato}", kpiId, formato);

            return resultado;
        }

        /// <summary>
        /// Regenera los KPIs más consultados para mantener el caché caliente
        /// </summary>
        public async Task RegenerarKpisPrincipalesAsync()
        {
            _logger.LogInformation("Iniciando regeneración de KPIs principales");

            foreach (var kpiId in KpisPrincipales)
            {
                try
                {
                    // Regenerar con parámetros por defecto (sin filtros)
                    await ObtenerKpiAsync(kpiId, new Dictionary<string, object>(), forzarRecalculo: true);
                    _logger.LogInformation("KPI {KpiId} regenerado correctamente", kpiId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al regenerar KPI {KpiId}", kpiId);
                }
            }

            _logger.LogInformation("Regeneración de KPIs principales completada");
        }

        #region Métodos privados de caché

        /// <summary>
        /// Genera una clave única de caché basada en el KPI y sus parámetros
        /// </summary>
        private static string GenerarClaveCache(string kpiId, Dictionary<string, object> parametros, int? fkFuenteProveedor)
        {
            var parametrosOrdenados = parametros
                .OrderBy(p => p.Key)
                .ToDictionary(p => p.Key, p => p.Value);

            string parametrosJson = JsonSerializer.Serialize(parametrosOrdenados);
            string hash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(parametrosJson)
                )
            ).Substring(0, 16);

            if (kpiId.StartsWith("proveedor-", StringComparison.Ordinal) && fkFuenteProveedor.HasValue)
            {
                return $"kpi:proveedor:{fkFuenteProveedor.Value}:{kpiId}:{hash}";
            }

            return $"kpi:{kpiId}:{hash}";
        }

        /// <summary>
        /// Obtiene un resultado de caché de Redis
        /// </summary>
        private async Task<ResultadoKpi?> ObtenerDeCache(string claveCache)
        {
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);

                if (valorCache.HasValue)
                {
                    return JsonSerializer.Deserialize<ResultadoKpi>(valorCache!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener de caché Redis, clave: {ClaveCache}", claveCache);
            }

            return null;
        }

        /// <summary>
        /// Guarda un resultado en caché de Redis con TTL
        /// </summary>
        private async Task GuardarEnCache(string claveCache, ResultadoKpi resultado)
        {
            try
            {
                var db = _redis.GetDatabase();
                string json = JsonSerializer.Serialize(resultado);
                await db.StringSetAsync(claveCache, json, TimeSpan.FromMinutes(TtlCacheMinutos));

                _logger.LogDebug("Resultado guardado en caché Redis: {ClaveCache}, TTL: {Ttl} minutos", claveCache, TtlCacheMinutos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar en caché Redis, clave: {ClaveCache}", claveCache);
            }
        }

        #endregion

        #region Métricas del Dashboard Principal

        /// <summary>
        /// Obtiene el número de usuarios activos en los últimos 30 días (con caché)
        /// </summary>
        public async Task<int> ObtenerUsuariosActivosAsync()
        {
            const string claveCache = "dashboard:usuarios-activos";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue && int.TryParse(valorCache, out int valor))
                {
                    return valor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener usuarios activos de caché");
            }

            // Si no está en caché, obtener de BD
            var resultado = await _repositorio.ObtenerUsuariosActivosAsync();
            
            // Guardar en caché (5 minutos de TTL para métricas del dashboard)
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(claveCache, resultado.ToString(), TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar usuarios activos en caché");
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene el número de actividades del mes actual (con caché)
        /// </summary>
        public async Task<int> ObtenerActividadMensualAsync()
        {
            const string claveCache = "dashboard:actividad-mensual";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue && int.TryParse(valorCache, out int valor))
                {
                    return valor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener actividad mensual de caché");
            }

            var resultado = await _repositorio.ObtenerActividadMensualAsync();
            
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(claveCache, resultado.ToString(), TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar actividad mensual en caché");
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene el número de acciones realizadas hoy (con caché corto, 1 minuto)
        /// </summary>
        public async Task<int> ObtenerAccionesHoyAsync()
        {
            const string claveCache = "dashboard:acciones-hoy";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue && int.TryParse(valorCache, out int valor))
                {
                    return valor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener acciones hoy de caché");
            }

            var resultado = await _repositorio.ObtenerAccionesHoyAsync();
            
            // Caché más corto para "hoy" (1 minuto)
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(claveCache, resultado.ToString(), TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar acciones hoy en caché");
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene el total de usuarios activos en el sistema (con caché)
        /// </summary>
        public async Task<int> ObtenerTotalUsuariosAsync()
        {
            const string claveCache = "dashboard:total-usuarios";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue && int.TryParse(valorCache, out int valor))
                {
                    return valor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener total usuarios de caché");
            }

            var resultado = await _repositorio.ObtenerTotalUsuariosAsync();
            
            // Caché más largo para total de usuarios (15 minutos, cambia poco)
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(claveCache, resultado.ToString(), TimeSpan.FromMinutes(15));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar total usuarios en caché");
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene actividad mensual de los últimos 6 meses (para gráfico)
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync()
        {
            const string claveCache = "dashboard:actividad-mensual-6meses";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue)
                {
                    var datos = JsonSerializer.Deserialize<Dictionary<string, int>>(valorCache);
                    if (datos != null)
                        return datos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener actividad mensual de caché");
            }

            var resultado = await _repositorio.ObtenerActividadMensualUltimos6MesesAsync();
            
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(resultado);
                await db.StringSetAsync(claveCache, json, TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar actividad mensual en caché");
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene actividad diaria de los últimos 7 días (para gráfico)
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerActividadDiariaUltimos7DiasAsync()
        {
            const string claveCache = "dashboard:actividad-diaria-7dias";
            
            try
            {
                var db = _redis.GetDatabase();
                var valorCache = await db.StringGetAsync(claveCache);
                
                if (valorCache.HasValue)
                {
                    var datos = JsonSerializer.Deserialize<Dictionary<string, int>>(valorCache);
                    if (datos != null)
                        return datos;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener actividad diaria de caché");
            }

            var resultado = await _repositorio.ObtenerActividadDiariaUltimos7DiasAsync();
            
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(resultado);
                // Caché más corto para datos diarios (1 minuto)
                await db.StringSetAsync(claveCache, json, TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar actividad diaria en caché");
            }

            return resultado;
        }

        #endregion
    }
}
