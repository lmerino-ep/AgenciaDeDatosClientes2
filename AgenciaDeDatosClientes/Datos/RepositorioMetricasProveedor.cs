using Dapper;

using Microsoft.Extensions.Logging;

using Modelos.Dashboard;

using Modelos.DTO.Metricas;

using Modelos.InyeccionDeDependencias;

using System.Data;

using System.Diagnostics;



namespace Datos;



public class RepositorioMetricasProveedor : BaseRepositorio, IRepositorioMetricasProveedor

{

    private const int MinimoKAnonimidad = 5;



    private readonly ILogger<RepositorioMetricasProveedor> _logger;

    private readonly string _urlBasePublicacionEpData;



    public RepositorioMetricasProveedor(

        Aurora aurora,

        Configuracion configuracion,

        ILogger<RepositorioMetricasProveedor> logger) : base(aurora)

    {

        _logger = logger;

        _urlBasePublicacionEpData = (configuracion.UrlBasePublicacionEpData ?? "https://www.epdata.es").TrimEnd('/');

    }



    public List<Kpi> ObtenerKpisDisponibles()

    {

        return

        [

            CrearKpi("proveedor-resumen", "Resumen de uso", "Totales agregados de uso de datos de la fuente", ParametrosRangoFechas()),

            CrearKpi("proveedor-descargas-por-formato", "Descargas por formato", "Descargas CSV, JSON e imagen", ParametrosRangoFechas()),

            CrearKpi("proveedor-actividad-por-mes", "Actividad por mes y tipo", "Actividad mensual agregada por tipo", ParametrosRangoFechas()),

            CrearKpi("proveedor-actividad-por-tipo", "Actividad por tipo", "Totales por tipo de actividad", ParametrosRangoFechas()),

            CrearKpi("proveedor-actividad-diaria", "Actividad diaria", "Actividad diaria agregada", ParametrosDias()),

            CrearKpi("proveedor-usuarios-unicos-por-mes", "Usuarios únicos por mes", "Conteo mensual de usuarios distintos (anonimizado)", ParametrosRangoFechas()),

            CrearKpi("proveedor-representaciones-mas-usadas", "Representaciones más usadas", "Top por interacciones; referencia con URL pública de la representación", ParametrosTopRepresentaciones()),

            CrearKpi("proveedor-descargas-top", "Top descargas por formato", "Evolución mensual por formato (gráfico) y ranking por representación (tabla)", ParametrosTopDescargas()),

            CrearKpi("proveedor-volumen-datos", "Volumen de datos en fuente", "Cantidad de valores numéricos almacenados para la fuente", []),

            CrearKpi("proveedor-series-en-fuente", "Series en la fuente", "Número de series originales asociadas a la fuente", []),

            CrearKpi("proveedor-representaciones-activas", "Representaciones activas con datos", "Conteo de representaciones activas que usan la fuente (incluye clones)", [])

        ];

    }



    public async Task<ResultadoKpi> EjecutarKpiAsync(string kpiId, int fkFuente, Dictionary<string, object> parametros)

