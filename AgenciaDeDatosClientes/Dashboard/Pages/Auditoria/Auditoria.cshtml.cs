using Utils.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Modelos.DTO;
using Negocio;
using System.Text.Json;

namespace Pages.Auditoria
{
    [Authorize(Policy = PoliticasAutorizacion.Politicas.Auditoria)]
    [ResponseCache(CacheProfileName = "NoCache")]
    public class AuditoriaModel : PageModel
    {
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly ILogger<AuditoriaModel> _logger;

        public AuditoriaModel(
            IServicioAuditoria servicioAuditoria,
            IServicioUsuarios servicioUsuarios,
            ILogger<AuditoriaModel> logger)
        {
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _servicioUsuarios = servicioUsuarios ?? throw new ArgumentNullException(nameof(servicioUsuarios));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Filtros
        [BindProperty(SupportsGet = true)]
        public int? UsuarioId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Accion { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Modulo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Resultado { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaFin { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int TamanoPagina { get; set; } = 100;

        // Datos para mostrar
        public List<AuditoriaConUsuario> Auditorias { get; set; } = new List<AuditoriaConUsuario>();
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }

        // Listas para dropdowns
        public List<UsuarioDropdown> Usuarios { get; set; } = new List<UsuarioDropdown>();
        public List<string> Acciones { get; set; } = new List<string>();
        public List<string> Modulos { get; set; } = new List<string>();

        public string? MensajeError { get; set; }

        public class UsuarioDropdown
        {
            public int Id { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Si no hay fechas, establecer rango por defecto (últimos 30 días)
                if (!FechaInicio.HasValue)
                {
                    FechaInicio = DateTime.Now.AddDays(-30);
                }
                if (!FechaFin.HasValue)
                {
                    FechaFin = DateTime.Now;
                }

                // Obtener usuarios con roles de dashboard para el dropdown
                Usuarios = await ObtenerUsuariosConRolesDashboardAsync();

                // Obtener acciones y módulos únicos para los dropdowns
                Acciones = await ObtenerAccionesUnicasAsync();
                Modulos = await ObtenerModulosUnicosAsync();

                // Obtener auditorías con filtros
                int offset = Pagina * TamanoPagina;
                Auditorias = await _servicioAuditoria.ObtenerAuditoriasAsync(
                    UsuarioId,
                    Accion,
                    Modulo,
                    Resultado,
                    FechaInicio,
                    FechaFin,
                    TamanoPagina,
                    offset
                );

                // Obtener total para paginación
                TotalRegistros = await _servicioAuditoria.ObtenerTotalAuditoriasAsync(
                    UsuarioId,
                    Accion,
                    Modulo,
                    Resultado,
                    FechaInicio,
                    FechaFin
                );

                TotalPaginas = (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar auditoría");
                MensajeError = "Error al cargar los registros de auditoría. Por favor, intenta nuevamente.";
                return Page();
            }
        }

        /// <summary>
        /// Exporta los registros de auditoría en formato JSON
        /// </summary>
        public async Task<IActionResult> OnGetExportarJsonAsync()
        {
            try
            {
                // Aplicar los mismos filtros pero sin límite
                var auditorias = await _servicioAuditoria.ObtenerAuditoriasAsync(
                    UsuarioId,
                    Accion,
                    Modulo,
                    Resultado,
                    FechaInicio,
                    FechaFin,
                    100000, // Límite alto para exportación
                    0
                );

                var resultado = new
                {
                    timestamp_exportacion = DateTime.UtcNow,
                    usuario_exportacion = User.Identity?.Name,
                    total_registros = auditorias.Count,
                    filtros = new
                    {
                        usuario_id = UsuarioId,
                        accion = Accion,
                        modulo = Modulo,
                        resultado = Resultado,
                        fecha_inicio = FechaInicio,
                        fecha_fin = FechaFin
                    },
                    datos = auditorias
                };

                var json = JsonSerializer.Serialize(resultado, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                return File(
                    System.Text.Encoding.UTF8.GetBytes(json),
                    "application/json",
                    $"auditoria_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar auditoría en JSON");
                TempData["MensajeError"] = $"Error al exportar: {ex.Message}";
                return RedirectToPage();
            }
        }

        /// <summary>
        /// Exporta los registros de auditoría en formato CSV
        /// </summary>
        public async Task<IActionResult> OnGetExportarCsvAsync()
        {
            try
            {
                // Aplicar los mismos filtros pero sin límite
                var auditorias = await _servicioAuditoria.ObtenerAuditoriasAsync(
                    UsuarioId,
                    Accion,
                    Modulo,
                    Resultado,
                    FechaInicio,
                    FechaFin,
                    100000, // Límite alto para exportación
                    0
                );

                // Construir CSV
                var csv = new System.Text.StringBuilder();
                
                // Encabezados
                csv.AppendLine("Fecha/Hora;Usuario Email;Usuario Nombre;Acción;Módulo;Descripción;Resultado;Dirección IP");

                // Datos
                foreach (var auditoria in auditorias)
                {
                    csv.AppendLine($"{auditoria.FechaHora:yyyy-MM-dd HH:mm:ss};" +
                                   $"{EscapeCsv(auditoria.UsuarioEmail)};" +
                                   $"{EscapeCsv(auditoria.UsuarioNombre)};" +
                                   $"{EscapeCsv(auditoria.Accion)};" +
                                   $"{EscapeCsv(auditoria.Modulo)};" +
                                   $"{EscapeCsv(auditoria.Descripcion)};" +
                                   $"{EscapeCsv(auditoria.Resultado)};" +
                                   $"{EscapeCsv(auditoria.DireccionIP ?? "")}");
                }

                // UTF-8 con BOM para Excel
                var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(
                    System.Text.Encoding.UTF8.GetBytes(csv.ToString())
                ).ToArray();

                return File(
                    bytes,
                    "text/csv; charset=utf-8",
                    $"auditoria_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar auditoría en CSV");
                TempData["MensajeError"] = $"Error al exportar: {ex.Message}";
                return RedirectToPage();
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Si contiene punto y coma, comillas o saltos de línea, encerrar entre comillas y duplicar comillas
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private async Task<List<UsuarioDropdown>> ObtenerUsuariosConRolesDashboardAsync()
        {
            try
            {
                // Obtener todos los usuarios activos que tienen roles de dashboard
                var todosUsuarios = await _servicioUsuarios.ListarUsuariosAsync(null, true, null, 0, 1000);
                
                // Filtrar solo los que tienen roles de dashboard (esto se puede optimizar con una consulta SQL directa)
                // Por ahora, devolvemos todos los usuarios activos
                return todosUsuarios.usuarios
                    .Select(u => new UsuarioDropdown
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Nombre = $"{u.Nombre} {u.Apellidos}".Trim()
                    })
                    .OrderBy(u => u.Email)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener usuarios con roles de dashboard");
                return new List<UsuarioDropdown>();
            }
        }

        private Task<List<string>> ObtenerAccionesUnicasAsync()
        {
            // Valores estáticos basados en TiposAccionAuditoria
            return Task.FromResult(new List<string>
            {
                Modelos.TiposAccionAuditoria.UsuarioCrear,
                Modelos.TiposAccionAuditoria.UsuarioEditar,
                Modelos.TiposAccionAuditoria.UsuarioEliminar,
                Modelos.TiposAccionAuditoria.UsuarioCambiarContrasena,
                Modelos.TiposAccionAuditoria.UsuarioCambiarTipo,
                Modelos.TiposAccionAuditoria.RolesAsignar,
                Modelos.TiposAccionAuditoria.RolesRemover,
                Modelos.TiposAccionAuditoria.TokensGenerarRefreshToken1,
                Modelos.TiposAccionAuditoria.TokensGenerarRefreshToken2,
                Modelos.TiposAccionAuditoria.TokensRevocarTokens,
                Modelos.TiposAccionAuditoria.MetricasConsultar,
                Modelos.TiposAccionAuditoria.MetricasExportar,
                Modelos.TiposAccionAuditoria.DocumentacionSubir,
                Modelos.TiposAccionAuditoria.DocumentacionDescargar,
                Modelos.TiposAccionAuditoria.DocumentacionEliminar,
                Modelos.TiposAccionAuditoria.DocumentacionSobrescribir
            });
        }

        private Task<List<string>> ObtenerModulosUnicosAsync()
        {
            // Valores estáticos basados en ModulosDashboard
            return Task.FromResult(new List<string>
            {
                Modelos.ModulosDashboard.Usuarios,
                Modelos.ModulosDashboard.Roles,
                Modelos.ModulosDashboard.Tokens,
                Modelos.ModulosDashboard.Metricas,
                Modelos.ModulosDashboard.Documentacion
            });
        }
    }
}

