using Datos;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Metricas;
using StackExchange.Redis;
using System.Text.Json;

namespace Negocio
{
    public class ServicioMetricas : IServicioMetricas
    {
        private readonly IRepositorioMetricas _repositorio;
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
            IConnectionMultiplexer redis,
            ILogger<ServicioMetricas> logger)
        {
            _repositorio = repositorio;
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene un KPI, primero intentando desde caché y si no existe ejecutando la consulta
        /// </summary>
        public async Task<ResultadoKpi> ObtenerKpiAsync(string kpiId, Dictionary<string, object> parametros, bool forzarRecalculo = false)
        {
            string claveCache = GenerarClaveCache(kpiId, parametros);

            // Si no se fuerza recalculo, intentar obtener de caché
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

            // Si no está en caché o se forzó recalculo, ejecutar consulta
            _logger.LogInformation("Ejecutando KPI {KpiId} desde base de datos", kpiId);
            var resultado = await _repositorio.EjecutarKpiAsync(kpiId, parametros);

            // Guardar en caché
            await GuardarEnCache(claveCache, resultado);

            return resultado;
        }

        /// <summary>
        /// Obtiene la lista de KPIs disponibles
        /// </summary>
        public List<Kpi> ObtenerKpisDisponibles()
        {
            return _repositorio.ObtenerKpisDisponibles();
        }

        /// <summary>
        /// Exporta un KPI en el formato especificado (JSON o CSV)
        /// </summary>
        public async Task<ResultadoKpi> ExportarKpiAsync(string kpiId, Dictionary<string, object> parametros, string formato)
        {
            if (formato != "json" && formato != "csv")
            {
                throw new ArgumentException("Formato de exportación no válido. Use 'json' o 'csv'");
            }

            // Obtener el resultado (con caché)
            var resultado = await ObtenerKpiAsync(kpiId, parametros);

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
        private string GenerarClaveCache(string kpiId, Dictionary<string, object> parametros)
        {
            // Serializar parámetros a JSON ordenado para consistencia
            var parametrosOrdenados = parametros
                .OrderBy(p => p.Key)
                .ToDictionary(p => p.Key, p => p.Value);

            string parametrosJson = JsonSerializer.Serialize(parametrosOrdenados);
            string hash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(parametrosJson)
                )
            ).Substring(0, 16); // Primeros 16 caracteres del hash

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
    }
}