    {

        var stopwatch = Stopwatch.StartNew();

        var kpiInfo = ObtenerKpisDisponibles().FirstOrDefault(k => k.Id == kpiId)

            ?? throw new ArgumentException($"KPI '{kpiId}' no encontrado para proveedor");



        try

        {

            List<Dictionary<string, object>> datos;

            List<Dictionary<string, object>>? datosGrafico = null;



            switch (kpiId)

            {

                case "proveedor-resumen":

                    datos = await ProveedorResumen(fkFuente, parametros);

                    break;

                case "proveedor-descargas-por-formato":

                    datos = await ProveedorDescargasPorFormato(fkFuente, parametros);

                    break;

                case "proveedor-actividad-por-mes":

                    datos = await ProveedorActividadPorMes(fkFuente, parametros);

                    break;

                case "proveedor-actividad-por-tipo":

                    datos = await ProveedorActividadPorTipo(fkFuente, parametros);

                    break;

                case "proveedor-actividad-diaria":

                    datos = await ProveedorActividadDiaria(fkFuente, parametros);

                    break;

                case "proveedor-usuarios-unicos-por-mes":

                    datos = await ProveedorUsuariosUnicosPorMes(fkFuente, parametros);

                    break;

                case "proveedor-representaciones-mas-usadas":

                    {

                        var vistas = await ProveedorRepresentacionesMasUsadas(fkFuente, parametros);

                        datos = vistas.Tabla;

                        datosGrafico = vistas.Grafico;

                        break;

                    }

                case "proveedor-descargas-top":

                    {

                        var vistas = await ProveedorDescargasTop(fkFuente, parametros);

                        datos = vistas.Tabla;

                        datosGrafico = vistas.Grafico;

                        break;

                    }

                case "proveedor-volumen-datos":

                    datos = await ProveedorVolumenDatos(fkFuente);

                    break;

                case "proveedor-series-en-fuente":

                    datos = await ProveedorSeriesEnFuente(fkFuente);

                    break;

                case "proveedor-representaciones-activas":

                    datos = await ProveedorRepresentacionesActivas(fkFuente);

                    break;

                default:

                    throw new ArgumentException($"KPI '{kpiId}' no implementado para proveedor");

            }



            stopwatch.Stop();



            return new ResultadoKpi

            {

                KpiId = kpiId,

                Nombre = kpiInfo.Nombre,

                Parametros = parametros,

                TimestampGeneracion = DateTime.UtcNow,

                TotalRegistros = datos.Count,

                Datos = datos,

                DatosGrafico = datosGrafico,

                DesdCache = false,

                DuracionMs = (int)stopwatch.ElapsedMilliseconds

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error al ejecutar KPI proveedor {KpiId} fuente {FkFuente}", kpiId, fkFuente);

            throw;

        }

    }



    public async Task<ResumenDashboardProveedor> ObtenerResumenDashboardAsync(int fkFuente)
    {
        var join = SqlJoinRepresentacionesFuente(incluirClones: true);
        var joinR2 = join.Replace("R.Id", "R2.Id", StringComparison.Ordinal);

        var sql = $@"
            SELECT
                COALESCE(SUM(CASE WHEN UA.FK_Actividad IN (1, 2, 3) THEN UA.Total ELSE 0 END), 0) AS DescargasUltimos30Dias,
                COALESCE(SUM(CASE WHEN DATE_FORMAT(UA.LastUpdate, '%Y-%m') = DATE_FORMAT(NOW(), '%Y-%m') THEN UA.Total ELSE 0 END), 0) AS ActividadMesActual,
                COUNT(DISTINCT CASE WHEN UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 30 DAY) THEN UA.FK_Usuario END) AS UsuariosUnicosUltimos30Dias,
                (
                    SELECT COUNT(DISTINCT R2.Id)
                    FROM Representacion R2
                    {joinR2}
                    WHERE R2.Activa = 1
                ) AS RepresentacionesActivasConDatos,
                (SELECT COUNT(*) FROM SerieOriginal SO WHERE SO.FK_Fuente = @FkFuente) AS SeriesEnFuente
            FROM UsuarioActividad UA
            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1
            {join}
            WHERE UA.FK_Representacion IS NOT NULL
              AND UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return await db.QueryFirstAsync<ResumenDashboardProveedor>(sql, new { FkFuente = fkFuente });

    }



    public async Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync(int fkFuente)

    {

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);

        var sql = $@"

            SELECT

