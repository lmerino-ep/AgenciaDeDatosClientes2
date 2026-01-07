using Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using Modelos.DTO.Cabecera;

namespace Controllers
{
    /// <summary>
    /// Controlador para operaciones de autenticación
    /// </summary>
    public class AutenticacionController : Controller
    {
        private readonly ILogger<AutenticacionController> _logger;
        private readonly SignInManager<Modelos.Autenticacion.Usuario> _signInManager;
        private readonly UserManager<Modelos.Autenticacion.Usuario> _userManager;

        public AutenticacionController(
            ILogger<AutenticacionController> logger,
            SignInManager<Modelos.Autenticacion.Usuario> signInManager,
            UserManager<Modelos.Autenticacion.Usuario> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Cierra la sesión del usuario autenticado
        /// </summary>
        [AllowAnonymous]
        [Route("logout")]
        public async Task<IActionResult> LogOut()
        {
            string urlDestino = "/";
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("Usuario cerró sesión correctamente");
            }
            return Redirect(urlDestino);
        }

        /// <summary>
        /// Devuelve las opciones del menú "Mi Cuenta" según el usuario autenticado
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ResponseCache(CacheProfileName = "NoCache")]
        [Route("opciones-mi-cuenta")]
        public async Task<dynamic> OpcionesMiCuenta()
        {
            dynamic resultado = new ExpandoObject();
            List<OpcionMiCuenta> opciones = new List<OpcionMiCuenta>();

            try
            {
                // DEBUGGING: Log authentication state
                var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
                var userName = User?.Identity?.Name ?? "null";
                _logger.LogInformation("OpcionesMiCuenta llamado - IsAuthenticated: {IsAuthenticated}, UserName: {UserName}", isAuthenticated, userName);

                if (isAuthenticated)
                {
                    // Usuario autenticado
                    _logger.LogInformation("Usuario autenticado detectado - Mostrando opciones de usuario autenticado");

                    // Opción Dashboard (siempre visible para usuarios autenticados)
                    opciones.Add(new OpcionMiCuenta
                    {
                        Texto = "Dashboard",
                        Icono = "home",
                        Pagina = "/index"
                    });

                    // === GESTIÓN DE USUARIOS ===
                    opciones.Add(new OpcionMiCuenta
                    {
                        Texto = "Usuarios",
                        Icono = "gestion-de-usuarios",
                        Pagina = "/usuarios/listado"
                    });

                    // === GESTIÓN DE ROLES ===
                    if (User.IsInRole("Dashboard - Gestión de Roles") ||
                        User.IsInRole("Dashboard - Acceso total"))
                    {
                        opciones.Add(new OpcionMiCuenta
                        {
                            Texto = "Roles",
                            Icono = "gestion-listados",
                            Pagina = "/roles/listado"
                        });
                    }

                    // === MÉTRICAS Y KPIs (Fase 3) ===
                    // Verificar permisos para consulta de métricas
                    if (User.IsInRole("Dashboard - Consulta de Métricas") ||
                        User.IsInRole("Dashboard - Exportación de Métricas") ||
                        User.IsInRole("Dashboard - Acceso total"))
                    {
                        opciones.Add(new OpcionMiCuenta
                        {
                            Texto = "Métricas y KPIs",
                            Icono = "mis-graficas",
                            Pagina = "/metricas/kpis"
                        });
                    }

                    // === AUDITORÍA ===
                    if (User.IsInRole("Dashboard - Auditoría") ||
                        User.IsInRole("Dashboard - Acceso total"))
                    {
                        opciones.Add(new OpcionMiCuenta
                        {
                            Texto = "Auditoría",
                            Icono = "gestion-solicitudes",
                            Pagina = "/Auditoria/Auditoria"
                        });
                    }

                    // === DOCUMENTACIÓN ===
                    if (User.IsInRole("Dashboard - Gestión de Documentación") ||
                        User.IsInRole("Dashboard - Acceso total"))
                    {
                        opciones.Add(new OpcionMiCuenta
                        {
                            Texto = "Documentación",
                            Icono = "documentacion",
                            Pagina = "/documentacion/listado"
                        });
                    }

                    OpcionesMiCuenta modelo = new OpcionesMiCuenta
                    {
                        Opciones = opciones,
                        HaySesion = true
                    };

                    resultado.bCorrecto = true;
                    resultado.sHtml = await this.GenerarHtml("Components/Cabecera/OpcionesMiCuenta", modelo, true);
                }
                else
                {
                    // Usuario no autenticado
                    _logger.LogInformation("Usuario NO autenticado - Mostrando opciones de guest");

                    opciones.Add(new OpcionMiCuenta
                    {
                        Texto = "Iniciar Sesión",
                        Icono = "iniciar-sesion",
                        Pagina = "/login"
                    });

                    OpcionesMiCuenta modelo = new OpcionesMiCuenta
                    {
                        Opciones = opciones,
                        HaySesion = false
                    };

                    resultado.bCorrecto = true;
                    resultado.sHtml = await this.GenerarHtml("Components/Cabecera/OpcionesMiCuenta", modelo, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar opciones de Mi Cuenta");
                resultado.bCorrecto = false;
                resultado.sMotivo = "Error al cargar las opciones del menú. Inténtelo de nuevo más tarde.";
            }

            return resultado;
        }
    }
}
