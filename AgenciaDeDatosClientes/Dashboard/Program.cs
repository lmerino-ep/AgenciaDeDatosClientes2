using Dashboard.Utils.Servicios;
using AutoLogin.Token;
using AutoLogin.Verification.Data;
using AutoLogin.Verification.Interfaces;
using AutoLogin.Verification.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Modelos.InyeccionDeDependencias;
using StackExchange.Redis;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// ===== DEBUGGING: Verificar que se está usando el código correcto en producción.ini =====
//var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
//logger.LogInformation("=== CÓDIGO ACTUALIZADO - VERSIÓN 2025.09.22.002 ===");
// ===== DEBUGGING: Verificar que se está usando el código correcto en producción.fin =====

// 1. Configuración de servicios básicos (incluyendo AWS)
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();  //sirve para poder acceder al httpcontext desde cualquier punto de la aplicación, incluso desde el constructor de una razor page
builder.Services.AddMvcAndRazorPages();
builder.Services.AddCorsPolicies();

// ===== CONFIGURACIÓN DE COMPRESIÓN ESTÁNDAR =====
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Habilitar para HTTPS
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    
    // Usar los MIME types que tenías en tu política personalizada
    options.MimeTypes = new[]
    {
        "text/html",
        "text/css",
        "text/javascript",
        "application/javascript",
        "application/json",
        "application/xml",
        "text/xml",
        "text/plain",
        "application/x-font-ttf",
        "application/x-font-otf",
        "font/ttf",
        "font/otf",
        "image/svg+xml"
    };
});

// Configurar niveles de compresión
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // Mejor compresión
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // Mejor compresión
});

// Cargar configuración necesaria para servicios adicionales
var autoLoginConfig = builder.Configuration.GetSection("AutoLogin").Get<Modelos.InyeccionDeDependencias.AutoLogin>();

// ===== CONFIGURACIÓN DE KESTREL =====
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Usar la configuración de Kestrel del appsettings.json
    builder.Configuration.GetSection("Kestrel").Bind(serverOptions);
});

// 2. Inicialización asíncrona de infraestructura (ServiceProvider temporal)
var tempServices = new ServiceCollection();

// Cargar configuraciones desde appsettings
var redisConfig = builder.Configuration.GetSection("Redis").Get<Modelos.InyeccionDeDependencias.Redis>();
var sqsConfig = builder.Configuration.GetSection("Sqs").Get<Modelos.InyeccionDeDependencias.Sqs>();
var auroraConfig = builder.Configuration.GetSection("Aurora").Get<Modelos.InyeccionDeDependencias.Aurora>();
var zillizConfig = builder.Configuration.GetSection("Zilliz").Get<Modelos.InyeccionDeDependencias.Zilliz>();
var openAIConfig = builder.Configuration.GetSection("OpenAI").Get<Modelos.InyeccionDeDependencias.OpenAI>();
var anthropicConfig = builder.Configuration.GetSection("Anthropic").Get<Modelos.InyeccionDeDependencias.Anthropic>();
var googleConfig = builder.Configuration.GetSection("Google").Get<Modelos.InyeccionDeDependencias.Google>();
var s3Config = builder.Configuration.GetSection("S3").Get<Modelos.InyeccionDeDependencias.S3>();
var aplicacionConfig = builder.Configuration.GetSection("Configuracion").Get<Configuracion>();
var cdnConfig = builder.Configuration.GetSection("CDN").Get<Modelos.InyeccionDeDependencias.Cdn>();

// Registrar configuraciones en el ServiceProvider temporal
tempServices.AddSingleton(redisConfig);
tempServices.AddSingleton(sqsConfig);
tempServices.AddSingleton(auroraConfig);
tempServices.AddSingleton(zillizConfig);
tempServices.AddSingleton(openAIConfig);
tempServices.AddSingleton(anthropicConfig);
tempServices.AddSingleton(googleConfig);
tempServices.AddSingleton(autoLoginConfig);
tempServices.AddSingleton(s3Config);
tempServices.AddSingleton(aplicacionConfig);
tempServices.AddSingleton(cdnConfig);
tempServices.AddSingleton<Dashboard.Utils.Secretos.ISecretos, Dashboard.Utils.Secretos.Secretos>();