                DATE_FORMAT(UA.LastUpdate, '%Y-%m') AS Fecha,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 6 MONTH)

            GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m')

            ORDER BY Fecha ASC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var filas = await db.QueryAsync<(string Fecha, int Total)>(sql, new { FkFuente = fkFuente });

        return filas.ToDictionary(f => f.Fecha, f => f.Total);

    }



    public async Task<Dictionary<string, int>> ObtenerActividadDiariaAsync(int fkFuente, int dias)

    {

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);

        var sql = $@"

            SELECT

                DATE(UA.LastUpdate) AS Fecha,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL @Dias DAY)

            GROUP BY DATE(UA.LastUpdate)

            ORDER BY Fecha ASC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var filas = await db.QueryAsync<(DateTime Fecha, int Total)>(sql, new { FkFuente = fkFuente, Dias = dias });

        return filas.ToDictionary(f => f.Fecha.ToString("yyyy-MM-dd"), f => f.Total);

    }



    public async Task<Dictionary<string, int>> ObtenerDescargasPorFormatoAsync(int fkFuente)

    {

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);

        var sql = $@"

            SELECT

                CASE UA.FK_Actividad

                    WHEN 1 THEN 'CSV'

                    WHEN 2 THEN 'JSON'

                    WHEN 3 THEN 'Imagen'

                    ELSE 'Otro'

                END AS Formato,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Actividad IN (1, 2, 3)

              AND UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 30 DAY)

            GROUP BY UA.FK_Actividad

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var filas = await db.QueryAsync<(string Formato, int Total)>(sql, new { FkFuente = fkFuente });

        return filas.ToDictionary(f => f.Formato, f => f.Total);

    }



    #region KPIs proveedor



    private async Task<List<Dictionary<string, object>>> ProveedorResumen(int fkFuente, Dictionary<string, object> parametros)

    {

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);
        var join = SqlJoinRepresentacionesFuente(incluirClones: true);
        var joinR2 = join.Replace("R.Id", "R2.Id", StringComparison.Ordinal);

        var sql = $@"
            SELECT
                COALESCE(SUM(CASE WHEN UA.FK_Actividad IN (1, 2, 3) THEN UA.Total ELSE 0 END), 0) AS Descargas,
                COALESCE(SUM(UA.Total), 0) AS Interacciones,
                COUNT(DISTINCT UA.FK_Usuario) AS UsuariosUnicos,
                (
                    SELECT COUNT(DISTINCT R2.Id)
                    FROM Representacion R2
                    {joinR2}
                    WHERE R2.Activa = 1
                ) AS RepresentacionesActivas
            FROM UsuarioActividad UA
            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1
            {join}
            WHERE UA.FK_Representacion IS NOT NULL
              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin
        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var fila = await db.QueryFirstAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin });

        return [MapearFila(fila)];

    }



    private async Task<List<Dictionary<string, object>>> ProveedorDescargasPorFormato(int fkFuente, Dictionary<string, object> parametros)

    {

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT

                CASE UA.FK_Actividad WHEN 1 THEN 'CSV' WHEN 2 THEN 'JSON' WHEN 3 THEN 'Imagen' ELSE 'Otro' END AS Formato,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Actividad IN (1, 2, 3) AND UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY UA.FK_Actividad

            ORDER BY Total DESC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorActividadPorMes(int fkFuente, Dictionary<string, object> parametros)

    {

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT DATE_FORMAT(UA.LastUpdate, '%Y-%m') AS Mes, A.Nombre AS Actividad, SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            INNER JOIN Actividad A ON A.Id = UA.FK_Actividad

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m'), A.Nombre

            ORDER BY Mes ASC, Total DESC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorActividadPorTipo(int fkFuente, Dictionary<string, object> parametros)

    {

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT A.Nombre AS Actividad, SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            INNER JOIN Actividad A ON A.Id = UA.FK_Actividad

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY A.Nombre

            ORDER BY Total DESC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorActividadDiaria(int fkFuente, Dictionary<string, object> parametros)

    {

        int dias = parametros.ContainsKey("dias") ? Convert.ToInt32(parametros["dias"]) : 7;

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT DATE(UA.LastUpdate) AS Fecha, SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL @Dias DAY)

            GROUP BY DATE(UA.LastUpdate)

            ORDER BY Fecha ASC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente, Dias = dias }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorUsuariosUnicosPorMes(int fkFuente, Dictionary<string, object> parametros)

    {

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT DATE_FORMAT(UA.LastUpdate, '%Y-%m') AS Mes, COUNT(DISTINCT UA.FK_Usuario) AS UsuariosUnicos

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m')

            HAVING UsuariosUnicos >= {MinimoKAnonimidad}

            ORDER BY Mes ASC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin }));

    }



    private async Task<(List<Dictionary<string, object>> Tabla, List<Dictionary<string, object>> Grafico)> ProveedorRepresentacionesMasUsadas(int fkFuente, Dictionary<string, object> parametros)

    {

        int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 20;

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sql = $@"

            SELECT

                COALESCE(NULLIF(TRIM(MAX(RD.Titulo)), ''), NULLIF(TRIM(R.TituloUrl), ''), CONCAT('Representación ', R.Id)) AS Titulo,

                R.TituloUrl,

                R.GUID,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            LEFT JOIN RepresentacionDisegno RD ON RD.FK_Representacion = R.Id

            {join}

            WHERE UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY R.Id, R.TituloUrl, R.GUID

            HAVING Total >= {MinimoKAnonimidad}

            ORDER BY Total DESC

            LIMIT @Limite

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var filas = await db.QueryAsync(sql, new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin, Limite = limite });

        var tabla = MapearFilasRepresentacionTabla(filas);

        var grafico = tabla

            .Select(f => new Dictionary<string, object>

            {

                ["Titulo"] = f["Titulo"],

                ["Total"] = f["Total"]

            })

            .ToList();

        return (tabla, grafico);

    }



    private async Task<(List<Dictionary<string, object>> Tabla, List<Dictionary<string, object>> Grafico)> ProveedorDescargasTop(int fkFuente, Dictionary<string, object> parametros)

    {

        int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 20;

        var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);



        var sqlGrafico = $@"

            SELECT

                DATE_FORMAT(UA.LastUpdate, '%Y-%m') AS Mes,

                CASE UA.FK_Actividad WHEN 1 THEN 'CSV' WHEN 2 THEN 'JSON' WHEN 3 THEN 'Imagen' ELSE 'Otro' END AS Formato,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            {join}

            WHERE UA.FK_Actividad IN (1, 2, 3) AND UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m'), UA.FK_Actividad

            ORDER BY Mes ASC, Formato ASC

        ;";



        var sqlTabla = $@"

            SELECT

                COALESCE(NULLIF(TRIM(MAX(RD.Titulo)), ''), NULLIF(TRIM(R.TituloUrl), ''), CONCAT('Representación ', R.Id)) AS Titulo,

                R.TituloUrl,

                R.GUID,

                SUM(UA.Total) AS Total

            FROM UsuarioActividad UA

            INNER JOIN Representacion R ON R.Id = UA.FK_Representacion AND R.Activa = 1

            LEFT JOIN RepresentacionDisegno RD ON RD.FK_Representacion = R.Id

            {join}

            WHERE UA.FK_Actividad IN (1, 2, 3) AND UA.FK_Representacion IS NOT NULL

              AND UA.LastUpdate >= @FechaInicio AND UA.LastUpdate <= @FechaFin

            GROUP BY R.Id, R.TituloUrl, R.GUID

            HAVING Total >= {MinimoKAnonimidad}

            ORDER BY Total DESC

            LIMIT @Limite

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        var parametrosSql = new { FkFuente = fkFuente, FechaInicio = fechaInicio, FechaFin = fechaFin, Limite = limite };

        var filasGrafico = await db.QueryAsync(sqlGrafico, parametrosSql);

        var filasTabla = await db.QueryAsync(sqlTabla, parametrosSql);



        return (MapearFilasRepresentacionTabla(filasTabla), MapearFilas(filasGrafico));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorVolumenDatos(int fkFuente)

    {

        const string sql = @"

            SELECT COUNT(*) AS TotalDatos

            FROM DatoValor

            WHERE FK_Fuente = @FkFuente

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorSeriesEnFuente(int fkFuente)

    {

        const string sql = @"

            SELECT COUNT(*) AS TotalSeries

            FROM SerieOriginal

            WHERE FK_Fuente = @FkFuente

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente }));

    }



    private async Task<List<Dictionary<string, object>>> ProveedorRepresentacionesActivas(int fkFuente)

    {

        var join = SqlJoinRepresentacionesFuente(incluirClones: true);

        var sql = $@"

            SELECT

                CASE WHEN R.FK_RepresentacionDeLaCualSoyClon IS NULL THEN 'Original' ELSE 'Clon' END AS Tipo,

                COUNT(*) AS Total

            FROM Representacion R

            {join}

            WHERE R.Activa = 1

            GROUP BY CASE WHEN R.FK_RepresentacionDeLaCualSoyClon IS NULL THEN 'Original' ELSE 'Clon' END

            ORDER BY Total DESC

        ;";



        using IDbConnection db = ObtenerConexionLectura();

        return MapearFilas(await db.QueryAsync(sql, new { FkFuente = fkFuente }));

    }



    #endregion



    #region Helpers



    /// <summary>

    /// Representaciones con métricas de la fuente (una sola pasada por Metrica; evita EXISTS correlacionados).

    /// </summary>

    private static string SubqueryRepresentacionesFuente(bool incluirClones) => incluirClones

        ? @"

            SELECT DISTINCT M.FK_Representacion AS Id

            FROM Metrica M

            WHERE M.FK_Fuente = @FkFuente

            UNION

            SELECT Rc.Id

            FROM Representacion Rc

            INNER JOIN Metrica M ON M.FK_Fuente = @FkFuente

              AND M.FK_Representacion = Rc.FK_RepresentacionDeLaCualSoyClon

            WHERE Rc.FK_RepresentacionDeLaCualSoyClon IS NOT NULL"

        : @"

            SELECT DISTINCT M.FK_Representacion AS Id

            FROM Metrica M

            WHERE M.FK_Fuente = @FkFuente";



    private static string SqlJoinRepresentacionesFuente(bool incluirClones) =>

        $"INNER JOIN ({SubqueryRepresentacionesFuente(incluirClones)}) RepFuente ON RepFuente.Id = R.Id";



    private List<Dictionary<string, object>> MapearFilasRepresentacionTabla(IEnumerable<dynamic> filas)

    {

        return filas.Select(fila =>

        {

            var dict = (IDictionary<string, object>)fila;

            var titulo = dict.TryGetValue("Titulo", out var t) ? t?.ToString() : null;

            var tituloUrl = dict.TryGetValue("TituloUrl", out var tu) ? tu?.ToString() : null;

            var guid = dict.TryGetValue("GUID", out var g) && g != null

                ? g is Guid guidVal ? guidVal : Guid.Parse(g.ToString()!)

                : Guid.Empty;



            return new Dictionary<string, object>

            {

                ["Titulo"] = string.IsNullOrWhiteSpace(titulo) ? "—" : titulo!,

                ["Url"] = ConstruirUrlRepresentacion(tituloUrl, guid),

                ["Total"] = dict.TryGetValue("Total", out var total) ? total : 0

            };

        }).ToList();

    }



    private string ConstruirUrlRepresentacion(string? tituloUrl, Guid guid)

    {

        if (string.IsNullOrWhiteSpace(tituloUrl))

        {

            return guid == Guid.Empty ? "—" : $"{_urlBasePublicacionEpData}/representacion/{guid:D}";

        }



        var slug = tituloUrl.Trim().Trim('/');

        return $"{_urlBasePublicacionEpData}/{slug}/{guid:D}";

    }



    private static (DateTime FechaInicio, DateTime FechaFin) ResolverRangoFechasUltimos12Meses(Dictionary<string, object> parametros)

    {

        var fechaFin = parametros.ContainsKey("fecha_fin") && parametros["fecha_fin"] != null

            ? Convert.ToDateTime(parametros["fecha_fin"])

            : DateTime.Now;

        var fechaInicio = parametros.ContainsKey("fecha_inicio") && parametros["fecha_inicio"] != null

            ? Convert.ToDateTime(parametros["fecha_inicio"])

            : fechaFin.AddMonths(-12);

        return (fechaInicio, fechaFin);

    }



    private static List<Dictionary<string, object>> MapearFilas(IEnumerable<dynamic> filas) =>

        filas.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();



    private static Dictionary<string, object> MapearFila(dynamic fila) =>

        ((IDictionary<string, object>)fila).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value);



    private static Kpi CrearKpi(string id, string nombre, string descripcion, List<ParametroKpi> parametros) =>

        new() { Id = id, Nombre = nombre, Descripcion = descripcion, Categoria = "Mi fuente", Parametros = parametros };



    private static List<ParametroKpi> ParametrosRangoFechas() =>

    [

        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio (por defecto: hace 12 meses)" },

        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin (por defecto: hoy)" }

    ];



    private static List<ParametroKpi> ParametrosDias() =>

    [

        new ParametroKpi { Nombre = "dias", Tipo = "numero", Requerido = false, ValorPorDefecto = "7", Descripcion = "Días hacia atrás" }

    ];



    private static List<ParametroKpi> ParametrosTopRepresentaciones() =>

    [

        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "20", Descripcion = "Número máximo de resultados" },

        ..ParametrosRangoFechas()

    ];



    private static List<ParametroKpi> ParametrosTopDescargas() =>

    [

        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "20", Descripcion = "Número máximo de resultados en la tabla" },

        ..ParametrosRangoFechas()

    ];



    #endregion

}


