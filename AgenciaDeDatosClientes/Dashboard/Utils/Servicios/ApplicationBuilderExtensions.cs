using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dashboard.Utils.Middlewares;
using Dashboard.Utils.Middlewares.CabecerasDeSeguridad;
using Dashboard.Utils.Middlewares.ProxyHeaders;
using Dashboard.Utils.Middlewares.CdnHeaders;
using Dashboard.Utils.Middlewares.RequestTiming;
using Dashboard.Utils.Middlewares.GlobalExceptionHandler;
using Dashboard.Utils.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using System.Globalization;
using Dashboard.Utils.Servicios;
using Dashboard.Utils.Middlewares.GestionNonce;
using Dashboard.Utils.Middlewares.ResponseCompressionFix;

namespace Dashboard.Utils.Servicios
{
    /// <summary>
    /// Extension methods para configurar el pipeline de la aplicación
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configura el pipeline completo de la aplicación
        /// </summary>
        public static IApplicationBuilder ConfigureApplicationPipeline(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            // ===== CONFIGURACIÓN DE MIDDLEWARES =====
            
            // 1. Middleware de timing de requests (debe ir primero)
            app.UsarTimingDeRequests();

            // 2. Middleware de cabeceras de proxy (AWS ALB, Transparent CDN)
            app.UsarCabecerasDeProxy(new ProxyHeadersBuilder()
                .ParaTransparentCdn()
                .ValidarIPsConfiables(false)  // Solo validación de IPs del ALB
                .DetectarGeolocalizacion(false)
                .LogIPsNoConfiables(false));  // Logging para seguridad

            // 3. Configuración de ForwardedHeaders para AWS ALB y Transparent CDN
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
                RequireHeaderSymmetry = false,
                ForwardedForHeaderName = "X-Forwarded-For",
                ForwardedProtoHeaderName = "X-Forwarded-Proto",
                ForwardedHostHeaderName = "X-Forwarded-Host"
            };
            
            // IMPORTANTE: Limpiar KnownNetworks y KnownProxies para permitir TODOS los proxies
            // Esto es necesario porque el ALB y Transparent CDN pueden usar diferentes IPs
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            
            app.UseForwardedHeaders(forwardedHeadersOptions);
            
            // SOLUCIÓN para eliminar puerto :443 de las URLs
            // Middleware personalizado para normalizar el Host eliminando el puerto estándar
            app.Use(async (context, next) =>
            {
                var host = context.Request.Host;
                
                // Si el host incluye un puerto, normalizarlo
                if (host.Port.HasValue)
                {
                    var scheme = context.Request.Scheme;
                    var port = host.Port.Value;
                    
                    // Eliminar puerto si es el estándar para el scheme
                    if ((scheme == "https" && port == 443) || (scheme == "http" && port == 80))
                    {
                        // Recrear el Host sin el puerto
                        context.Request.Host = new HostString(host.Host);
                    }
                }
                
                await next();
            });

            // 4. NONCE GENERATION
            app.UseNonceGeneration();

            // 5. Middleware de cabeceras de seguridad
            app.UsarCabecerasDeSeguridad(new CabecerasDeSeguridadBuilder().AddDefaultSecurePolicy());

            // 6. Middleware de compresión Brotli y Gzip optimizado (DEBE ir ANTES que CDN)
            app.UseResponseCompression();

            // 6.5. Middleware de corrección de compresión para respuestas 304 (DEBE ir DESPUÉS de compresión)
            app.UseResponseCompressionFix();

            // 7. Middleware de cabeceras de CDN (Transparent) - Optimizado para producción
            app.UsarCabecerasDeCdn(new CdnHeadersBuilder()
                .ParaTransparentCdn()
                .HabilitarCabecerasCache(true)
                .HabilitarCabecerasSeguridad(true)
                .HabilitarCabecerasRendimiento(true)
                .HabilitarCabecerasMonitoreo(true)
                .HabilitarCabecerasCors(true)
                .ConGeolocalizacion(true)
                .ConCacheInteligente(5, 30, 15)); // 5min dinámico, 30 días estático, 15min moderado