// Agregar servicios AWS necesarios para la inicialización
if (builder.Environment.IsDevelopment())
{
    var awsConfig = builder.Configuration.GetSection("AWS").Get<Modelos.InyeccionDeDependencias.Aws>();
    // En desarrollo, configurar credenciales AWS hardcodeadas  
    if (awsConfig != null && !string.IsNullOrEmpty(awsConfig.AccessKeyId) && !string.IsNullOrEmpty(awsConfig.SecretAccessKey))
    {
        // Configurar credenciales AWS para desarrollo
        var region = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region);
        tempServices.AddSingleton<Amazon.SQS.IAmazonSQS>(new Amazon.SQS.AmazonSQSClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
        tempServices.AddSingleton<Amazon.SecretsManager.IAmazonSecretsManager>(new Amazon.SecretsManager.AmazonSecretsManagerClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
    }
    else
    {
        throw new Exception("Las credenciales de AWS son necesarias si se quiere ejecutar en debug esta aplicación");
    }
}
else
{
    // En producción, usar IAM roles para la inicialización
    tempServices.AddAWSService<Amazon.SQS.IAmazonSQS>();
    tempServices.AddAWSService<Amazon.SecretsManager.IAmazonSecretsManager>();
}

tempServices.AddLogging(logging =>
{
    logging.SetMinimumLevel(builder.Environment.IsDevelopment() ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Information);
});

var tempServiceProvider = tempServices.BuildServiceProvider();

var initializationService = new Dashboard.Utils.Servicios.InfrastructureInitializationService(
    tempServiceProvider.GetRequiredService<Amazon.SQS.IAmazonSQS>(),
    tempServiceProvider.GetRequiredService<Amazon.SecretsManager.IAmazonSecretsManager>(),
    sqsConfig,
    redisConfig,
    auroraConfig,
    zillizConfig,
    openAIConfig,
    anthropicConfig,
    googleConfig,
    autoLoginConfig,
    s3Config,
    cdnConfig,
    tempServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Dashboard.Utils.Servicios.InfrastructureInitializationService>>(),
    tempServiceProvider,
    tempServices);

// Ejecutar inicialización de manera síncrona
await initializationService.InitializeAsync();

// ===== TRANSFERIR SERVICIOS DESDE TEMP AL DEFINITIVO =====
builder.Services.TransferServicesFromTemp(tempServiceProvider, builder);

// ===== ELIMINAMOS EL SERVICEPROVIDER TEMPORAL =====
tempServiceProvider.Dispose();

// ===== CONFIGURACIÓN DE DATAPROTECTION CON REDIS YA INICIALIZADO =====
// CRÍTICO: Esta configuración es esencial para entornos autoescalables como ECS
// CRÍTICO: DataProtection debe configurarse ANTES de AddAuthenticationServices y AddSessionAndAntiforgery
// porque estos servicios automáticamente usan DataProtection
// DataProtection se usa para encriptar cookies de autenticación, tokens antiforgery y datos de sesión
// Sin esta configuración, cada instancia de ECS tendría claves diferentes, causando:
// - Cookies de autenticación inválidas entre instancias
// - Tokens antiforgery rotos al cambiar de instancia
// - Sesiones perdidas y errores de autenticación
// Con Redis como almacén compartido, todas las instancias usan las mismas claves de encriptación

var redisConfigInicializado = builder.Services.BuildServiceProvider().GetRequiredService<Modelos.InyeccionDeDependencias.Redis>();

// 3. Configuración de DataProtection (después de inicializar secretos)
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(
        ConnectionMultiplexer.Connect(redisConfigInicializado.Host),
        "DataProtection-Keys-Clientes"
    );

// ===== CONFIGURAR SERVICIOS CON CONFIGURACIONES YA INICIALIZADAS =====
builder.Services.AddInfrastructureServices(); // Ahora usa las configuraciones correctas
builder.Services.AddUtilityServices();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddCloudWatchLogging(builder.Configuration, builder.Environment);
builder.Services.AddLoggingManual(builder.Configuration, builder.Environment);
builder.Services.AddAuthenticationServices();
builder.Services.AddSessionAndAntiforgery();
builder.Services.AddAutologin(autoLoginConfig);

// 4. Configuración de ETag para cacheo de estáticos
DateTime ahora = DateTime.UtcNow;
Dashboard.Utils.Filters.ETag._fechaActualCompilacion = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, ahora.Second);

var app = builder.Build();

// 5. Configuración del pipeline de middlewares y endpoints
app.ConfigureApplicationPipeline(builder.Environment);

app.Run();


