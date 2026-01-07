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
    public class DetalleModel : PageModel
    {
        private readonly IServicioAuditoria _servicioAuditoria;
        private readonly ILogger<DetalleModel> _logger;

        public DetalleModel(
            IServicioAuditoria servicioAuditoria,
            ILogger<DetalleModel> logger)
        {
            _servicioAuditoria = servicioAuditoria ?? throw new ArgumentNullException(nameof(servicioAuditoria));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public AuditoriaDetalle Auditoria { get; set; }
        public List<CambioRealizado> Cambios { get; set; } = new List<CambioRealizado>();
        public string? MensajeError { get; set; }

        public class CambioRealizado
        {
            public string Campo { get; set; }
            public string Antes { get; set; }
            public string Despues { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (Id <= 0)
                {
                    MensajeError = "ID de auditoría no válido.";
                    return Page();
                }

                Auditoria = await _servicioAuditoria.ObtenerAuditoriaPorIdAsync(Id);

                if (Auditoria == null)
                {
                    MensajeError = "No se encontró el registro de auditoría solicitado.";
                    return Page();
                }

                // Parsear cambios realizados si existen
                if (!string.IsNullOrEmpty(Auditoria.CambiosRealizados))
                {
                    try
                    {
                        var cambiosJson = JsonSerializer.Deserialize<JsonElement>(Auditoria.CambiosRealizados);
                        
                        if (cambiosJson.TryGetProperty("antes", out var antes) && 
                            cambiosJson.TryGetProperty("despues", out var despues))
                        {
                            // Obtener todas las propiedades de ambos objetos
                            var propiedadesAntes = antes.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());
                            var propiedadesDespues = despues.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());

                            // Obtener todas las claves únicas
                            var todasLasClaves = propiedadesAntes.Keys.Union(propiedadesDespues.Keys).Distinct();

                            foreach (var clave in todasLasClaves)
                            {
                                var valorAntes = propiedadesAntes.ContainsKey(clave) ? propiedadesAntes[clave] : "(vacío)";
                                var valorDespues = propiedadesDespues.ContainsKey(clave) ? propiedadesDespues[clave] : "(vacío)";

                                if (valorAntes != valorDespues)
                                {
                                    Cambios.Add(new CambioRealizado
                                    {
                                        Campo = clave,
                                        Antes = valorAntes,
                                        Despues = valorDespues
                                    });
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Error al parsear cambios realizados para auditoría {Id}", Id);
                        // Si no se puede parsear, simplemente no mostramos los cambios
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalle de auditoría {Id}", Id);
                MensajeError = "Error al cargar el detalle de auditoría. Por favor, intenta nuevamente.";
                return Page();
            }
        }

        /// <summary>
        /// Parsea el User Agent para mostrar información legible
        /// </summary>
        public string ParsearUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "-";

            // Parseo básico del User Agent
            // Esto es una versión simplificada, se puede mejorar con una librería como UAParser
            if (userAgent.Contains("Chrome") && !userAgent.Contains("Edg"))
                return "Chrome";
            if (userAgent.Contains("Firefox"))
                return "Firefox";
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
                return "Safari";
            if (userAgent.Contains("Edg"))
                return "Edge";
            if (userAgent.Contains("Opera") || userAgent.Contains("OPR"))
                return "Opera";

            return userAgent.Length > 50 ? userAgent.Substring(0, 50) + "..." : userAgent;
        }
    }
}