            // ===== MANEJO DE EXCEPCIONES (MICROSOFT BEST PRACTICES) =====
            // UseExceptionHandler: Captura TODAS las excepciones no controladas
            // Incluye excepciones de: Middlewares, Razor Pages, Controllers, etc.
            app.UseExceptionHandler("/error/500");
            
            // ===== MANEJO DE CÓDIGOS DE ESTADO HTTP =====
            // UseStatusCodePagesWithReExecute: Solo para códigos de estado que NO son excepciones
            // Ejemplo: 404 Not Found, 403 Forbidden, etc.
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            //app.UseHttpsRedirection(); //No debemos hacer esto por el loadbalancer de amazon, que conecta via http con backend

            // Configure the HTTP request pipeline.
            if (!environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            var supportedCultures = new[]
            {
                CultureInfo.InvariantCulture
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseRewriter(new RewriteOptions().Add(new RedirectWwwRule()));

            //No añadimos response caching porque no se cachea incluyendo query string, y eso en el oembed, por ejemplo, nos jugaría malas pasadas
            //app.UseResponseCaching();

            //app.UseMiddleware<IgnoreRouteMiddleware>();

            TimeSpan cachePeriod = environment.IsDevelopment() ? TimeSpan.FromSeconds(0) : TimeSpan.FromDays(30);
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".avif"] = "image/avif";

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = http =>
                {
                    http.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
                    {
                        Public = true,
                        MaxAge = cachePeriod,
                        SharedMaxAge = cachePeriod
                    };
                },
                ContentTypeProvider = provider
            });

            app.UseSession();

            // ===== CONFIGURACIÓN DE ROUTING Y ENDPOINTS =====
            app.UseRouting();

            app.UseCors("ApiPolicy");

            // ===== CONFIGURACIÓN DE AUTENTICACIÓN Y AUTORIZACIÓN =====
            app.UseAuthentication();
            app.UseAuthorization();
            //app.UseAntiforgery(); //No ponemos esto, porque si no .net nos pondrá siempre no-cache, no-store en la respuesta de toda request, públicas y privadas.

            // ===== CONFIGURACIÓN DE ENDPOINTS =====
            app.UseEndpoints(endpoints =>
            {
                // Mapear todos los controllers
                endpoints.MapControllers();
                
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}",
                    defaults: new { controller = "Home", action = "Index" }
                ).RequireHost(AllowedHosts.GetHosts());
                
                // Mapear Razor Pages
                endpoints.MapRazorPages();
            });

            //No hacemos esto porque si no .net nos pondrá siempre no-cache, no-store en la respuesta de toda request
            //// Middleware de antiforgery para endpoints no MVC/Razor Pages
            //app.Use(async (context, next) =>
            //{
            //    if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "DELETE")
            //    {
            //        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            //        await antiforgery.ValidateRequestAsync(context);
            //    }
            //    await next();
            //});

            return app;
        }

        ///// <summary>
        ///// Configura DataProtection con Redis (después de la inicialización de infraestructura)
        ///// </summary>
        //public static IApplicationBuilder ConfigureDataProtection(this IApplicationBuilder app)
        //{
        //    try
        //    {
        //        var redisConfig = app.ApplicationServices.GetRequiredService<Modelos.InyeccionDeDependencias.Redis>();
        //        var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();

        //        // DataProtection ya se configura en Program.cs con builder.Services.AddDataProtection()
        //        // Aquí solo verificamos que la conexión a Redis funciona
        //        var connection = ConnectionMultiplexer.Connect(redisConfig.Host);
        //        logger.LogInformation("DataProtection configurado correctamente con Redis");

        //        return app;
        //    }
        //    catch (Exception ex)
        //    {
        //        var logger = app.ApplicationServices.GetRequiredService<ILogger<Program>>();
        //        logger.LogError(ex, "Error configurando DataProtection con Redis, usando almacenamiento en memoria");
                
        //        // Fallback: usar almacenamiento en memoria (no recomendado para producción)
        //        // DataProtection ya se configura en Program.cs
        //        return app;
        //    }
        //}
    }
} 