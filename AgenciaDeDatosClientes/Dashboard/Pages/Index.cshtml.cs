using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Modelos.Dashboard;
using Modelos.InyeccionDeDependencias;
using Negocio;
using Nuget.Helper;
using NugetPortable.Cache;
using NugetPortable.Herramientas.CDN;
using NugetPortable.SQS;
using System.Security.Claims;
using Utils.Filters;
using Utils.Logging;

namespace Pages
{
    public class IndexModel : PageModel
    {
        public ETag ETag { get; set; } //Ojo: esta propiedad debe llamarse así. No cambiar, porque lo casteamos cmomo dynamic con este nombre (ETag) desde el ETagFilterControllers para evitar usar reflection
        private readonly ILogger<IndexModel> _logger;
        private readonly IServicioMetricas _servicioMetricas;
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly IServicioSuscripciones _servicioSuscripciones;
        private readonly IServicioContextoDashboard _servicioContextoDashboard;
        private readonly Utils.Autologin.CreadorEnlaceAutologin _creadorEnlaceAutologin;

        // Propiedades para las métricas del dashboard
        public int UsuariosActivos { get; set; }
        public int ActividadMensual { get; set; }
        public int AccionesHoy { get; set; }
        public int TotalUsuarios { get; set; }

        //Tipo de usuario
        public bool EsUsuarioAdmin { get; set; }
        public bool EsTrabajadorEuropaPress { get; set; }
        public bool TieneSuscripcionActiva { get; set; }

        public bool EsProveedor { get; set; }

        public ContextoDashboardUsuario? ContextoDashboard { get; set; }

        public int? FkFuenteActiva { get; set; }

        public string? NombreFuenteActiva { get; set; }

        public ResumenDashboardProveedor ResumenProveedor { get; set; } = new ResumenDashboardProveedor();

        public Dictionary<string, int> ProveedorActividadMensual { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> ProveedorActividadDiaria { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, int> ProveedorDescargasPorFormato { get; set; } = new Dictionary<string, int>();

        [BindProperty(SupportsGet = true)]
        public int? FkFuenteProveedor { get; set; }

        // Datos para gráficos
        public Dictionary<string, int> ActividadMensualUltimos6Meses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AccionesPorModuloHoy { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ActividadDiariaUltimos7Dias { get; set; } = new Dictionary<string, int>();

        // Opciones de acciones rápidas
        public List<OpcionAccionRapida> OpcionesAccionesRapidas { get; set; } = new List<OpcionAccionRapida>();

        public string UrlEpDataConAutologin { get; set; } = "https://www.epdata.es";

        /// <summary>Título de la pestaña del navegador (ViewData Title).</summary>
        public string TituloPaginaDashboard { get; set; } = "Dashboard";

        /// <summary>Título principal bajo la cabecera del sitio.</summary>
        public string TituloBienvenida { get; set; } = "Bienvenido";

        /// <summary>Subtítulo o descripción bajo el título principal.</summary>
        public string DescripcionBienvenida { get; set; } = string.Empty;

        public class OpcionAccionRapida
        {
            public string Texto { get; set; }
            public string Icono { get; set; }
            public string Pagina { get; set; }
            public string Descripcion { get; set; }
        }

        public IndexModel(
            ILogger<IndexModel> logger,
            LoggerManual loggerManual,
            IRedis redis,
            Aurora auroraConfig,
            Anthropic anthropic,
            OpenAI openAI,
            Zilliz zilliz,
            Configuracion configuracion,
            Transparent transparentCDN,
            S3 s3,
            ISqs sqs,
            Modelos.InyeccionDeDependencias.Sqs sqsConfig,
            IServicioMetricas servicioMetricas,
            IServicioAuditoria servicioAuditoria,
            IServicioSuscripciones servicioSuscripciones,
            IServicioContextoDashboard servicioContextoDashboard,
            Utils.Autologin.CreadorEnlaceAutologin creadorEnlaceAutologin
        )
        {
            _logger = logger;
            _servicioMetricas = servicioMetricas;
            _servicioAuditoria = servicioAuditoria;
            _servicioSuscripciones = servicioSuscripciones;
            _servicioContextoDashboard = servicioContextoDashboard;
            _creadorEnlaceAutologin = creadorEnlaceAutologin;

            HelperS3 amazonS3 = new HelperS3(
                s3.Bucket,
                s3.AccessKeyId,
                s3.CloudfrontDistributionId,
                s3.SecretAccessKey,
                transparentCDN
            );
        }

        public async Task OnGetAsync()
        {
            try
            {
                ContextoDashboard = await _servicioContextoDashboard.ObtenerContextoAsync();
                EsProveedor = ContextoDashboard.Perfil == PerfilDashboard.Proveedor;

                if (EsProveedor)
                {
                    var fuente = _servicioContextoDashboard.ResolverFuenteProveedor(ContextoDashboard, FkFuenteProveedor);
                    FkFuenteActiva = fuente.FkFuente;
                    NombreFuenteActiva = fuente.NombreFuente;

                    await CargarDashboardProveedorAsync(fuente.FkFuente);
                    CargarOpcionesAccionesRapidas();
                    AplicarTextosBienvenida();
                    return;
                }

                // Obtener métricas en paralelo para mejor rendimiento
                var tareaUsuariosActivos = _servicioMetricas.ObtenerUsuariosActivosAsync();
                var tareaActividadMensual = _servicioMetricas.ObtenerActividadMensualAsync();
                var tareaAccionesHoy = _servicioMetricas.ObtenerAccionesHoyAsync();
                var tareaTotalUsuarios = _servicioMetricas.ObtenerTotalUsuariosAsync();

                // Obtener datos para gráficos (solo si el usuario tiene permisos de auditoría o métricas)
                var tareaActividadMensual6Meses = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());
                var tareaAccionesPorModulo = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());
                var tareaActividadDiaria7Dias = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());

                var usuarioTipo = User.FindFirstValue("FK_UsuarioTipo") ?? string.Empty;
                EsUsuarioAdmin = usuarioTipo != "1" && usuarioTipo != "2";
                EsTrabajadorEuropaPress = usuarioTipo is "2" or "3" or "4" or "5" or "6";

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out var fkUsuario))
                {
                    var contextoTarifa = await _servicioSuscripciones.ObtenerContextoTarifaAsync(fkUsuario, usuarioTipo);
                    EsTrabajadorEuropaPress = contextoTarifa.EsTrabajadorEuropaPress;
                    TieneSuscripcionActiva = contextoTarifa.TieneSuscripcionActiva;
                    UrlEpDataConAutologin = _creadorEnlaceAutologin.ObtenerUrlAutologinHaciaEpData(fkUsuario, "/");
                }

