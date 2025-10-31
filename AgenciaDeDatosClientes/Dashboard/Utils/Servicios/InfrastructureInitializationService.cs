using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modelos.InyeccionDeDependencias;
using Dashboard.Utils.Secretos;
using Amazon.SQS;
using Amazon.SecretsManager;
using Dashboard.Utils.Servicios;
using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Dashboard.Utils.Servicios
{
    /// <summary>
    /// Servicio para inicializar infraestructura (Redis y SQS) de manera asíncrona
    /// </summary>
    public class InfrastructureInitializationService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly Modelos.InyeccionDeDependencias.Sqs _sqsConfig;
        private readonly Modelos.InyeccionDeDependencias.Redis _redisConfig;
        private readonly Modelos.InyeccionDeDependencias.Aurora _auroraConfig;
        private readonly Modelos.InyeccionDeDependencias.Zilliz _zillizConfig;
        private readonly Modelos.InyeccionDeDependencias.OpenAI _openAIConfig;
        private readonly Modelos.InyeccionDeDependencias.Anthropic _anthropicConfig;
        private readonly Modelos.InyeccionDeDependencias.Google _googleConfig;
        private readonly Modelos.InyeccionDeDependencias.AutoLogin _autoLoginConfig;
        private readonly Modelos.InyeccionDeDependencias.S3 _s3Config;
        private readonly Modelos.InyeccionDeDependencias.Cdn _cdnConfig;
        private readonly ILogger<InfrastructureInitializationService> _logger;
        private readonly IServiceProvider _serviceProviderTemporal;
        private readonly IServiceCollection _serviceCollection;
        private Task? _initializationTask;
        private bool _isInitialized = false;

        public InfrastructureInitializationService(
            IAmazonSQS sqsClient,
            IAmazonSecretsManager secretsManager,
            Modelos.InyeccionDeDependencias.Sqs sqsConfig,
            Modelos.InyeccionDeDependencias.Redis redisConfig,
            Modelos.InyeccionDeDependencias.Aurora auroraConfig,
            Modelos.InyeccionDeDependencias.Zilliz zillizConfig,
            Modelos.InyeccionDeDependencias.OpenAI openAIConfig,
            Modelos.InyeccionDeDependencias.Anthropic anthropicConfig,
            Modelos.InyeccionDeDependencias.Google googleConfig,
            Modelos.InyeccionDeDependencias.AutoLogin autoLoginConfig,
            Modelos.InyeccionDeDependencias.S3 s3Config,
            Modelos.InyeccionDeDependencias.Cdn cdnConfig,
            ILogger<InfrastructureInitializationService> logger,
            IServiceProvider serviceProviderTemporal,
            IServiceCollection serviceCollection)
        {
            _sqsClient = sqsClient;
            _secretsManager = secretsManager;
            _sqsConfig = sqsConfig;
            _redisConfig = redisConfig;
            _auroraConfig = auroraConfig;
            _zillizConfig = zillizConfig;
            _openAIConfig = openAIConfig;
            _anthropicConfig = anthropicConfig;
            _googleConfig = googleConfig;
            _autoLoginConfig = autoLoginConfig;
            _s3Config = s3Config;
            _cdnConfig = cdnConfig;
            _logger = logger;
            _serviceProviderTemporal = serviceProviderTemporal;
            _serviceCollection = serviceCollection;

            //Como hemos recibido por DI estos objetos, cuando desde le program.cs se llame a InitializeAsync se modificarán los valores de sus propiedades
            //de manera que las futuras DI se recibirán estos objetos con sus valores correctos y rellenos, pues es la misma instancia la que se propaga
        }

        /// <summary>
        /// Inicializa la infraestructura de manera asíncrona
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                _logger.LogInformation("Infraestructura ya está inicializada, saltando inicialización");
                return;
            }

            if (_initializationTask != null)
            {
                // Si ya hay una inicialización en curso, esperar a que termine
                await _initializationTask;
                return;
            }

            _initializationTask = InitializeInfrastructureAsync(cancellationToken);
            await _initializationTask;
            _isInitialized = true;
        }

        /// <summary>
        /// Espera a que se complete la inicialización (método legacy para compatibilidad)
        /// </summary>
        public async Task WaitForInitializationAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);
        }

        /// <summary>
        /// Inicializa toda la infraestructura de manera asíncrona
        /// </summary>
        private async Task InitializeInfrastructureAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Iniciando inicialización asíncrona de infraestructura...");

                // Crear un CancellationTokenSource con timeout para cada operación
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(2)); // 2 minutos total

                // Ejecutar inicializaciones en paralelo
                var redisTask = InitializeRedisAsync(cts.Token);
                var sqsTask = InitializeSqsAsync(cts.Token);
                var auroraTask = InitializeAuroraAsync(cts.Token);
                var zillizTask = InitializeZillizAsync(cts.Token);
                var openAITask = InitializeOpenAIAsync(cts.Token);
                var anthropicTask = InitializeAnthropicAsync(cts.Token);                
                var googleTask = InitializeGoogleAsync(cts.Token);
                var autoLoginTask = InitializeAutoLoginAsync(cts.Token);
                var s3Task = InitializeS3Async(cts.Token);
                var cdnTask = InitializeCdnAsync(cts.Token);

                await Task.WhenAll(
                    redisTask, 
                    sqsTask, 
                    auroraTask, 
                    zillizTask, 
                    openAITask, 
                    anthropicTask, 
                    googleTask, 
                    autoLoginTask, 
                    s3Task, 
                    cdnTask
                );

                _logger.LogInformation("Toda la infraestructura inicializada correctamente");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Timeout inicializando infraestructura");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando infraestructura");
                throw;
            }
        }

        /// <summary>
        /// Inicializa Aurora resolviendo el host desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeAuroraAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando Aurora...");

                if (_auroraConfig.UseSecretsManager && !string.IsNullOrEmpty(_auroraConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo cadenas de conexión de Aurora desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_auroraConfig.NombreDelSecreto, cancellationToken);

                    // Obtener cadena de conexión de lectura
                    if (!string.IsNullOrEmpty(_auroraConfig.SecretoClaveLectura))
                    {
                        string cadenaDeConexionLectura = secretos.GetSecretValue(secretString, _auroraConfig.SecretoClaveLectura);
                        if (!string.IsNullOrEmpty(cadenaDeConexionLectura))
                        {
                            _auroraConfig.Lectura = cadenaDeConexionLectura;
                            _logger.LogInformation("Cadena de conexión de Aurora de lectura resuelta correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver la cadena de conexión de Aurora de lectura desde Secrets Manager, usando configuración por defecto");
                        }
                    }

                    // Obtener cadena de conexión de escritura
                    if (!string.IsNullOrEmpty(_auroraConfig.SecretoClaveEscritura))
                    {
                        string cadenaDeConexionEscritura = secretos.GetSecretValue(secretString, _auroraConfig.SecretoClaveEscritura);
                        if (!string.IsNullOrEmpty(cadenaDeConexionEscritura))
                        {
                            _auroraConfig.Escritura = cadenaDeConexionEscritura;
                            _logger.LogInformation("Cadena de conexión de Aurora de escritura resuelta correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver la cadena de conexión de Aurora de escritura desde Secrets Manager, usando configuración por defecto");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando cadenas de conexión de Aurora de configuración: {Lectura}, {Escritura}", _auroraConfig.Lectura, _auroraConfig.Escritura);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando cadenas de conexión de Aurora");
                throw;
            }
        }

        /// <summary>
        /// Inicializa Zilliz resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeZillizAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando Zilliz...");

                if (_zillizConfig.UseSecretsManager && !string.IsNullOrEmpty(_zillizConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de Zilliz desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_zillizConfig.NombreDelSecreto, cancellationToken);

                    // Obtener límite de bulk insert
                    if (!string.IsNullOrEmpty(_zillizConfig.SecretoClaveLimiteBulkInsert))
                    {
                        var limiteBulkInsertValue = secretos.GetSecretValue(secretString, _zillizConfig.SecretoClaveLimiteBulkInsert);
                        if (int.TryParse(limiteBulkInsertValue, out int limiteBulkInsert))
                        {
                            _zillizConfig.LimiteBulkInsert = limiteBulkInsert;
                            _logger.LogInformation("Límite de bulk insert de Zilliz resuelto correctamente desde Secrets Manager: {LimiteBulkInsert}", limiteBulkInsert);
                        }
                    }

                    // Obtener nombre de colección
                    if (!string.IsNullOrEmpty(_zillizConfig.SecretoClaveNombreColeccion))
                    {
                        var nombreColeccion = secretos.GetSecretValue(secretString, _zillizConfig.SecretoClaveNombreColeccion);
                        if (!string.IsNullOrEmpty(nombreColeccion))
                        {
                            _zillizConfig.NombreColeccion = nombreColeccion;
                            _logger.LogInformation("Nombre de colección de Zilliz resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener algoritmo de cálculo de similitud
                    if (!string.IsNullOrEmpty(_zillizConfig.SecretoClaveAlgoritmoCalculoSimilitud))
                    {
                        var algoritmo = secretos.GetSecretValue(secretString, _zillizConfig.SecretoClaveAlgoritmoCalculoSimilitud);
                        if (!string.IsNullOrEmpty(algoritmo))
                        {
                            _zillizConfig.AlgoritmoCalculoSimilitud = algoritmo;
                            _logger.LogInformation("Algoritmo de cálculo de similitud de Zilliz resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener API key
                    if (!string.IsNullOrEmpty(_zillizConfig.SecretoClaveApiKey))
                    {
                        var apiKey = secretos.GetSecretValue(secretString, _zillizConfig.SecretoClaveApiKey);
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            _zillizConfig.ApiKey = apiKey;
                            _logger.LogInformation("API key de Zilliz resuelta correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener endpoint
                    if (!string.IsNullOrEmpty(_zillizConfig.SecretoClaveEndpoint))
                    {
                        var endpoint = secretos.GetSecretValue(secretString, _zillizConfig.SecretoClaveEndpoint);
                        if (!string.IsNullOrEmpty(endpoint))
                        {
                            _zillizConfig.Endpoint = endpoint;
                            _logger.LogInformation("Endpoint de Zilliz resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de Zilliz por defecto: Endpoint={Endpoint}, Colección={Coleccion}, Algoritmo={Algoritmo}",
                        _zillizConfig.Endpoint, _zillizConfig.NombreColeccion, _zillizConfig.AlgoritmoCalculoSimilitud);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Zilliz");
                throw;
            }
        }

        /// <summary>
        /// Inicializa OpenAI resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeOpenAIAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando OpenAI...");

                if (_openAIConfig.UseSecretsManager && !string.IsNullOrEmpty(_openAIConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de OpenAI desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_openAIConfig.NombreDelSecreto, cancellationToken);

                    // Obtener dimensiones del vector
                    if (!string.IsNullOrEmpty(_openAIConfig.SecretoClaveDimensionesVector))
                    {
                        var dimensionesValue = secretos.GetSecretValue(secretString, _openAIConfig.SecretoClaveDimensionesVector);
                        if (int.TryParse(dimensionesValue, out int dimensiones))
                        {
                            _openAIConfig.DimensionesVector = dimensiones;
                            _logger.LogInformation("Dimensiones del vector de OpenAI resueltas correctamente desde Secrets Manager: {Dimensiones}", dimensiones);
                        }
                    }

                    // Obtener timeout
                    if (!string.IsNullOrEmpty(_openAIConfig.SecretoClaveSegundosTimeOut))
                    {
                        var timeoutValue = secretos.GetSecretValue(secretString, _openAIConfig.SecretoClaveSegundosTimeOut);
                        if (int.TryParse(timeoutValue, out int timeout))
                        {
                            _openAIConfig.SegundosTimeOut = timeout;
                            _logger.LogInformation("Timeout de OpenAI resuelto correctamente desde Secrets Manager: {Timeout}", timeout);
                        }
                    }

                    // Obtener modelo
                    if (!string.IsNullOrEmpty(_openAIConfig.SecretoClaveModelo))
                    {
                        var modelo = secretos.GetSecretValue(secretString, _openAIConfig.SecretoClaveModelo);
                        if (!string.IsNullOrEmpty(modelo))
                        {
                            _openAIConfig.Modelo = modelo;
                            _logger.LogInformation("Modelo de OpenAI resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener API key
                    if (!string.IsNullOrEmpty(_openAIConfig.SecretoClaveApiKey))
                    {
                        var apiKey = secretos.GetSecretValue(secretString, _openAIConfig.SecretoClaveApiKey);
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            _openAIConfig.ApiKey = apiKey;
                            _logger.LogInformation("API key de OpenAI resuelta correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener endpoint
                    if (!string.IsNullOrEmpty(_openAIConfig.SecretoClaveEndpoint))
                    {
                        var endpoint = secretos.GetSecretValue(secretString, _openAIConfig.SecretoClaveEndpoint);
                        if (!string.IsNullOrEmpty(endpoint))
                        {
                            _openAIConfig.Endpoint = endpoint;
                            _logger.LogInformation("Endpoint de OpenAI resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de OpenAI por defecto: Endpoint={Endpoint}, Modelo={Modelo}, Dimensiones={Dimensiones}",
                        _openAIConfig.Endpoint, _openAIConfig.Modelo, _openAIConfig.DimensionesVector);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando OpenAI");
                throw;
            }
        }

        /// <summary>
        /// Inicializa Anthropic resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeAnthropicAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando Anthropic...");

                if (_anthropicConfig.UseSecretsManager && !string.IsNullOrEmpty(_anthropicConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de Anthropic desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_anthropicConfig.NombreDelSecreto, cancellationToken);

                    // Obtener modelo
                    if (!string.IsNullOrEmpty(_anthropicConfig.SecretoClaveModelo))
                    {
                        var modelo = secretos.GetSecretValue(secretString, _anthropicConfig.SecretoClaveModelo);
                        if (!string.IsNullOrEmpty(modelo))
                        {
                            _anthropicConfig.Modelo = modelo;
                            _logger.LogInformation("Modelo de Anthropic resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener clave
                    if (!string.IsNullOrEmpty(_anthropicConfig.SecretoClaveClave))
                    {
                        var clave = secretos.GetSecretValue(secretString, _anthropicConfig.SecretoClaveClave);
                        if (!string.IsNullOrEmpty(clave))
                        {
                            _anthropicConfig.Clave = clave;
                            _logger.LogInformation("Clave de Anthropic resuelta correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener endpoint
                    if (!string.IsNullOrEmpty(_anthropicConfig.SecretoClaveEndpoint))
                    {
                        var endpoint = secretos.GetSecretValue(secretString, _anthropicConfig.SecretoClaveEndpoint);
                        if (!string.IsNullOrEmpty(endpoint))
                        {
                            _anthropicConfig.Endpoint = endpoint;
                            _logger.LogInformation("Endpoint de Anthropic resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de Anthropic por defecto: Endpoint={Endpoint}, Modelo={Modelo}",
                        _anthropicConfig.Endpoint, _anthropicConfig.Modelo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Anthropic");
                throw;
            }
        }
        

        /// <summary>
        /// Inicializa Google resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeGoogleAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando Google...");

                if (_googleConfig.UseSecretsManager && !string.IsNullOrEmpty(_googleConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de Google desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_googleConfig.NombreDelSecreto, cancellationToken);

                    // Obtener instance
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveInstance))
                    {
                        var instance = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveInstance);
                        if (!string.IsNullOrEmpty(instance))
                        {
                            _googleConfig.Instance = instance;
                            _logger.LogInformation("Instance de Google resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener client ID
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveClientId))
                    {
                        var clientId = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveClientId);
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            _googleConfig.ClientId = clientId;
                            _logger.LogInformation("Client ID de Google resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener client secret
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveClientSecret))
                    {
                        var clientSecret = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveClientSecret);
                        if (!string.IsNullOrEmpty(clientSecret))
                        {
                            _googleConfig.ClientSecret = clientSecret;
                            _logger.LogInformation("Client Secret de Google resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener callback path
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveCallbackPath))
                    {
                        var callbackPath = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveCallbackPath);
                        if (!string.IsNullOrEmpty(callbackPath))
                        {
                            _googleConfig.CallbackPath = callbackPath;
                            _logger.LogInformation("Callback Path de Google resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener reCAPTCHA clave sitio
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveReCaptchaClaveSitio))
                    {
                        var reCaptchaClaveSitio = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveReCaptchaClaveSitio);
                        if (!string.IsNullOrEmpty(reCaptchaClaveSitio))
                        {
                            _googleConfig.ReCaptchaClaveSitio = reCaptchaClaveSitio;
                            _logger.LogInformation("ReCAPTCHA Clave Sitio de Google resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener reCAPTCHA clave secreta
                    if (!string.IsNullOrEmpty(_googleConfig.SecretoClaveReCaptchaClaveSecreta))
                    {
                        var reCaptchaClaveSecreta = secretos.GetSecretValue(secretString, _googleConfig.SecretoClaveReCaptchaClaveSecreta);
                        if (!string.IsNullOrEmpty(reCaptchaClaveSecreta))
                        {
                            _googleConfig.ReCaptchaClaveSecreta = reCaptchaClaveSecreta;
                            _logger.LogInformation("ReCAPTCHA Clave Secreta de Google resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de Google por defecto: Instance={Instance}, ClientId={ClientId}",
                        _googleConfig.Instance, _googleConfig.ClientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Google");
                throw;
            }
        }

        /// <summary>
        /// Inicializa AutoLogin resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeAutoLoginAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando AutoLogin...");

                if (_autoLoginConfig.UseSecretsManager && !string.IsNullOrEmpty(_autoLoginConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de AutoLogin desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_autoLoginConfig.NombreDelSecreto, cancellationToken);

                    // Obtener secret keys (array)
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveSecretKeys))
                    {
                        var secretKeysValue = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveSecretKeys);
                        if (!string.IsNullOrEmpty(secretKeysValue))
                        {
                            // Asumimos que las secret keys están separadas por comas
                            _autoLoginConfig.SecretKeys = secretKeysValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToArray();
                            _logger.LogInformation("Secret Keys de AutoLogin resueltas correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener endpoint
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveEndPoint))
                    {
                        var endpoint = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveEndPoint);
                        if (!string.IsNullOrEmpty(endpoint))
                        {
                            _autoLoginConfig.EndPoint = endpoint;
                            _logger.LogInformation("Endpoint de AutoLogin resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener account name
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveAccountName))
                    {
                        var accountName = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveAccountName);
                        if (!string.IsNullOrEmpty(accountName))
                        {
                            _autoLoginConfig.AccountName = accountName;
                            _logger.LogInformation("Account Name de AutoLogin resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener account key
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveAccountKey))
                    {
                        var accountKey = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveAccountKey);
                        if (!string.IsNullOrEmpty(accountKey))
                        {
                            _autoLoginConfig.AccountKey = accountKey;
                            _logger.LogInformation("Account Key de AutoLogin resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener storage connection string
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveStorageConnectionString))
                    {
                        var storageConnectionString = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveStorageConnectionString);
                        if (!string.IsNullOrEmpty(storageConnectionString))
                        {
                            _autoLoginConfig.StorageConnectionString = storageConnectionString;
                            _logger.LogInformation("Storage Connection String de AutoLogin resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener table name
                    if (!string.IsNullOrEmpty(_autoLoginConfig.SecretoClaveTableName))
                    {
                        var tableName = secretos.GetSecretValue(secretString, _autoLoginConfig.SecretoClaveTableName);
                        if (!string.IsNullOrEmpty(tableName))
                        {
                            _autoLoginConfig.TableName = tableName;
                            _logger.LogInformation("Table Name de AutoLogin resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de AutoLogin por defecto: EndPoint={EndPoint}, AccountName={AccountName}",
                        _autoLoginConfig.EndPoint, _autoLoginConfig.AccountName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando AutoLogin");
                throw;
            }
        }

        /// <summary>
        /// Inicializa S3 resolviendo la configuración desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeS3Async(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando S3...");

                if (_s3Config.UseSecretsManager && !string.IsNullOrEmpty(_s3Config.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de S3 desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_s3Config.NombreDelSecreto, cancellationToken);

                    // Obtener bucket
                    if (!string.IsNullOrEmpty(_s3Config.SecretoClaveBucket))
                    {
                        var bucket = secretos.GetSecretValue(secretString, _s3Config.SecretoClaveBucket);
                        if (!string.IsNullOrEmpty(bucket))
                        {
                            _s3Config.Bucket = bucket;
                            _logger.LogInformation("Bucket de S3 resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener access key ID
                    if (!string.IsNullOrEmpty(_s3Config.SecretoClaveAccessKeyId))
                    {
                        var accessKeyId = secretos.GetSecretValue(secretString, _s3Config.SecretoClaveAccessKeyId);
                        if (!string.IsNullOrEmpty(accessKeyId))
                        {
                            _s3Config.AccessKeyId = accessKeyId;
                            _logger.LogInformation("Access Key ID de S3 resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener cloudfront distribution ID
                    if (!string.IsNullOrEmpty(_s3Config.SecretoClaveCloudfrontDistributionId))
                    {
                        var cloudfrontDistributionId = secretos.GetSecretValue(secretString, _s3Config.SecretoClaveCloudfrontDistributionId);
                        if (!string.IsNullOrEmpty(cloudfrontDistributionId))
                        {
                            _s3Config.CloudfrontDistributionId = cloudfrontDistributionId;
                            _logger.LogInformation("CloudFront Distribution ID de S3 resuelto correctamente desde Secrets Manager");
                        }
                    }

                    // Obtener secret access key
                    if (!string.IsNullOrEmpty(_s3Config.SecretoClaveSecretAccessKey))
                    {
                        var secretAccessKey = secretos.GetSecretValue(secretString, _s3Config.SecretoClaveSecretAccessKey);
                        if (!string.IsNullOrEmpty(secretAccessKey))
                        {
                            _s3Config.SecretAccessKey = secretAccessKey;
                            _logger.LogInformation("Secret Access Key de S3 resuelto correctamente desde Secrets Manager");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de S3 por defecto: Bucket={Bucket}, AccessKeyId={AccessKeyId}",
                        _s3Config.Bucket, _s3Config.AccessKeyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando S3");
                throw;
            }
        }

        
        /// <summary>
        /// Inicializa Redis resolviendo el host desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeRedisAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando Redis...");

                if (_redisConfig.UseSecretsManager && !string.IsNullOrEmpty(_redisConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo configuración de Redis desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_redisConfig.NombreDelSecreto, cancellationToken);

                    // Obtener host
                    if (!string.IsNullOrEmpty(_redisConfig.SecretoClaveHost))
                    {
                        var redisHost = secretos.GetSecretValue(secretString, _redisConfig.SecretoClaveHost);
                        if (!string.IsNullOrEmpty(redisHost))
                        {
                            _redisConfig.Host = redisHost;
                            _logger.LogInformation("Host de Redis resuelto correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver el host de Redis desde Secrets Manager, usando configuración por defecto");
                        }
                    }

                    // Obtener configuración SSL
                    if (!string.IsNullOrEmpty(_redisConfig.SecretoClaveUsarSSL))
                    {
                        var usarSSLValue = secretos.GetSecretValue(secretString, _redisConfig.SecretoClaveUsarSSL);
                        if (bool.TryParse(usarSSLValue, out bool usarSSL))
                        {
                            _redisConfig.UsarSSL = usarSSL;
                            _logger.LogInformation("Configuración SSL de Redis resuelta correctamente desde Secrets Manager: {SSL}", usarSSL);
                        }
                    }

                    // Obtener timeout
                    if (!string.IsNullOrEmpty(_redisConfig.SecretoClaveSegundosTimeout))
                    {
                        var timeoutValue = secretos.GetSecretValue(secretString, _redisConfig.SecretoClaveSegundosTimeout);
                        if (int.TryParse(timeoutValue, out int timeout))
                        {
                            _redisConfig.SegundosTimeout = timeout;
                            _logger.LogInformation("Timeout de Redis resuelto correctamente desde Secrets Manager: {Timeout}", timeout);
                        }
                    }

                    // Obtener connect timeout
                    if (!string.IsNullOrEmpty(_redisConfig.SecretoClaveConnectTimeout))
                    {
                        var connectTimeoutValue = secretos.GetSecretValue(secretString, _redisConfig.SecretoClaveConnectTimeout);
                        if (int.TryParse(connectTimeoutValue, out int connectTimeout))
                        {
                            _redisConfig.ConnectTimeout = connectTimeout;
                            _logger.LogInformation("Connect timeout de Redis resuelto correctamente desde Secrets Manager: {ConnectTimeout}", connectTimeout);
                        }
                    }

                    // Obtener sync timeout
                    if (!string.IsNullOrEmpty(_redisConfig.SecretoClaveSyncTimeout))
                    {
                        var syncTimeoutValue = secretos.GetSecretValue(secretString, _redisConfig.SecretoClaveSyncTimeout);
                        if (int.TryParse(syncTimeoutValue, out int syncTimeout))
                        {
                            _redisConfig.SyncTimeout = syncTimeout;
                            _logger.LogInformation("Sync timeout de Redis resuelto correctamente desde Secrets Manager: {SyncTimeout}", syncTimeout);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de Redis por defecto: Host={Host}, SSL={SSL}, Timeout={Timeout}",
                        _redisConfig.Host, _redisConfig.UsarSSL, _redisConfig.SegundosTimeout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando Redis");
                throw;
            }
        }

        /// <summary>
        /// Inicializa SQS resolviendo las URLs de las colas
        /// </summary>
        private async Task InitializeSqsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando URLs de SQS...");

                // Crear un CancellationTokenSource con timeout para cada operación
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1)); // 1 minuto por operación

                // Resolver URLs para ColasSerieTipo
                if (_sqsConfig.ColasSerieTipo != null)
                {
                    var requestAlta = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasSerieTipo.ColaPrioridadAlta };
                    var responseAlta = await _sqsClient.GetQueueUrlAsync(requestAlta, cts.Token);
                    _sqsConfig.ColasSerieTipo.UrlAbsolutaColaPrioridadAlta = responseAlta.QueueUrl;

                    var requestMedia = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasSerieTipo.ColaPrioridadMedia };
                    var responseMedia = await _sqsClient.GetQueueUrlAsync(requestMedia, cts.Token);
                    _sqsConfig.ColasSerieTipo.UrlAbsolutaColaPrioridadMedia = responseMedia.QueueUrl;

                    var requestBaja = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasSerieTipo.ColaPrioridadBaja };
                    var responseBaja = await _sqsClient.GetQueueUrlAsync(requestBaja, cts.Token);
                    _sqsConfig.ColasSerieTipo.UrlAbsolutaColaPrioridadBaja = responseBaja.QueueUrl;

                    _logger.LogInformation("URLs de ColasSerieTipo resueltas");
                }

                // Resolver URLs para ColasRepresentacion
                if (_sqsConfig.ColasRepresentacion != null)
                {
                    var requestAlta = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasRepresentacion.ColaPrioridadAlta };
                    var responseAlta = await _sqsClient.GetQueueUrlAsync(requestAlta, cts.Token);
                    _sqsConfig.ColasRepresentacion.UrlAbsolutaColaPrioridadAlta = responseAlta.QueueUrl;

                    var requestMedia = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasRepresentacion.ColaPrioridadMedia };
                    var responseMedia = await _sqsClient.GetQueueUrlAsync(requestMedia, cts.Token);
                    _sqsConfig.ColasRepresentacion.UrlAbsolutaColaPrioridadMedia = responseMedia.QueueUrl;

                    var requestBaja = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasRepresentacion.ColaPrioridadBaja };
                    var responseBaja = await _sqsClient.GetQueueUrlAsync(requestBaja, cts.Token);
                    _sqsConfig.ColasRepresentacion.UrlAbsolutaColaPrioridadBaja = responseBaja.QueueUrl;

                    _logger.LogInformation("URLs de ColasRepresentacion resueltas");
                }

                // Resolver URLs para ColasPlantillaWeb
                if (_sqsConfig.ColasPlantillaWeb != null)
                {
                    var requestAlta = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasPlantillaWeb.ColaPrioridadAlta };
                    var responseAlta = await _sqsClient.GetQueueUrlAsync(requestAlta, cts.Token);
                    _sqsConfig.ColasPlantillaWeb.UrlAbsolutaColaPrioridadAlta = responseAlta.QueueUrl;

                    var requestMedia = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasPlantillaWeb.ColaPrioridadMedia };
                    var responseMedia = await _sqsClient.GetQueueUrlAsync(requestMedia, cts.Token);
                    _sqsConfig.ColasPlantillaWeb.UrlAbsolutaColaPrioridadMedia = responseMedia.QueueUrl;

                    var requestBaja = new Amazon.SQS.Model.GetQueueUrlRequest { QueueName = _sqsConfig.ColasPlantillaWeb.ColaPrioridadBaja };
                    var responseBaja = await _sqsClient.GetQueueUrlAsync(requestBaja, cts.Token);
                    _sqsConfig.ColasPlantillaWeb.UrlAbsolutaColaPrioridadBaja = responseBaja.QueueUrl;

                    _logger.LogInformation("URLs de ColasPlantillaWeb resueltas");
                }

                _logger.LogInformation("Todas las URLs de SQS resueltas correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando SQS");
                throw;
            }
        }

        /// <summary>
        /// Inicializa CDN resolviendo las credenciales desde Secrets Manager si es necesario
        /// </summary>
        private async Task InitializeCdnAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Inicializando CDN...");

                if (_cdnConfig.UseSecretsManager && !string.IsNullOrEmpty(_cdnConfig.NombreDelSecreto))
                {
                    _logger.LogInformation("Resolviendo credenciales de CDN desde Secrets Manager...");

                    // Obtener el logger específico para Secretos del contenedor de servicios
                    var secretosLogger = _serviceProviderTemporal.GetRequiredService<ILogger<Secretos.Secretos>>();
                    var secretos = new Secretos.Secretos(_secretsManager, secretosLogger);

                    // Obtener el secret completo una sola vez
                    string secretString = await secretos.GetSecretAsync(_cdnConfig.NombreDelSecreto, cancellationToken);

                    // Obtener ClientId
                    if (!string.IsNullOrEmpty(_cdnConfig.SecretoClaveClientId))
                    {
                        string clientId = secretos.GetSecretValue(secretString, _cdnConfig.SecretoClaveClientId);
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            _cdnConfig.ClientId = clientId;
                            _logger.LogInformation("ClientId de CDN resuelto correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver el ClientId de CDN desde Secrets Manager, usando configuración por defecto");
                        }
                    }

                    // Obtener ClientSecret
                    if (!string.IsNullOrEmpty(_cdnConfig.SecretoClaveClientSecret))
                    {
                        string clientSecret = secretos.GetSecretValue(secretString, _cdnConfig.SecretoClaveClientSecret);
                        if (!string.IsNullOrEmpty(clientSecret))
                        {
                            _cdnConfig.ClientSecret = clientSecret;
                            _logger.LogInformation("ClientSecret de CDN resuelto correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver el ClientSecret de CDN desde Secrets Manager, usando configuración por defecto");
                        }
                    }

                    // Obtener CompanyId
                    if (!string.IsNullOrEmpty(_cdnConfig.SecretoClaveCompanyId))
                    {
                        string companyId = secretos.GetSecretValue(secretString, _cdnConfig.SecretoClaveCompanyId);
                        if (!string.IsNullOrEmpty(companyId))
                        {
                            _cdnConfig.CompanyId = companyId;
                            _logger.LogInformation("CompanyId de CDN resuelto correctamente desde Secrets Manager");
                        }
                        else
                        {
                            _logger.LogWarning("No se pudo resolver el CompanyId de CDN desde Secrets Manager, usando configuración por defecto");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Usando configuración de CDN por defecto: ClientId={ClientId}, CompanyId={CompanyId}", 
                        _cdnConfig.ClientId, _cdnConfig.CompanyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inicializando CDN");
                throw;
            }
        }
    }
}