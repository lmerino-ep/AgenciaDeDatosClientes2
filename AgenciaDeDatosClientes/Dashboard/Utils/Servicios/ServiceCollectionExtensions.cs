using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modelos.InyeccionDeDependencias;
using Utils.Secretos;
using NugetPortable.Cache;
using NugetPortable.SQS;
using Amazon.SQS;
using Ganss.Xss;
using Utils.Servicios;
using Utils.InyeccionDeDependencias;
using Microsoft.AspNetCore.Identity;
using Amazon.CloudWatchLogs;
using Amazon.SecretsManager;
using Amazon;
using Amazon.S3.Model;
using Polly;
using Utils.Logging;
using Utils.Autenticacion;
using Microsoft.AspNetCore.Authentication.Cookies;
using Utils.Filters;
using Microsoft.AspNetCore.Mvc;
using Amazon.KinesisFirehose;
using Datos;
using Negocio;
using Utils.Authorization;

namespace Utils.Servicios
{
    /// <summary>
    /// Extension methods para configurar servicios de la aplicación
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configura la inyección de dependencias principal de la aplicación
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // ===== CONFIGURACIÓN DE DESARROLLO LOCAL =====
            var soloDesarrollo = Environment.GetEnvironmentVariable("SOLO_DURANTE_DESARROLLO");
            if (!string.IsNullOrEmpty(soloDesarrollo))
            {
                // Solo en desarrollo local, cuando no hay variable configurada
                environment.EnvironmentName = Microsoft.Extensions.Hosting.Environments.Development;
                
                // Para definir la vble de entorno en VS:
                // Botón derecho en proyecto -> propiedades -> depurar -> general -> Abrir la interfaz de perfiles de inicio de depuración ->
                // Container (Dockerfile) -> Variables de entorno:
                //   Nombre: SOLO_DURANTE_DESARROLLO
                //   Valor: True
            }

            // ===== CONFIGURACIÓN DE AWS =====
            if (environment.IsDevelopment())
            {
                var awsConfig = configuration.GetSection("AWS").Get<Modelos.InyeccionDeDependencias.Aws>();

                // En desarrollo, configurar credenciales AWS hardcodeadas    
                if (awsConfig != null && !string.IsNullOrEmpty(awsConfig.AccessKeyId) && !string.IsNullOrEmpty(awsConfig.SecretAccessKey))
                {
                    // Configurar credenciales AWS para desarrollo
                    var region = RegionEndpoint.GetBySystemName(awsConfig.Region);

                    // Configurar SQS con credenciales hardcodeadas
                    services.AddSingleton<IAmazonSQS>(provider =>
                        new AmazonSQSClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));

                    // Configurar Secrets Manager con credenciales hardcodeadas
                    services.AddSingleton<IAmazonSecretsManager>(provider =>
                        new AmazonSecretsManagerClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));