                if (User.IsInRole("Dashboard - Consulta de Métricas") ||
                    User.IsInRole("Dashboard - Auditoría") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    tareaActividadMensual6Meses = _servicioMetricas.ObtenerActividadMensualUltimos6MesesAsync();
                    tareaActividadDiaria7Dias = _servicioMetricas.ObtenerActividadDiariaUltimos7DiasAsync();
                }

                if (User.IsInRole("Dashboard - Auditoría") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    tareaAccionesPorModulo = _servicioAuditoria.ObtenerAccionesPorModuloHoyAsync();
                }

                await Task.WhenAll(
                    tareaUsuariosActivos,
                    tareaActividadMensual,
                    tareaAccionesHoy,
                    tareaTotalUsuarios,
                    tareaActividadMensual6Meses,
                    tareaAccionesPorModulo,
                    tareaActividadDiaria7Dias
                );

                UsuariosActivos = await tareaUsuariosActivos;
                ActividadMensual = await tareaActividadMensual;
                AccionesHoy = await tareaAccionesHoy;
                TotalUsuarios = await tareaTotalUsuarios;
                ActividadMensualUltimos6Meses = await tareaActividadMensual6Meses;
                AccionesPorModuloHoy = await tareaAccionesPorModulo;
                ActividadDiariaUltimos7Dias = await tareaActividadDiaria7Dias;

