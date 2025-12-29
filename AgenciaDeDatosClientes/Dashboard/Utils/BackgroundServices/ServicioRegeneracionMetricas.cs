using Negocio;

namespace Utils.BackgroundServices
{
    /// <summary>
    /// Background service que regenera periódicamente los KPIs más consultados
    /// para mantener el caché caliente y reducir tiempos de respuesta
    /// </summary>
    public class ServicioRegeneracionMetricas : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServicioRegeneracionMetricas> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(10);

        public ServicioRegeneracionMetricas(
            IServiceProvider serviceProvider,
            ILogger<ServicioRegeneracionMetricas> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de regeneración de métricas iniciado");

            // Esperar 1 minuto después del inicio para no interferir con el startup
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Iniciando regeneración periódica de KPIs principales");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var servicioMetricas = scope.ServiceProvider.GetRequiredService<IServicioMetricas>();
                        await servicioMetricas.RegenerarKpisPrincipalesAsync();
                    }

                    _logger.LogInformation("Regeneración periódica completada. Próxima ejecución en {Intervalo} minutos", _intervalo.TotalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la regeneración periódica de KPIs");
                }

                // Esperar hasta la próxima ejecución
                try
                {
                    await Task.Delay(_intervalo, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Servicio de regeneración de métricas detenido por cancelación");
                    break;
                }
            }

            _logger.LogInformation("Servicio de regeneración de métricas finalizado");
        }
    }
}