                    // Configurar CloudWatch Logs con credenciales hardcodeadas
                    services.AddSingleton<IAmazonCloudWatchLogs>(provider =>
                        new AmazonCloudWatchLogsClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
                }
                else
                {
                    throw new Exception("Las credenciales de AWS son necesarias si se quiere ejecutar en debug esta aplicación");
                }
            }
            else
            {
                // En producción, usar IAM roles
                services.AddAWSService<Amazon.SQS.IAmazonSQS>();
                services.AddAWSService<Amazon.SecretsManager.IAmazonSecretsManager>();
                services.AddAWSService<Amazon.CloudWatchLogs.IAmazonCloudWatchLogs>();
            }

            return services;
        }

        /// <summary>
        /// Configura los servicios de infraestructura (Redis, SQS, etc.)
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Servicios de infraestructura
            services.AddSingleton<ISecretos, Secretos.Secretos>();

            // Configurar Redis usando factory method con configuración ya resuelta
            services.AddSingleton<NugetPortable.Cache.IRedis>(provider =>
            {
                var redisConfig = provider.GetRequiredService<Modelos.InyeccionDeDependencias.Redis>();
                return new NugetPortable.Cache.Redis(
                    redisConfig.Host,
                    redisConfig.ConnectTimeout,
                    redisConfig.SyncTimeout,
                    redisConfig.UsarSSL
                );
            });

            // Configurar SQS usando factory method con URLs ya resueltas
            services.AddSingleton<NugetPortable.SQS.ISqs>(provider =>
            {
                var amazonSqs = provider.GetRequiredService<IAmazonSQS>();
                var sqsConfig = provider.GetRequiredService<Modelos.InyeccionDeDependencias.Sqs>();

                return new NugetPortable.SQS.Sqs(
                    amazonSqs,
                    sqsConfig.NumeroMaximoDeMensajesPorRequest,
                    sqsConfig.LimiteDeSegundosDeEsperaHastaRecibirMensajes,
                    sqsConfig.SegundosOcultoParaSucesivasRequestsDespuesDeHaberSidoConsultadoPorNuestroProceso
                );
            });

            // Servicios de CDN
            services.AddSingleton<NugetPortable.Herramientas.CDN.Transparent>(provider =>
            {
                var cdnConfig = provider.GetRequiredService<Modelos.InyeccionDeDependencias.Cdn>();
                
                // Crear instancia de Transparent con la configuración de CDN
                return new NugetPortable.Herramientas.CDN.Transparent(
                    cdnConfig.ClientId,
                    cdnConfig.ClientSecret,
                    cdnConfig.CompanyId
                );
            });

            return services;
        }

        /// <summary>
        /// Configura los servicios de utilidades (CriticalCss, HtmlSanitizer, etc.)
        /// </summary>
        public static IServiceCollection AddUtilityServices(this IServiceCollection services)
        {
            CriticalCss criticalCss = new CriticalCss(Path.GetFullPath("wwwroot"));
            services.AddSingleton(x => criticalCss);


            var sanitizerContraXSS = new HtmlSanitizer();
            services.AddSingleton(x => sanitizerContraXSS);

            return services;
        }

        /// <summary>
        /// Configura los servicios de negocio y repositorios
        /// </summary>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // ===== REPOSITORIOS =====
            services.AddSingleton<RepositorioAuditoria>();

            // ===== SERVICIOS DE NEGOCIO =====
            services.AddScoped<IServicioAuditoria, ServicioAuditoria>();

            return services;
        }

        /// <summary>
        /// Configura los servicios de autenticación y autorización
        /// </summary>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
        {
            // ===== CONFIGURACIÓN DE AUTENTICACIÓN Y AUTORIZACIÓN =====
            // Obtener Aurora config para los stores personalizados
            var auroraConfig = services.BuildServiceProvider().GetRequiredService<Modelos.InyeccionDeDependencias.Aurora>();

            var userStore = new Utils.Autenticacion.UserStore(auroraConfig);
            services.AddSingleton<Microsoft.AspNetCore.Identity.IUserStore<Modelos.Autenticacion.Usuario>>(userStore);
            services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<Modelos.Autenticacion.Usuario>>(userStore);
            services.AddSingleton<Microsoft.AspNetCore.Identity.IRoleStore<AgenciaDeDatosBD.Rol>, Utils.Autenticacion.RoleStore>();

            services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<Modelos.Autenticacion.Usuario>, Utils.Autenticacion.ClaimsPersonalizados>();

            services.AddIdentity<Modelos.Autenticacion.Usuario, AgenciaDeDatosBD.Rol>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            }).AddDefaultTokenProviders();

            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "clientesepdataCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.MaxAge = TimeSpan.FromDays(2);
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(2);
                options.SlidingExpiration = true;
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            });

            return services;
        }

        /// <summary>
        /// Configura las políticas de autorización
        /// </summary>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ConsultaMetricasOProveedorHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("EditarUsuarios", policy => policy.RequireRole("Alta/Baja/Modificación de usuarios"));
                options.AddPolicy("EditorDePlantillas", policy => policy.RequireRole("Crear y Editar Plantilla"));
                options.AddPolicy("GestionUsuarios", policy => policy.RequireRole("Consultar y buscar otros usuarios"));
                options.AddPolicy("AsociarNoticias", policy => policy.RequireRole("Asociar gráfico con noticias"));
                options.AddPolicy("IslaDeDatos", policy => policy.RequireRole("Isla de datos"));
                options.AddPolicy("EditorNoticiasAutomaticas", policy => policy.RequireRole("Editor Noticias Automáticas"));
                options.AddPolicy("GestionIntegracion", policy => policy.RequireRole("Gestión Integración"));
                options.AddPolicy("GestionSolicitudes", policy => policy.RequireRole("Consulta solicitudes: pendientes e histórico de rechazadas y resueltas"));
                options.AddPolicy("GestionFormulas", policy => policy.RequireRole("Gestión unidades y fórmulas"));
                options.AddPolicy("GestionCache", policy => policy.RequireRole("Gestión caché"));
                options.AddPolicy("GestionEntidadesCliente", policy => policy.RequireRole("Gestión entidades cliente"));
                options.AddPolicy("EditorDeHTML", policy => policy.RequireRole("Editor de HTML"));
                options.AddPolicy("AccesoContenidoAjeno", policy => policy.RequireRole("Acceso a contenido ajeno"));
                options.AddPolicy("AccesoContenidoDeEuropaPress", policy => policy.RequireRole("Acceso a contenido de Europa Press"));
                options.AddPolicy("ConsultarMensajesDeError", policy => policy.RequireRole("Consultar mensajes de error"));
                options.AddPolicy("CrearRepresentacion", policy => policy.RequireRole("Crear Representación"));
                options.AddPolicy("TramitarSolicitudes", policy => policy.RequireRole("Resolución de solicitudes para masters"));
                options.AddPolicy("EditarMonitorPublicacionesCalendario", policy =>
                {
                    policy.RequireRole("Editar Monitor Calendario de publicaciones");
                    policy.RequireRole("Consultar calendario de publicaciones");
                });
                options.AddPolicy("ConsultarMonitorPublicacionesCalendario", policy => policy.RequireRole("Consultar calendario de publicaciones"));
                options.AddPolicy("GestionarListas", policy => policy.RequireRole("Gestionar listas"));
                options.AddPolicy("AccesoPluginHerramientaInterna", policy => policy.RequireRole("Acceso a Plugin Herramienta Interna"));
                options.AddPolicy("CrearEditarPluginsYBibliotecas", policy => policy.RequireRole("Crear y Editar Plugins y Bibliotecas"));
                options.AddPolicy("EditorCMS", policy => policy.RequireRole("Editor CMS"));
                options.AddPolicy("GestionarElementosYEstructuraCMS", policy => policy.RequireRole("Gestionar elementos y estructura CMS"));

                // ===== POLÍTICAS DEL DASHBOARD DE GESTIÓN =====
                // Gestión de usuarios
                options.AddPolicy("Dashboard.GestionUsuarios", policy =>
                    policy.RequireRole("Dashboard - Gestión de Usuarios", "Dashboard - Acceso total"));

                options.AddPolicy("Dashboard.ConsultaUsuarios", policy =>
                    policy.RequireRole("Dashboard - Consulta de Usuarios", "Dashboard - Gestión de Usuarios", "Dashboard - Acceso total"));

                // Gestión de roles y tokens
                options.AddPolicy("Dashboard.GestionRoles", policy =>
                    policy.RequireRole("Dashboard - Gestión de Roles", "Dashboard - Acceso total"));

                options.AddPolicy("Dashboard.GestionTokens", policy =>
                    policy.RequireRole("Dashboard - Gestión de Tokens", "Dashboard - Acceso total"));

                // Métricas
                options.AddPolicy("Dashboard.ConsultaMetricas", policy =>
                    policy.RequireRole("Dashboard - Consulta de Métricas", "Dashboard - Exportación de Métricas", "Dashboard - Acceso total"));

                options.AddPolicy(PoliticasAutorizacion.Politicas.ConsultaMetricasOProveedor, policy =>
                    policy.Requirements.Add(new ConsultaMetricasOProveedorRequirement()));

                options.AddPolicy("Dashboard.ExportacionMetricas", policy =>
                    policy.RequireRole("Dashboard - Exportación de Métricas", "Dashboard - Acceso total"));

                // Documentación
                options.AddPolicy("Dashboard.GestionDocumentacion", policy =>
                    policy.RequireRole("Dashboard - Gestión de Documentación", "Dashboard - Acceso total"));

                // Auditoría
                options.AddPolicy("Dashboard.Auditoria", policy =>
                    policy.RequireRole("Dashboard - Auditoría", "Dashboard - Acceso total"));

                // Acceso total al dashboard (superusuario)
                options.AddPolicy("Dashboard.AccesoTotal", policy =>
                    policy.RequireRole("Dashboard - Acceso total"));
            });

            return services;
        }

        /// <summary>
        /// Configura el logging de CloudWatch para producción
        /// </summary>
        public static IServiceCollection AddCloudWatchLogging(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // ===== CONFIGURACIÓN BÁSICA DE LOGGING =====
            services.AddLogging(logging =>
            {
                // Configuración básica de logging
                logging.ClearProviders();
                logging.AddConsole();

                // Configurar nivel de log según entorno
                var logLevel = environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information;
                logging.SetMinimumLevel(logLevel);

                // ===== CONFIGURACIÓN DE AWS CLOUDWATCH LOGS =====    
                if (!environment.IsDevelopment())
                {
                    var cloudWatchConfig = configuration.GetSection("CloudWatch").Get<Modelos.InyeccionDeDependencias.CloudWatch>();
                    var region = !string.IsNullOrEmpty(cloudWatchConfig?.Region) 
                        ? Amazon.RegionEndpoint.GetBySystemName(cloudWatchConfig.Region) 
                        : Amazon.RegionEndpoint.EUWest1;
                    
                    // Configurar logging para CloudWatch usando el provider personalizado
                    logging.AddProvider(new Utils.Logging.CloudWatchLogsProvider(
                        logGroupName: cloudWatchConfig.LogGroupName,
                        region: region,
                        logLevel: logLevel
                    ));
                }
            });

            return services;
        }

        /// <summary>
        /// Configura el logging de búsquedas de usuarios a S3 via Kinesis Data Firehose
        /// </summary>
        public static IServiceCollection AddLoggingManual(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var firehoseConfig = configuration.GetSection("FirehoseLogs").Get<Modelos.InyeccionDeDependencias.FirehoseLogs>();
            if ((firehoseConfig?.Enabled ?? false) && !environment.IsDevelopment())
            {
                // Registrar el cliente de Firehose
                services.AddAWSService<IAmazonKinesisFirehose>();

                // Registrar el logger de búsquedas de usuarios
                services.AddSingleton<LoggerManual>(provider =>
                {
                    var firehoseClient = provider.GetRequiredService<IAmazonKinesisFirehose>();
                    var logger = provider.GetRequiredService<ILogger<LoggerManual>>();
                    return new LoggerManual(firehoseClient, firehoseConfig.DeliveryStreamName, logger);
                });
            }
            else
            {
                // Registrar un logger "no-op" cuando está deshabilitado
                services.AddSingleton<LoggerManual>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<LoggerManual>>();
                    return new LoggerManualDeshabilitado(logger);
                });
            }

            return services;
        }

        /// <summary>
        /// Configura MVC, RazorPages y sus convenciones
        /// </summary>
        public static IServiceCollection AddMvcAndRazorPages(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.CacheProfiles.Add("NoCache", new Microsoft.AspNetCore.Mvc.CacheProfile
                {
                    Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None,
                    NoStore = true
                });

                options.CacheProfiles.Add("Private", new Microsoft.AspNetCore.Mvc.CacheProfile
                {
                    Duration = 60,
                    Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Client,
                    NoStore = false,
                    VaryByHeader = "Accept-Encoding"
                });

                options.CacheProfiles.Add("Public", new Microsoft.AspNetCore.Mvc.CacheProfile
                {
                    Duration = 60,
                    Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
                    NoStore = false,
                    VaryByHeader = "Accept-Encoding"
                });

                options.SuppressAsyncSuffixInActionNames = false;
            });

            // Configurar routing para que todas las rutas sean minúsculas
            services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });

            services.AddRazorPages(options =>
            {                
                //Para añadir cacheprofiles es necesario aplicarlos desde un page filter (https://github.com/dotnet/aspnetcore/issues/18890#issuecomment-584290537)
                options.Conventions.AddPageApplicationModelConvention("/Index", model =>
                    model.Filters.Add(new ResponseCacheRazorPages("Public", 240, false, "Accept-Encoding", ResponseCacheLocation.Any))
                );
        }).AddMvcOptions(options =>
        {
            options.Filters.Add(new ETagFilterRazorPages()); //Se podría añadir solo el filtro a las rutas de AddRazorPages individualmente, pero lo ponemos aquí para que aplique a cualquiera
        });

            return services;
        }

        /// <summary>
        /// Configura sesiones y antiforgery
        /// </summary>
        public static IServiceCollection AddSessionAndAntiforgery(this IServiceCollection services)
        {
            //No añadimos response caching porque no se cachea incluyendo query string, y eso en el oembed, por ejemplo, nos jugaría malas pasadas
            //services.AddResponseCaching();

            services.AddAntiforgery(options => options.HeaderName = "XSRF-TOKEN");
            services.AddSession(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.Name = "session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });
            return services;
        }

        /// <summary>
        /// Configura las políticas CORS
        /// </summary>
        public static IServiceCollection AddCorsPolicies(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("ApiPolicy", builder => builder
                    .AllowAnyOrigin()
                    .WithMethods("GET")
                    .WithHeaders("accept", "content-type", "origin")
                    .SetPreflightMaxAge(TimeSpan.FromSeconds(2520)));
            });
            return services;
        }

        /// <summary>
        /// Configura los servicios de AutoLogin
        /// </summary>
        public static IServiceCollection AddAutologin(this IServiceCollection services, Modelos.InyeccionDeDependencias.AutoLogin autoLoginConfig)
        {
            services.AddScoped<AutoLogin.Token.ITokenHandler, AutoLogin.Token.TokenHandler>();
            services.AddScoped<AutoLogin.Verification.Interfaces.ITokenDbService>(i =>
                new AutoLogin.Verification.Services.TokenDbService(
                    new AutoLogin.Verification.Data.TokenRepository(
                        autoLoginConfig.StorageConnectionString,
                        autoLoginConfig.TableName
                    ),
                    new AutoLogin.Token.TokenHandler(),
                    autoLoginConfig.SecretKeys
                )
            );
            return services;
        }

        /// <summary>
        /// Transfiere servicios desde tempServiceProvider al ServiceProvider definitivo
        /// </summary>
        public static IServiceCollection TransferServicesFromTemp(this IServiceCollection services, IServiceProvider tempServiceProvider, WebApplicationBuilder builder)
        {
            // ===== TRANSFERIR CONFIGURACIONES =====
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Redis>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Sqs>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Aurora>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Zilliz>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.OpenAI>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Anthropic>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Google>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.AutoLogin>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.S3>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Configuracion>());
            //services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Api>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Cdn>());
            services.AddSingleton(tempServiceProvider.GetRequiredService<Modelos.InyeccionDeDependencias.Stripe>());

            // ===== REGISTRAR SERVICIOS AWS NUEVAMENTE =====
            // CRÍTICO: NO transferir IAmazonSQS,IAmazonSecretsManager,IAmazonCloudWatchLogs desde tempServiceProvider 
            // porque al hacer tempServiceProvider.Dispose() estos clientes quedan disposed.
            // En su lugar, registrarlos de nuevo en el contenedor principal con nuevas instancias.
            // Este código es idéntico al método AddAwsServices para mantener coherencia.
            if (builder.Environment.IsDevelopment())
            {
                var awsConfig = builder.Configuration.GetSection("AWS").Get<Modelos.InyeccionDeDependencias.Aws>();
                // En desarrollo, configurar credenciales AWS hardcodeadas  
                if (awsConfig != null && !string.IsNullOrEmpty(awsConfig.AccessKeyId) && !string.IsNullOrEmpty(awsConfig.SecretAccessKey))
                {
                    // Configurar credenciales AWS para desarrollo
                    var region = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region);
                    services.AddSingleton<Amazon.SQS.IAmazonSQS>(new Amazon.SQS.AmazonSQSClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
                    services.AddSingleton<Amazon.SecretsManager.IAmazonSecretsManager>(new Amazon.SecretsManager.AmazonSecretsManagerClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
                    services.AddSingleton<Amazon.CloudWatchLogs.IAmazonCloudWatchLogs>(new Amazon.CloudWatchLogs.AmazonCloudWatchLogsClient(awsConfig.AccessKeyId, awsConfig.SecretAccessKey, region));
                }
                else
                {
                    throw new Exception("Las credenciales de AWS son necesarias si se quiere ejecutar en debug esta aplicación");
                }
            }
            else
            {
                // En producción, usar IAM roles
                services.AddAWSService<Amazon.SQS.IAmazonSQS>();
                services.AddAWSService<Amazon.SecretsManager.IAmazonSecretsManager>();
                services.AddAWSService<Amazon.CloudWatchLogs.IAmazonCloudWatchLogs>();
            }

            // Transferir ISecretos (este servicio no tiene problemas de dispose)
            services.AddSingleton(tempServiceProvider.GetRequiredService<Utils.Secretos.ISecretos>());
            
            // ===== CONFIGURAR LOGGING =====
            // Para ILogger, es mejor configurarlo directamente en lugar de transferir
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
            });
            
            return services;
        }
    }
} 