                // Cargar opciones de acciones rápidas basadas en permisos del usuario
                CargarOpcionesAccionesRapidas();
                AplicarTextosBienvenida();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener métricas del dashboard");
                // En caso de error, dejar valores en 0 (se mostrará como "0" en la vista)
                UsuariosActivos = 0;
                ActividadMensual = 0;
                AccionesHoy = 0;
                TotalUsuarios = 0;
                AplicarTextosBienvenida();
            }
        }

        private async Task CargarDashboardProveedorAsync(int fkFuente)
        {
            var tareaResumen = _servicioMetricas.ObtenerResumenDashboardProveedorAsync(fkFuente);
            var tareaMensual = _servicioMetricas.ObtenerActividadMensualProveedorAsync(fkFuente);
            var tareaDiaria = _servicioMetricas.ObtenerActividadDiariaProveedorAsync(fkFuente);
            var tareaFormatos = _servicioMetricas.ObtenerDescargasPorFormatoProveedorAsync(fkFuente);

            await Task.WhenAll(tareaResumen, tareaMensual, tareaDiaria, tareaFormatos);

            ResumenProveedor = await tareaResumen;
            ProveedorActividadMensual = await tareaMensual;
            ProveedorActividadDiaria = await tareaDiaria;
            ProveedorDescargasPorFormato = await tareaFormatos;
        }

        private void AplicarTextosBienvenida()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                TituloPaginaDashboard = "EpData · Acceso";
                TituloBienvenida = "Bienvenido";
                DescripcionBienvenida = "Inicia sesión para acceder a tu cuenta y a las funciones de EpData.";
                return;
            }

            if (EsProveedor)
            {
                var nombreFuente = NombreFuenteActiva ?? "su fuente";
                TituloPaginaDashboard = $"EpData · {nombreFuente}";
                TituloBienvenida = $"Panel de {nombreFuente}";
                DescripcionBienvenida =
                    "Consulte el uso agregado y anonimizado de los datos que su organización aporta a EpData.";
                return;
            }

            if (EsTrabajadorEuropaPress)
            {
                TituloPaginaDashboard = "Dashboard de Gestión";
                TituloBienvenida = "Bienvenido al Dashboard de Gestión";
                DescripcionBienvenida =
                    "Panel de control administrativo para la gestión de usuarios, métricas y documentación de EpData.";
                return;
            }

            TituloPaginaDashboard = "Mi cuenta EpData";
            if (TieneSuscripcionActiva)
            {
                TituloBienvenida = "Bienvenido a tu espacio en EpData";
                DescripcionBienvenida =
                    "Consulta tu plan, descargas, historial y opciones de cuenta desde un solo lugar.";
            }
            else
            {
                TituloBienvenida = "Bienvenido a tu cuenta EpData";
                DescripcionBienvenida =
                    "Gestiona tu suscripción, revisa tu actividad y elige el plan que mejor se adapte a ti.";
            }
        }

        private void CargarOpcionesAccionesRapidas()
        {
            OpcionesAccionesRapidas = new List<OpcionAccionRapida>();

            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return;
            }

            if (EsProveedor)
            {
                var nombreFuente = NombreFuenteActiva ?? "su fuente";
                OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                {
                    Texto = "Métricas de mi fuente",
                    Icono = "mis-graficas",
                    Pagina = "/metricas/kpis",
                    Descripcion = $"KPIs anonimizados del uso de los datos de {nombreFuente}"
                });
                return;
            }

            var usuarioTipo = User.FindFirstValue("FK_UsuarioTipo");
            var esUsuarioCliente = usuarioTipo == "1" || usuarioTipo == "2";
            var tienePermisosGestion = User.IsInRole("Dashboard - Gestión de Usuarios") || User.IsInRole("Dashboard - Consulta de Usuarios")
                || User.IsInRole("Dashboard - Gestión de Roles") || User.IsInRole("Dashboard - Gestión de Tokens")
                || User.IsInRole("Dashboard - Consulta de Métricas") || User.IsInRole("Dashboard - Exportación de Métricas")
                || User.IsInRole("Dashboard - Gestión de Documentación") || User.IsInRole("Dashboard - Auditoría")
                || User.IsInRole("Dashboard - Acceso total");

            if (esUsuarioCliente && !tienePermisosGestion)
            {
                // Opciones para usuarios tipo 1 (Usuario autenticado) y 2 (Abonado)

                // Gestión de suscripciones
                OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                {
                    Texto = "Gestión de Suscripciones",
                    Icono = "gestion-suscripciones",
                    Pagina = "/clientes/suscripciones",
                    Descripcion = "Gestiona tus suscripciones, consulta facturas y realiza pagos"
                });

                // Historial de acciones
                OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                {
                    Texto = "Historial de Acciones",
                    Icono = "historial-acciones",
                    Pagina = "/clientes/historial",
                    Descripcion = "Consulta tu historial de descargas de series, datos y gráficos"
                });

                // Resultados empresariales
                OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                {
                    Texto = "Resultados Empresariales",
                    Icono = "gestion-listados",
                    Pagina = "/Clientes/ResultadosEmpresariales",
                    Descripcion = "Compra y consulta los resultados empresariales disponibles"
                });
            }
            else
            {
                // Opciones para usuarios administradores

                if (User.IsInRole("Dashboard - Consulta de Usuarios") || User.IsInRole("Dashboard - Gestión de Usuarios") || User.IsInRole("Dashboard - Acceso total"))
                {
                    OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                    {
                        Texto = "Usuarios",
                        Icono = "gestion-de-usuarios",
                        Pagina = "/usuarios/listado",
                        Descripcion = "Gestiona usuarios del sistema, sus roles y permisos"
                    });
                }

                // Roles
                if (User.IsInRole("Dashboard - Gestión de Roles") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                    {
                        Texto = "Roles",
                        Icono = "gestion-listados",
                        Pagina = "/roles/listado",
                        Descripcion = "Visualiza y gestiona los roles del sistema"
                    });
                }

                // Métricas y KPIs
                if (User.IsInRole("Dashboard - Consulta de Métricas") ||
                    User.IsInRole("Dashboard - Exportación de Métricas") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                    {
                        Texto = "Métricas y KPIs",
                        Icono = "mis-graficas",
                        Pagina = "/metricas/kpis",
                        Descripcion = "Consulta métricas y KPIs del sistema con gráficos interactivos"
                    });
                }

                // Auditoría
                if (User.IsInRole("Dashboard - Auditoría") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                    {
                        Texto = "Auditoría",
                        Icono = "gestion-solicitudes",
                        Pagina = "/auditoria/auditoria",
                        Descripcion = "Consulta el registro de acciones administrativas del sistema"
                    });
                }

                // Documentación
                if (User.IsInRole("Dashboard - Gestión de Documentación") ||
                    User.IsInRole("Dashboard - Acceso total"))
                {
                    OpcionesAccionesRapidas.Add(new OpcionAccionRapida
                    {
                        Texto = "Documentación",
                        Icono = "documentacion",
                        Pagina = "/documentacion/listado",
                        Descripcion = "Gestiona documentos almacenados"
                    });
                }
            }
        }
    }
}
