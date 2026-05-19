using Dapper;
using Microsoft.Extensions.Logging;
using Modelos.DTO.Metricas;
using Modelos.InyeccionDeDependencias;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;

namespace Datos
{
    public class RepositorioMetricas : BaseRepositorio, IRepositorioMetricas
    {
        private readonly ILogger<RepositorioMetricas> _logger;

        public RepositorioMetricas(Aurora aurora, ILogger<RepositorioMetricas> logger) : base(aurora)
        {
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de todos los KPIs disponibles con sus parámetros
        /// </summary>
        public List<Kpi> ObtenerKpisDisponibles()
        {
            return new List<Kpi>
            {
                // ===== CATEGORÍA: ACTIVIDAD DE USUARIOS =====
                new Kpi
                {
                    Id = "actividad-por-mes",
                    Nombre = "Actividad por mes y tipo",
                    Descripcion = "Actividad agregada mensualmente por tipo de actividad",
                    Categoria = "Actividad",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin" }
                    }
                },
                new Kpi
                {
                    Id = "actividad-total",
                    Nombre = "Total de actividades",
                    Descripcion = "Suma total de todas las actividades registradas",
                    Categoria = "Actividad",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "actividad-por-usuario",
                    Nombre = "Actividad por usuario",
                    Descripcion = "Actividad agregada por usuario",
                    Categoria = "Actividad",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "50", Descripcion = "Número máximo de resultados" },
                        new ParametroKpi { Nombre = "orden", Tipo = "texto", Requerido = false, ValorPorDefecto = "desc", Descripcion = "Orden (asc/desc)" }
                    }
                },
                new Kpi
                {
                    Id = "actividad-usuario-mensual",
                    Nombre = "Actividad mensual por usuario",
                    Descripcion = "Actividad desagregada por usuario y mes",
                    Categoria = "Actividad",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "usuario_email", Tipo = "texto", Requerido = true, Descripcion = "Email del usuario" },
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin" }
                    }
                },

                // ===== CATEGORÍA: USUARIOS =====
                new Kpi
                {
                    Id = "usuarios-por-tipo",
                    Nombre = "Usuarios por tipo",
                    Descripcion = "Usuarios activos agrupados por tipo",
                    Categoria = "Usuarios",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "usuarios-activos-mes",
                    Nombre = "Usuarios activos último mes",
                    Descripcion = "Usuarios con actividad en los últimos 30 días",
                    Categoria = "Usuarios",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "10", Descripcion = "Número máximo de resultados" }
                    }
                },
                new Kpi
                {
                    Id = "tokens-activos",
                    Nombre = "Tokens API activos",
                    Descripcion = "Usuarios con refresh tokens activos",
                    Categoria = "Usuarios",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "roles-por-usuario-tipo",
                    Nombre = "Distribución de roles por tipo de usuario",
                    Descripcion = "Roles asignados agrupados por tipo de usuario",
                    Categoria = "Usuarios",
                    Parametros = new List<ParametroKpi>()
                },

                // ===== CATEGORÍA: DESCARGAS =====
                new Kpi
                {
                    Id = "descargas-csv",
                    Nombre = "Top descargas CSV",
                    Descripcion = "Representaciones más descargadas en formato CSV",
                    Categoria = "Descargas",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "10", Descripcion = "Número máximo de resultados" }
                    }
                },
                new Kpi
                {
                    Id = "descargas-json",
                    Nombre = "Top descargas JSON",
                    Descripcion = "Representaciones más descargadas en formato JSON",
                    Categoria = "Descargas",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "10", Descripcion = "Número máximo de resultados" }
                    }
                },
                new Kpi
                {
                    Id = "descargas-imagenes",
                    Nombre = "Top descargas de imágenes",
                    Descripcion = "Representaciones más descargadas como imagen",
                    Categoria = "Descargas",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "10", Descripcion = "Número máximo de resultados" }
                    }
                },

                // ===== CATEGORÍA: DATOS Y CONTENIDO =====
                new Kpi
                {
                    Id = "total-datos",
                    Nombre = "Total de registros de datos",
                    Descripcion = "Cantidad total de datos almacenados en DatoValor",
                    Categoria = "Datos",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "fuentes-por-tipo",
                    Nombre = "Fuentes por tipo",
                    Descripcion = "Distribución de fuentes agrupadas por tipo",
                    Categoria = "Datos",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "fuentes-totales",
                    Nombre = "Total de fuentes",
                    Descripcion = "Cantidad total de fuentes en el sistema",
                    Categoria = "Datos",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "representaciones-estadisticas",
                    Nombre = "Estadísticas de representaciones",
                    Descripcion = "Originales vs clones de representaciones",
                    Categoria = "Contenido",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "plantillas-estadisticas",
                    Nombre = "Estadísticas de plantillas",
                    Descripcion = "Plantillas estáticas vs dinámicas",
                    Categoria = "Contenido",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "representaciones-mas-vistas",
                    Nombre = "Representaciones más vistas",
                    Descripcion = "Top de representaciones por visualizaciones (no descargas)",
                    Categoria = "Contenido",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "50", Descripcion = "Número máximo de resultados" },
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin" }
                    }
                },

                // ===== CATEGORÍA: SUSCRIPCIONES =====
                new Kpi
                {
                    Id = "suscripciones-usuarios-vigentes",
                    Nombre = "Usuarios con suscripción vigente",
                    Descripcion = "Usuarios activos con al menos una suscripción vigente (active o trialing)",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "suscripciones-por-estado",
                    Nombre = "Suscripciones por estado",
                    Descripcion = "Histórico de filas en Suscripcion agrupadas por estado de negocio",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio (FechaInicio de la suscripción)" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin (FechaInicio de la suscripción)" }
                    }
                },
                new Kpi
                {
                    Id = "suscripciones-por-plan",
                    Nombre = "Suscripciones por plan",
                    Descripcion = "Distribución de filas de suscripción por tipo de plan (SuscripcionTipo.Id)",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio (FechaInicio de la suscripción)" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin (FechaInicio de la suscripción)" }
                    }
                },
                new Kpi
                {
                    Id = "suscripciones-altas-por-mes",
                    Nombre = "Altas de suscripción por mes",
                    Descripcion = "Nuevas suscripciones por mes según FechaInicio",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio (por defecto: hace 12 meses)" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin (por defecto: hoy)" }
                    }
                },
                new Kpi
                {
                    Id = "suscripciones-bajas-por-mes",
                    Nombre = "Bajas de suscripción por mes",
                    Descripcion = "Suscripciones con FechaFin por mes",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio (por defecto: hace 12 meses)" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin (por defecto: hoy)" }
                    }
                },
                new Kpi
                {
                    Id = "suscripciones-vigentes-con-fin-programado",
                    Nombre = "Vigentes con cancelación programada",
                    Descripcion = "Suscripciones vigentes con FechaFin futura (baja programada)",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>()
                },
                new Kpi
                {
                    Id = "suscripciones-usuarios-por-origen-acceso",
                    Nombre = "Usuarios por origen de acceso",
                    Descripcion = "Usuarios activos con historial de suscripción: interna EP, pago Stripe vigente o sin suscripción activa",
                    Categoria = "Suscripciones",
                    Parametros = new List<ParametroKpi>()
                },

                // ===== CATEGORÍA: API Y ERRORES =====
                new Kpi
                {
                    Id = "actividad-api-usuario",
                    Nombre = "Actividad API por usuario",
                    Descripcion = "Accesos a la API agrupados por usuario",
                    Categoria = "API",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "fecha_inicio", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de inicio" },
                        new ParametroKpi { Nombre = "fecha_fin", Tipo = "fecha", Requerido = false, Descripcion = "Fecha de fin" }
                    }
                },
                new Kpi
                {
                    Id = "errores-recientes",
                    Nombre = "Errores recientes",
                    Descripcion = "Logs de errores de las últimas 24 horas",
                    Categoria = "Sistema",
                    Parametros = new List<ParametroKpi>
                    {
                        new ParametroKpi { Nombre = "limite", Tipo = "numero", Requerido = false, ValorPorDefecto = "100", Descripcion = "Número máximo de resultados" }
                    }
                }
            };
        }

        /// <summary>
        /// Ejecuta un KPI específico con los parámetros proporcionados
        /// </summary>
        public async Task<ResultadoKpi> EjecutarKpiAsync(string kpiId, Dictionary<string, object> parametros)
        {
            var stopwatch = Stopwatch.StartNew();
            var kpiInfo = ObtenerKpisDisponibles().FirstOrDefault(k => k.Id == kpiId);

            if (kpiInfo == null)
            {
                throw new ArgumentException($"KPI '{kpiId}' no encontrado");
            }

            try
            {
                List<Dictionary<string, object>> datos = kpiId switch
                {
                    // Actividad
                    "actividad-por-mes" => await ActividadPorMes(parametros),
                    "actividad-total" => await ActividadTotal(),
                    "actividad-por-usuario" => await ActividadPorUsuario(parametros),
                    "actividad-usuario-mensual" => await ActividadUsuarioMensual(parametros),

                    // Usuarios
                    "usuarios-por-tipo" => await UsuariosPorTipo(),
                    "usuarios-activos-mes" => await UsuariosActivosMes(parametros),
                    "tokens-activos" => await TokensActivos(),
                    "roles-por-usuario-tipo" => await RolesPorUsuarioTipo(),

                    // Descargas
                    "descargas-csv" => await DescargasCSV(parametros),
                    "descargas-json" => await DescargasJSON(parametros),
                    "descargas-imagenes" => await DescargasImagenes(parametros),

                    // Datos
                    "total-datos" => await TotalDatos(),
                    "fuentes-por-tipo" => await FuentesPorTipo(),
                    "fuentes-totales" => await FuentesTotales(),

                    // Contenido
                    "representaciones-estadisticas" => await RepresentacionesEstadisticas(),
                    "plantillas-estadisticas" => await PlantillasEstadisticas(),
                    "representaciones-mas-vistas" => await RepresentacionesMasVistas(parametros),

                    // Suscripciones
                    "suscripciones-usuarios-vigentes" => await SuscripcionesUsuariosVigentes(),
                    "suscripciones-por-estado" => await SuscripcionesPorEstado(parametros),
                    "suscripciones-por-plan" => await SuscripcionesPorPlan(parametros),
                    "suscripciones-altas-por-mes" => await SuscripcionesAltasPorMes(parametros),
                    "suscripciones-bajas-por-mes" => await SuscripcionesBajasPorMes(parametros),
                    "suscripciones-vigentes-con-fin-programado" => await SuscripcionesVigentesConFinProgramado(),
                    "suscripciones-usuarios-por-origen-acceso" => await SuscripcionesUsuariosPorOrigenAcceso(),

                    // API y Errores
                    "actividad-api-usuario" => await ActividadApiUsuario(parametros),
                    "errores-recientes" => await ErroresRecientes(parametros),

                    _ => throw new ArgumentException($"KPI '{kpiId}' no implementado")
                };

                stopwatch.Stop();

                return new ResultadoKpi
                {
                    KpiId = kpiId,
                    Nombre = kpiInfo.Nombre,
                    Parametros = parametros,
                    TimestampGeneracion = DateTime.UtcNow,
                    TotalRegistros = datos.Count,
                    Datos = datos,
                    DesdCache = false,
                    DuracionMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar KPI {KpiId}", kpiId);
                throw;
            }
        }

        // ===========================
        // MÉTODOS PRIVADOS POR KPI
        // ===========================

        #region Actividad

        private async Task<List<Dictionary<string, object>>> ActividadPorMes(Dictionary<string, object> parametros)
        {
            DateTime? fechaInicio = parametros.ContainsKey("fecha_inicio") ? Convert.ToDateTime(parametros["fecha_inicio"]) : null;
            DateTime? fechaFin = parametros.ContainsKey("fecha_fin") ? Convert.ToDateTime(parametros["fecha_fin"]) : null;

            string sql = @"
                SELECT
                    DATE_FORMAT(UA.LastUpdate, '%Y-%m') as Fecha,
                    A.Nombre as Actividad,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE (@FechaInicio IS NULL OR UA.LastUpdate >= @FechaInicio)
                  AND (@FechaFin IS NULL OR UA.LastUpdate <= @FechaFin)
                GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m'), A.Nombre
                ORDER BY Fecha DESC, Total DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> ActividadTotal()
        {
            string sql = @"
                SELECT
                    A.Nombre as Actividad,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                GROUP BY A.Nombre
                ORDER BY Total DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> ActividadPorUsuario(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 50;
            string orden = parametros.ContainsKey("orden") ? parametros["orden"].ToString() ?? "desc" : "desc";

            string sql = $@"
                SELECT
                    U.Email as Usuario,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                INNER JOIN Usuario U ON UA.FK_Usuario = U.Id
                GROUP BY U.Id, U.Email
                ORDER BY Total {(orden.ToLower() == "asc" ? "ASC" : "DESC")}
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> ActividadUsuarioMensual(Dictionary<string, object> parametros)
        {
            if (!parametros.ContainsKey("usuario_email"))
            {
                throw new ArgumentException("El parámetro 'usuario_email' es requerido");
            }

            string usuarioEmail = parametros["usuario_email"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(usuarioEmail))
            {
                throw new ArgumentException("El email del usuario no puede estar vacío");
            }

            DateTime? fechaInicio = parametros.ContainsKey("fecha_inicio") ? Convert.ToDateTime(parametros["fecha_inicio"]) : null;
            DateTime? fechaFin = parametros.ContainsKey("fecha_fin") ? Convert.ToDateTime(parametros["fecha_fin"]) : null;

            using (IDbConnection db = ObtenerConexionLectura())
            {
                // Primero buscar el ID del usuario por email
                string sqlBuscarUsuario = @"
                    SELECT Id
                    FROM Usuario
                    WHERE UPPER(Email) = UPPER(@Email)
                    LIMIT 1";

                int? usuarioId = await db.QueryFirstOrDefaultAsync<int?>(sqlBuscarUsuario, new { Email = usuarioEmail });

                if (!usuarioId.HasValue)
                {
                    throw new ArgumentException($"No se encontró ningún usuario con el email '{usuarioEmail}'");
                }

                // Ejecutar la consulta de actividad con el ID encontrado
                string sql = @"
                    SELECT
                        DATE_FORMAT(UA.LastUpdate, '%Y-%m') as Fecha,
                        A.Nombre as Actividad,
                        COUNT(*) as Total
                    FROM UsuarioActividad UA
                    INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                    WHERE UA.FK_Usuario = @UsuarioId
                      AND (@FechaInicio IS NULL OR UA.LastUpdate >= @FechaInicio)
                      AND (@FechaFin IS NULL OR UA.LastUpdate <= @FechaFin)
                    GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m'), A.Nombre
                    ORDER BY Fecha DESC, Total DESC";

                var resultado = await db.QueryAsync(sql, new { UsuarioId = usuarioId.Value, FechaInicio = fechaInicio, FechaFin = fechaFin });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        #endregion

        #region Usuarios

        private async Task<List<Dictionary<string, object>>> UsuariosPorTipo()
        {
            string sql = @"
                SELECT
                    UT.Nombre as TipoUsuario,
                    COUNT(*) as Total
                FROM Usuario U
                INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                WHERE U.Activo = 1
                GROUP BY UT.Nombre
                ORDER BY Total DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> UsuariosActivosMes(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 10;

            string sql = @"
                SELECT
                    U.Email as Usuario,
                    U.Nombre as NombreUsuario,
                    MAX(UA.LastUpdate) as UltimaActividad,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                INNER JOIN Usuario U ON UA.FK_Usuario = U.Id
                WHERE UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 30 DAY)
                GROUP BY U.Id, U.Email, U.Nombre
                ORDER BY Total DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> TokensActivos()
        {
            string sql = @"
                SELECT
                    U.Email as Usuario,
                    U.Nombre as NombreUsuario,
                    UT.Nombre as TipoUsuario,
                    CASE
                        WHEN U.RefreshToken1EpDataApi IS NOT NULL AND U.RefreshToken2EpDataApi IS NOT NULL THEN 'Ambos'
                        WHEN U.RefreshToken1EpDataApi IS NOT NULL THEN 'Token1'
                        WHEN U.RefreshToken2EpDataApi IS NOT NULL THEN 'Token2'
                    END as TokensActivos
                FROM Usuario U
                INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                WHERE (U.RefreshToken1EpDataApi IS NOT NULL OR U.RefreshToken2EpDataApi IS NOT NULL)
                  AND U.Activo = 1
                ORDER BY U.Email";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> RolesPorUsuarioTipo()
        {
            string sql = @"
                SELECT
                    UT.Nombre as TipoUsuario,
                    R.Nombre as Rol,
                    COUNT(DISTINCT U.Id) as TotalUsuarios
                FROM Usuario U
                INNER JOIN UsuarioTipo UT ON U.FK_UsuarioTipo = UT.Id
                INNER JOIN UsuarioRol UR ON U.Id = UR.FK_Usuario
                INNER JOIN Rol R ON UR.FK_Rol = R.Id
                WHERE U.Activo = 1
                GROUP BY UT.Nombre, R.Nombre
                ORDER BY UT.Nombre, TotalUsuarios DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        #endregion

        #region Descargas

        private async Task<List<Dictionary<string, object>>> DescargasCSV(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 10;

            string sql = @"
                SELECT
                    COALESCE(RD.Titulo, CONCAT('Representación ', R.Id)) as Representacion,
                    R.Id as RepresentacionId,
                    COUNT(*) as TotalDescargas
                FROM UsuarioActividad UA
                INNER JOIN Representacion R ON UA.FK_Representacion = R.Id
                LEFT JOIN RepresentacionDisegno RD ON R.Id = RD.FK_Representacion
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE A.Nombre = 'Descargado CSV de datos de representación'
                GROUP BY R.Id, RD.Titulo
                ORDER BY TotalDescargas DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> DescargasJSON(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 10;

            string sql = @"
                SELECT
                    COALESCE(RD.Titulo, CONCAT('Representación ', R.Id)) as Representacion,
                    R.Id as RepresentacionId,
                    COUNT(*) as TotalDescargas
                FROM UsuarioActividad UA
                INNER JOIN Representacion R ON UA.FK_Representacion = R.Id
                LEFT JOIN RepresentacionDisegno RD ON R.Id = RD.FK_Representacion
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE A.Nombre = 'Descargado JSON de datos de representación'
                GROUP BY R.Id, RD.Titulo
                ORDER BY TotalDescargas DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> DescargasImagenes(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 10;

            string sql = @"
                SELECT
                    COALESCE(RD.Titulo, CONCAT('Representación ', R.Id)) as Representacion,
                    R.Id as RepresentacionId,
                    COUNT(*) as TotalDescargas
                FROM UsuarioActividad UA
                INNER JOIN Representacion R ON UA.FK_Representacion = R.Id
                LEFT JOIN RepresentacionDisegno RD ON R.Id = RD.FK_Representacion
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE A.Nombre = 'Descargada imagen de representación'
                GROUP BY R.Id, RD.Titulo
                ORDER BY TotalDescargas DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        #endregion

        #region Datos y Contenido

        private async Task<List<Dictionary<string, object>>> TotalDatos()
        {
            string sql = @"
                SELECT COUNT(*) as Total
                FROM DatoValor";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> FuentesPorTipo()
        {
            string sql = @"
                SELECT
                    FT.Nombre as TipoFuente,
                    COUNT(*) as Total
                FROM Fuente F
                INNER JOIN FuenteTipo FT ON F.FK_FuenteTipo = FT.Id
                GROUP BY FT.Nombre
                ORDER BY Total DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> FuentesTotales()
        {
            string sql = @"
                SELECT COUNT(*) as Total
                FROM Fuente";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> RepresentacionesEstadisticas()
        {
            string sql = @"
                SELECT
                    CASE
                        WHEN FK_RepresentacionDeLaCualSoyClon IS NULL THEN 'Originales'
                        ELSE 'Clones'
                    END as Tipo,
                    COUNT(*) as Total
                FROM Representacion
                GROUP BY CASE WHEN FK_RepresentacionDeLaCualSoyClon IS NULL THEN 'Originales' ELSE 'Clones' END";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> PlantillasEstadisticas()
        {
            string sql = @"
                SELECT
                    CASE
                        WHEN FK_PlantillaWebPadre IS NOT NULL THEN 'Dinámicas'
                        ELSE 'Estáticas'
                    END as Tipo,
                    COUNT(*) as Total
                FROM PlantillaWeb
                GROUP BY CASE WHEN FK_PlantillaWebPadre IS NOT NULL THEN 'Dinámicas' ELSE 'Estáticas' END";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql);
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> RepresentacionesMasVistas(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 50;
            DateTime? fechaInicio = parametros.ContainsKey("fecha_inicio") ? Convert.ToDateTime(parametros["fecha_inicio"]) : null;
            DateTime? fechaFin = parametros.ContainsKey("fecha_fin") ? Convert.ToDateTime(parametros["fecha_fin"]) : null;

            string sql = @"
                SELECT
                    COALESCE(RD.Titulo, CONCAT('Representación ', R.Id)) as Representacion,
                    R.Id as RepresentacionId,
                    COUNT(*) as TotalVistas
                FROM UsuarioActividad UA
                INNER JOIN Representacion R ON UA.FK_Representacion = R.Id
                LEFT JOIN RepresentacionDisegno RD ON R.Id = RD.FK_Representacion
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE (@FechaInicio IS NULL OR UA.LastUpdate >= @FechaInicio)
                  AND (@FechaFin IS NULL OR UA.LastUpdate <= @FechaFin)
                GROUP BY R.Id, RD.Titulo
                ORDER BY TotalVistas DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite, FechaInicio = fechaInicio, FechaFin = fechaFin });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        #endregion

        #region Suscripciones

        private const string SqlEstadoAgrupado = @"
                CASE
                    WHEN S.Estado = 'active' THEN 'Activa'
                    WHEN S.Estado = 'trialing' THEN 'En prueba'
                    WHEN S.Estado IN ('canceled', 'incomplete_expired') THEN 'Cancelada'
                    WHEN S.Estado IN ('past_due', 'unpaid') THEN 'Impago'
                    ELSE 'Otra'
                END";

        private const string SqlSuscripcionVigente = @"
                S.Estado IN ('active', 'trialing')
                AND (S.FechaFin IS NULL OR S.FechaFin >= UTC_TIMESTAMP())";

        private static (DateTime? FechaInicio, DateTime? FechaFin) ResolverRangoFechasOpcional(Dictionary<string, object> parametros)
        {
            DateTime? fechaInicio = parametros.ContainsKey("fecha_inicio") && parametros["fecha_inicio"] != null
                ? Convert.ToDateTime(parametros["fecha_inicio"])
                : null;
            DateTime? fechaFin = parametros.ContainsKey("fecha_fin") && parametros["fecha_fin"] != null
                ? Convert.ToDateTime(parametros["fecha_fin"])
                : null;
            return (fechaInicio, fechaFin);
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

        private static List<Dictionary<string, object>> MapearFilas(IEnumerable<dynamic> filas)
        {
            return filas
                .Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value))
                .ToList();
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesUsuariosVigentes()
        {
            string sql = $@"
                SELECT COUNT(DISTINCT S.FK_Usuario) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                WHERE {SqlSuscripcionVigente}";

            using IDbConnection db = ObtenerConexionLectura();
            var total = await db.QueryFirstOrDefaultAsync<long?>(sql);
            return
            [
                new Dictionary<string, object> { ["Total"] = total ?? 0L }
            ];
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesPorEstado(Dictionary<string, object> parametros)
        {
            var (fechaInicio, fechaFin) = ResolverRangoFechasOpcional(parametros);

            string sql = $@"
                SELECT
                    {SqlEstadoAgrupado} AS EstadoAgrupado,
                    COUNT(*) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                WHERE (@FechaInicio IS NULL OR S.FechaInicio >= @FechaInicio)
                  AND (@FechaFin IS NULL OR S.FechaInicio <= @FechaFin)
                GROUP BY EstadoAgrupado
                ORDER BY Total DESC";

            using IDbConnection db = ObtenerConexionLectura();
            var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
            return MapearFilas(resultado);
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesPorPlan(Dictionary<string, object> parametros)
        {
            var (fechaInicio, fechaFin) = ResolverRangoFechasOpcional(parametros);

            string sql = @"
                SELECT
                    ST.Id AS PlanId,
                    ST.Nombre AS PlanNombre,
                    COUNT(*) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                INNER JOIN SuscripcionTipo ST ON ST.Id = S.FK_SuscripcionTipo
                WHERE (@FechaInicio IS NULL OR S.FechaInicio >= @FechaInicio)
                  AND (@FechaFin IS NULL OR S.FechaInicio <= @FechaFin)
                GROUP BY ST.Id, ST.Nombre
                ORDER BY Total DESC, PlanId ASC";

            using IDbConnection db = ObtenerConexionLectura();
            var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
            return MapearFilas(resultado);
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesAltasPorMes(Dictionary<string, object> parametros)
        {
            var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

            string sql = @"
                SELECT
                    DATE_FORMAT(S.FechaInicio, '%Y-%m') AS Mes,
                    COUNT(*) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                WHERE S.FechaInicio >= @FechaInicio
                  AND S.FechaInicio <= @FechaFin
                GROUP BY DATE_FORMAT(S.FechaInicio, '%Y-%m')
                ORDER BY Mes ASC";

            using IDbConnection db = ObtenerConexionLectura();
            var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
            return MapearFilas(resultado);
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesBajasPorMes(Dictionary<string, object> parametros)
        {
            var (fechaInicio, fechaFin) = ResolverRangoFechasUltimos12Meses(parametros);

            string sql = @"
                SELECT
                    DATE_FORMAT(S.FechaFin, '%Y-%m') AS Mes,
                    COUNT(*) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                WHERE S.FechaFin IS NOT NULL
                  AND S.FechaFin >= @FechaInicio
                  AND S.FechaFin <= @FechaFin
                GROUP BY DATE_FORMAT(S.FechaFin, '%Y-%m')
                ORDER BY Mes ASC";

            using IDbConnection db = ObtenerConexionLectura();
            var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
            return MapearFilas(resultado);
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesVigentesConFinProgramado()
        {
            string sql = $@"
                SELECT COUNT(*) AS Total
                FROM Suscripcion S
                INNER JOIN Usuario U ON U.Id = S.FK_Usuario AND U.Activo = 1
                WHERE {SqlSuscripcionVigente}
                  AND S.FechaFin IS NOT NULL
                  AND S.FechaFin > UTC_TIMESTAMP()";

            using IDbConnection db = ObtenerConexionLectura();
            var total = await db.QueryFirstOrDefaultAsync<long?>(sql);
            return
            [
                new Dictionary<string, object> { ["Total"] = total ?? 0L }
            ];
        }

        private async Task<List<Dictionary<string, object>>> SuscripcionesUsuariosPorOrigenAcceso()
        {
            string sql = $@"
                SELECT
                    Clasificados.OrigenAcceso,
                    COUNT(*) AS Total
                FROM (
                    SELECT
                        CASE
                            WHEN U.FK_UsuarioTipo IN (2, 3, 4, 5, 6) THEN 'Interna'
                            WHEN EXISTS (
                                SELECT 1
                                FROM Suscripcion S
                                WHERE S.FK_Usuario = U.Id
                                  AND {SqlSuscripcionVigente}
                            ) THEN 'Pago'
                            ELSE 'Sin suscripción activa'
                        END AS OrigenAcceso
                    FROM Usuario U
                    WHERE U.Activo = 1
                      AND EXISTS (
                          SELECT 1
                          FROM Suscripcion S0
                          WHERE S0.FK_Usuario = U.Id
                      )
                ) AS Clasificados
                GROUP BY Clasificados.OrigenAcceso
                ORDER BY Total DESC";

            using IDbConnection db = ObtenerConexionLectura();
            var resultado = await db.QueryAsync(sql);
            return MapearFilas(resultado);
        }

        #endregion

        #region API y Errores

        private async Task<List<Dictionary<string, object>>> ActividadApiUsuario(Dictionary<string, object> parametros)
        {
            DateTime? fechaInicio = parametros.ContainsKey("fecha_inicio") ? Convert.ToDateTime(parametros["fecha_inicio"]) : null;
            DateTime? fechaFin = parametros.ContainsKey("fecha_fin") ? Convert.ToDateTime(parametros["fecha_fin"]) : null;

            string sql = @"
                SELECT
                    U.Email as Usuario,
                    U.Nombre as NombreUsuario,
                    COUNT(*) as TotalAccesos,
                    MAX(UA.LastUpdate) as UltimoAcceso
                FROM UsuarioActividad UA
                INNER JOIN Usuario U ON UA.FK_Usuario = U.Id
                INNER JOIN Actividad A ON UA.FK_Actividad = A.Id
                WHERE A.Nombre LIKE '%API%'
                  AND (@FechaInicio IS NULL OR UA.LastUpdate >= @FechaInicio)
                  AND (@FechaFin IS NULL OR UA.LastUpdate <= @FechaFin)
                GROUP BY U.Id, U.Email, U.Nombre
                ORDER BY TotalAccesos DESC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { FechaInicio = fechaInicio, FechaFin = fechaFin });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        private async Task<List<Dictionary<string, object>>> ErroresRecientes(Dictionary<string, object> parametros)
        {
            int limite = parametros.ContainsKey("limite") ? Convert.ToInt32(parametros["limite"]) : 100;

            // Consulta actualizada para usar la nueva estructura de tablas de auditoría
            // Los errores se identifican porque la descripción contiene " - Error" o un mensaje de error
            string sql = @"
                SELECT
                    A.LastUpdate as FechaHora,
                    AC.Nombre as Modulo,
                    AA.Nombre as Accion,
                    A.Descripcion as MensajeError,
                    U.Email as UsuarioEmail,
                    U.Nombre as UsuarioNombre
                FROM Auditoria A
                INNER JOIN Usuario U ON A.FK_Usuario = U.Id
                INNER JOIN AuditoriaAccion AA ON A.FK_AuditoriaAccion = AA.Id
                INNER JOIN AuditoriaCategoria AC ON AA.FK_AuditoriaCategoria = AC.Id
                WHERE A.Descripcion LIKE '% - Error%'
                  AND A.LastUpdate >= DATE_SUB(NOW(), INTERVAL 24 HOUR)
                ORDER BY A.LastUpdate DESC
                LIMIT @Limite";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync(sql, new { Limite = limite });
                return resultado.Select(r => ((IDictionary<string, object>)r).ToDictionary(k => k.Key, v => v.Value ?? (object)DBNull.Value)).ToList();
            }
        }

        #endregion

        #region Estadísticas del Dashboard

        /// <summary>
        /// Obtiene el número de usuarios activos en los últimos 30 días
        /// </summary>
        public async Task<int> ObtenerUsuariosActivosAsync()
        {
            string sql = @"
                SELECT COUNT(DISTINCT U.Id) as Total
                FROM UsuarioActividad UA
                INNER JOIN Usuario U ON UA.FK_Usuario = U.Id
                WHERE UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 30 DAY)
                  AND U.Activo = 1";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryFirstOrDefaultAsync<int?>(sql);
                return resultado ?? 0;
            }
        }

        /// <summary>
        /// Obtiene el número de actividades del mes actual
        /// </summary>
        public async Task<int> ObtenerActividadMensualAsync()
        {
            string sql = @"
                SELECT COUNT(*) as Total
                FROM UsuarioActividad UA
                WHERE DATE_FORMAT(UA.LastUpdate, '%Y-%m') = DATE_FORMAT(NOW(), '%Y-%m')";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryFirstOrDefaultAsync<int?>(sql);
                return resultado ?? 0;
            }
        }

        /// <summary>
        /// Obtiene el número de acciones realizadas hoy
        /// </summary>
        public async Task<int> ObtenerAccionesHoyAsync()
        {
            string sql = @"
                SELECT COUNT(*) as Total
                FROM UsuarioActividad UA
                WHERE DATE(UA.LastUpdate) = CURDATE()";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryFirstOrDefaultAsync<int?>(sql);
                return resultado ?? 0;
            }
        }

        /// <summary>
        /// Obtiene el total de usuarios activos en el sistema
        /// </summary>
        public async Task<int> ObtenerTotalUsuariosAsync()
        {
            string sql = @"
                SELECT COUNT(*) as Total
                FROM Usuario
                WHERE Activo = 1";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryFirstOrDefaultAsync<int?>(sql);
                return resultado ?? 0;
            }
        }

        /// <summary>
        /// Obtiene actividad mensual de los últimos 6 meses (agregada por mes)
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerActividadMensualUltimos6MesesAsync()
        {
            string sql = @"
                SELECT
                    DATE_FORMAT(UA.LastUpdate, '%Y-%m') as Fecha,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                WHERE UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
                GROUP BY DATE_FORMAT(UA.LastUpdate, '%Y-%m')
                ORDER BY Fecha ASC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync<(string Fecha, int Total)>(sql);
                return resultado.ToDictionary(r => r.Fecha, r => r.Total);
            }
        }

        /// <summary>
        /// Obtiene actividad diaria de los últimos 7 días (actividad real del sistema)
        /// </summary>
        public async Task<Dictionary<string, int>> ObtenerActividadDiariaUltimos7DiasAsync()
        {
            string sql = @"
                SELECT
                    DATE(UA.LastUpdate) as Fecha,
                    COUNT(*) as Total
                FROM UsuarioActividad UA
                WHERE UA.LastUpdate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                GROUP BY DATE(UA.LastUpdate)
                ORDER BY Fecha ASC";

            using (IDbConnection db = ObtenerConexionLectura())
            {
                var resultado = await db.QueryAsync<(DateTime Fecha, int Total)>(sql);
                return resultado.ToDictionary(
                    r => r.Fecha.ToString("yyyy-MM-dd"),
                    r => r.Total
                );
            }
        }

        #endregion
    }
}
