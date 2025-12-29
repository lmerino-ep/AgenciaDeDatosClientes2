using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Utils.Middlewares;
using Utils.Middlewares.CabecerasDeSeguridad;
using Utils.Middlewares.ProxyHeaders;
using Utils.Middlewares.CdnHeaders;
using Utils.Middlewares.RequestTiming;
using Utils.Middlewares.GlobalExceptionHandler;
using Utils.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Linq;
using Utils.Servicios;
using Utils.Middlewares.GestionNonce;
using Utils.Middlewares.ResponseCompressionFix;

namespace Utils.Servicios
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

            // 4. Configuración de ForwardedHeaders para AWS ALB y Transparent CDN
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

            // 5. NONCE GENERATION
            app.UseNonceGeneration();

            // 6. Middleware de cabeceras de seguridad
            app.UsarCabecerasDeSeguridad(new CabecerasDeSeguridadBuilder().AddDefaultSecurePolicy());

            // 7. Middleware de compresión Brotli y Gzip optimizado (DEBE ir ANTES que CDN)
            app.UseResponseCompression();

            // 7.5. Middleware de corrección de compresión para respuestas 304 (DEBE ir DESPUÉS de compresión)
            app.UseResponseCompressionFix();

            // 8. Middleware de cabeceras de CDN (Transparent) - Optimizado para producción
            app.UsarCabecerasDeCdn(new CdnHeadersBuilder()
                .ParaTransparentCdn()
                .HabilitarCabecerasCache(true)
                .HabilitarCabecerasSeguridad(true)
                .HabilitarCabecerasRendimiento(true)
                .HabilitarCabecerasMonitoreo(true)
                .HabilitarCabecerasCors(true)
                .ConGeolocalizacion(true)
                .ConCacheInteligente(5, 30, 15)); // 5min dinámico, 30 días estático, 15min moderado

            // ===== MANEJO DE EXCEPCIONES =====
            // UseExceptionHandler: Captura TODAS las excepciones no controladas
            // Incluye excepciones de: Middlewares, Razor Pages, Controllers, etc.
            app.UseExceptionHandler("/error/500");
            
            // ===== MANEJO DE CÓDIGOS DE ESTADO HTTP =====
            // UseStatusCodePagesWithReExecute: Solo para códigos de estado que NO son excepciones
            // Ejemplo: 404 Not Found, 403 Forbidden, etc.
            app.UseStatusCodePagesWithReExecute("/error/{0}");

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

            return app;
        }
    }
} 
