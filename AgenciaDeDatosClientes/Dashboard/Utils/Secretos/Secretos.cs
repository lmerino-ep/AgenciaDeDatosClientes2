using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Polly;
using System.Text.Json;

namespace Utils.Secretos
{
    public class Secretos : ISecretos
    {
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly ILogger<Secretos> _logger;
        private readonly IAsyncPolicy<string> _retryPolicy;

        public Secretos(IAmazonSecretsManager secretsManager, ILogger<Secretos> logger)
        {
            _secretsManager = secretsManager;
            _logger = logger;

            _retryPolicy = Policy<string>
                .Handle<AmazonSecretsManagerException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Error al obtener secreto. Reintento {RetryCount}/3", retryCount);
                    });
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var request = new GetSecretValueRequest
                    {
                        SecretId = secretName
                    };

                    var response = await _secretsManager.GetSecretValueAsync(request, cancellationToken);
                    return response.SecretString ?? string.Empty;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el secreto {SecretName}", secretName);
                throw;
            }
        }

        public async Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                string secretString = await GetSecretAsync<string>(secretName, cancellationToken);
                return JsonSerializer.Deserialize<T>(secretString) ?? throw new InvalidOperationException($"No se pudo deserializar el secreto {secretName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al deserializar el secreto {SecretName} a tipo {Type}", secretName, typeof(T).Name);
                throw;
            }
        }

        public string GetSecretValue(string secretString, string key)
        {
            try
            {
                using var document = JsonDocument.Parse(secretString);

                if (document.RootElement.TryGetProperty(key, out var value))
                {
                    return value.GetString() ?? string.Empty;
                }

                throw new KeyNotFoundException($"La clave '{key}' no se encontró en el secret string");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la clave '{Key}' del secret string", key);
                throw;
            }
        }
    }
}
