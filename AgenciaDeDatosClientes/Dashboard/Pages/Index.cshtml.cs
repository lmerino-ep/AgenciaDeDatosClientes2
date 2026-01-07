using AutoLogin.Token;
using AutoLogin.Verification.Interfaces;
using Utils.Filters;
using Utils.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Modelos.InyeccionDeDependencias;
using Nuget.Helper;
using NugetPortable.Cache;
using NugetPortable.Herramientas.CDN;
using NugetPortable.SQS;
using Negocio;

namespace Pages
{
    public class IndexModel : PageModel
    {
        public ETag ETag { get; set; } //Ojo: esta propiedad debe llamarse así. No cambiar, porque lo casteamos cmomo dynamic con este nombre (ETag) desde el ETagFilterControllers para evitar usar reflection
        private readonly ILogger<IndexModel> _logger;
        private readonly IServicioMetricas _servicioMetricas;
        private readonly IServicioAuditoria _servicioAuditoria;

        // Propiedades para las métricas del dashboard
        public int UsuariosActivos { get; set; }
        public int ActividadMensual { get; set; }
        public int AccionesHoy { get; set; }
        public int TotalUsuarios { get; set; }

        // Datos para gráficos
        public Dictionary<string, int> ActividadMensualUltimos6Meses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AccionesPorModuloHoy { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ActividadDiariaUltimos7Dias { get; set; } = new Dictionary<string, int>();

        // Opciones de acciones rápidas
        public List<OpcionAccionRapida> OpcionesAccionesRapidas { get; set; } = new List<OpcionAccionRapida>();

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
            ITokenHandler autoLoginTokenHandler,
            ITokenDbService autoLoginTokenDbService,
            S3 s3,
            ISqs sqs,
            Modelos.InyeccionDeDependencias.Sqs sqsConfig,
            IServicioMetricas servicioMetricas,
            IServicioAuditoria servicioAuditoria
        )
        {
            _logger = logger;
            _servicioMetricas = servicioMetricas;
            _servicioAuditoria = servicioAuditoria;
            
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
                // Obtener métricas en paralelo para mejor rendimiento
                var tareaUsuariosActivos = _servicioMetricas.ObtenerUsuariosActivosAsync();
                var tareaActividadMensual = _servicioMetricas.ObtenerActividadMensualAsync();
                var tareaAccionesHoy = _servicioMetricas.ObtenerAccionesHoyAsync();
                var tareaTotalUsuarios = _servicioMetricas.ObtenerTotalUsuariosAsync();

                // Obtener datos para gráficos (solo si el usuario tiene permisos de auditoría o métricas)
                var tareaActividadMensual6Meses = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());
                var tareaAccionesPorModulo = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());
                var tareaActividadDiaria7Dias = Task.FromResult<Dictionary<string, int>>(new Dictionary<string, int>());

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener métricas del dashboard");
                // En caso de error, dejar valores en 0 (se mostrará como "0" en la vista)
                UsuariosActivos = 0;
                ActividadMensual = 0;
                AccionesHoy = 0;
                TotalUsuarios = 0;
            }
        }

        private void CargarOpcionesAccionesRapidas()
        {
            OpcionesAccionesRapidas = new List<OpcionAccionRapida>();

            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return;
            }

            // Usuarios (siempre visible para usuarios autenticados)
            OpcionesAccionesRapidas.Add(new OpcionAccionRapida
            {
                Texto = "Usuarios",
                Icono = "gestion-de-usuarios",
                Pagina = "/usuarios/listado",
                Descripcion = "Gestiona usuarios del sistema, sus roles y permisos"
            });

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
                    Pagina = "/Auditoria/Auditoria",
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
                    Descripcion = "Gestiona documentos almacenados en Google Drive"
                });
            }
        }
    }
}
