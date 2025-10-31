namespace Dashboard.Utils.Middlewares.CabecerasDeSeguridad
{
    public class CabecerasDeSeguridadBuilder
    {
        private readonly CabecerasDeSeguridadPolicy _policy = new CabecerasDeSeguridadPolicy();

        public const int OneYearInSeconds = 60 * 60 * 24 * 365;

        /// <summary>
        /// Add default headers in accordance with most secure approach
        /// </summary>
        public CabecerasDeSeguridadBuilder AddDefaultSecurePolicy()
        {
            //AddFrameOptionsDeny(); //No la activamos porque nuestro código de inserción es un iframe
            AddXssProtectionBlock();
            AddContentTypeOptionsNoSniff();
            AddStrictTransportSecurityMaxAge();
            AddPermissionsPolicyBrowsingTopics();
            AddContentSecurityPolicy();
            RemoveServerHeader();
            RemovePoweredByHeader();

            return this;
        }

        public CabecerasDeSeguridadBuilder AddContentSecurityPolicy()
        {
            //https://csp-evaluator.withgoogle.com/

            ////Lo más seguro, peor ojo: rompe scripts necesarios.ini
            //string sDefault = "default-src 'self'";
            //string sConnect = "connect-src 'self' *.google.com *.google-analytics.com *.analytics.google.com *.googletagmanager.com *.doubleclick.net *.googlesyndication.com csi.gstatic.com *.sensic.net";
            //string sFrame = "frame-src 'self' *.doubleclick.net *.googlesyndication.com *.google.com *.sensic.net";
            //string sFuentes = "font-src 'self' fonts.gstatic.com fonts.googleapis.com";
            //string sMedia = "media-src 'self'";
            //string sImagenes = "img-src 'self' *.analytics.google.com *.googleusercontent.com *.google-analytics.com *.googlesyndication.com *.google.com google.com *.google.es *.doubleclick.net *.googleadservices.com *.googletagmanager.com";
            //string sCss = "style-src 'self' 'unsafe-inline' fonts.googleapis.com";
            ////Añadimos unsafe-inline al final porque los navegadores que soporten nonces/hashes lo usarán, y los navegadores antiguos que no, no se romperán
            ////'strict-dynamic' overrides the 'unsafe-inline' action and disables whitelisting of host-based sources, including the 'self' token
            //string sScripts = "script-src 'nonce-{0}' 'strict-dynamic' 'self' *.googleadservices.com www.google-analytics.com ssl.google-analytics.com ajax.googleapis.com *.googletagmanager.com *.googlesyndication.com *.google.es cdn.ampproject.org *.google.com d3js.org *.doubleclick.net *.sensic.net 'unsafe-inline'";
            //string sWorkers = "worker-src 'nonce-{0}' 'self' *.googleadservices.com www.google-analytics.com ssl.google-analytics.com ajax.googleapis.com *.googletagmanager.com *.googlesyndication.com *.google.es cdn.ampproject.org *.google.com d3js.org *.doubleclick.net *.sensic.net";
            //string sObjectYEmbed = "object-src 'none'";

            ////No añadimos require-trusted-types-for 'script'; porque rompe el script de doubleclick para la publi
            //string sPolicy = $"base-uri 'self'; require-trusted-types-for 'script'; {sDefault}; {sConnect}; {sFrame}; {sFuentes}; {sCss}; {sMedia}; {sImagenes}; {sScripts}; {sWorkers}; {sObjectYEmbed}";
            //_policy.SetHeaders["Content-Security-Policy"] = sPolicy;
            //return this;
            ////Lo más seguro, peor ojo: rompe scripts necesarios.fin

            //Lo más seguro pero sin romper el app insights ni los anuncios, ni el analytics, ni el script de gfk ni empeorar el pagespeed.ini
            //Dominios de Google para anuncios y servicios - Lista específica y segura para evitar errores CSP
            //Incluimos dominios locales de Google para evitar errores en diferentes regiones (google.nl, google.es, google.fr, etc.)
            string googleDomains = "*.google.com *.google.nl *.google.es *.google.fr *.google.de *.google.co.uk *.google.it *.google.pt";
            string googleAdServices = "*.googleadservices.com *.googlesyndication.com *.googletagmanager.com *.google-analytics.com *.analytics.google.com";
            string googleOtherServices = "*.doubleclick.net *.googletagservices.com *.gstatic.com *.googleusercontent.com *.googleapis.com";
            string googleAdTraffic = "*.adtrafficquality.google";
            
            string sDefault = "default-src 'self'";
            string sConnect = $"connect-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} *.sensic.net *.console.aws.amazon.com cdnjs.cloudflare.com *.komoot.io";
            string sFrame = $"frame-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} *.sensic.net *.flourish.studio flo.uri.sh *.jwplayer.com *.infogram.com datawrapper.dwcdn.net *.carto.com carto.com";
            string sFenceFrame = $"fenced-frame-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} *.sensic.net *.flourish.studio flo.uri.sh *.jwplayer.com *.infogram.com datawrapper.dwcdn.net *.carto.com carto.com";
            string sFuentes = "font-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com fonts.gstatic.com fonts.googleapis.com cdnjs.cloudflare.com";
            string sMedia = "media-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com";
            //Para no perjudicar al pagespeed con errores de consola, admitimos todas las imágenes cargadas con el contenido en base64 en el src habilitando toda url del schema data:, aunque es menos seguro
            string sImagenes = $"img-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} cdnjs.cloudflare.com *.europapress.es *.flourish.studio *.mapbox.com *.cartocdn.com *.arcgisonline.com *.jwplayer.com data:";
            string sCss = "style-src 'self' 'unsafe-inline' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com fonts.googleapis.com cdnjs.cloudflare.com";
            //Añadimos unsafe-inline al final porque los navegadores que soporten nonces/hashes lo usarán, y los navegadores antiguos que no, no se romperán
            //no ponemos el strict-dynamic y ponemos la lista de dominios, porque aunque strict-dynamic extiende a los scripts dependientes del original el nonce, algunos pueden seguir bloqueándose, como le pasa al dominio cdn.ampproject.org
            //script-src se usará si no se proporcionan script-src-elem ni script-src-attr
            string sScripts = $"script-src 'nonce-{{0}}' 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} www.google-analytics.com ssl.google-analytics.com ajax.googleapis.com cdn.ampproject.org d3js.org *.sensic.net cdnjs.cloudflare.com *.console.aws.amazon.com 'unsafe-inline'";
            //Permitimos los event handler attributes porque nos hacen falta para los link rel preload as css para SEO
            string sScriptsEventHandlerAttributes = "script-src-attr 'unsafe-inline'";
            string sScriptsEnTagsScript = $"script-src-elem 'nonce-{{0}}' 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} www.google-analytics.com ssl.google-analytics.com ajax.googleapis.com cdn.ampproject.org d3js.org *.sensic.net cdnjs.cloudflare.com *.console.aws.amazon.com 'unsafe-inline'";
            string sWorkers = $"worker-src 'nonce-{{0}}' 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com {googleDomains} {googleAdServices} {googleOtherServices} {googleAdTraffic} ajax.googleapis.com cdn.ampproject.org d3js.org *.sensic.net";
            string sObjectYEmbed = "object-src 'self' *.epdata.es static01-europapress-net.s3.eu-west-1.amazonaws.com *.europapress.es";

            //No añadimos require-trusted-types-for 'script'; porque rompe el script de doubleclick para la publi
            string sPolicy = $"base-uri 'self'; {sDefault}; {sConnect}; {sFrame}; {sFenceFrame}; {sFuentes}; {sCss}; {sMedia}; {sImagenes}; {sScriptsEventHandlerAttributes}; {sScriptsEnTagsScript}; {sScripts}; {sWorkers}; {sObjectYEmbed}";
            _policy.SetHeaders["Content-Security-Policy"] = sPolicy;
            return this;
            //Lo más seguro pero sin romper los anuncios, ni el analytics, ni el script de gfk ni empeorar el pagespeed.fin
        }

        public CabecerasDeSeguridadBuilder AddFrameOptionsDeny()
        {
            _policy.SetHeaders["X-Frame-Options"] = "DENY";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddFrameOptionsSameOrigin()
        {
            _policy.SetHeaders["X-Frame-Options"] = "SAMEORIGIN";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddFrameOptionsSameOrigin(string uri)
        {
            _policy.SetHeaders["X-Frame-Options"] = $"ALLOW-FROM {uri}";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddXssProtectionEnabled()
        {
            _policy.SetHeaders["X-XSS-Protection"] = "1";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddXssProtectionDisabled()
        {
            _policy.SetHeaders["X-XSS-Protection"] = "0";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddXssProtectionBlock()
        {
            _policy.SetHeaders["X-XSS-Protection"] = "1; mode=block";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddXssProtectionReport(string reportUrl)
        {
            _policy.SetHeaders["X-XSS-Protection"] = $"1; report={reportUrl}";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddPermissionsPolicyBrowsingTopics()
        {
            /*
                accelerometer=(), ambient-light-sensor=(), 
                autoplay=(), battery=(), camera=(), cross-origin-isolated=(), 
                display-capture=(), document-domain=(), encrypted-media=(), 
                execution-while-not-rendered=(), execution-while-out-of-viewport=(), 
                fullscreen=(), geolocation=(), gyroscope=(), keyboard-map=(), magnetometer=(), 
                microphone=(), midi=(), navigation-override=(), payment=(), 
                picture-in-picture=(), publickey-credentials-get=(), screen-wake-lock=(), 
                sync-xhr=(), usb=(), web-share=(), xr-spatial-tracking=()
            */

            _policy.SetHeaders["Permissions-Policy"] = $"browsing-topics=()";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddStrictTransportSecurityMaxAge(int maxAge = OneYearInSeconds)
        {
            _policy.SetHeaders["Strict-Transport-Security"] = $"max-age={maxAge}";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddStrictTransportSecurityMaxAgeIncludeSubDomains(int maxAge = OneYearInSeconds)
        {
            _policy.SetHeaders["Strict-Transport-Security"] = $"max-age={maxAge}; includeSubDomains";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddStrictTransportSecurityNoCache()
        {
            _policy.SetHeaders["Strict-Transport-Security"] = $"max-age=0";
            return this;
        }

        public CabecerasDeSeguridadBuilder AddContentTypeOptionsNoSniff()
        {
            _policy.SetHeaders["X-Content-Type-Options"] = "nosniff";
            return this;
        }

        public CabecerasDeSeguridadBuilder RemoveServerHeader()
        {
            _policy.RemoveHeaders.Add("Server");
            return this;
        }

        public CabecerasDeSeguridadBuilder RemovePoweredByHeader()
        {
            _policy.RemoveHeaders.Add("X-Powered-By");
            return this;
        }

        public CabecerasDeSeguridadBuilder AddCustomHeader(string header, string value)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(nameof(header));
            }

            _policy.SetHeaders[header] = value;
            return this;
        }

        public CabecerasDeSeguridadBuilder RemoveHeader(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentNullException(nameof(header));
            }

            _policy.RemoveHeaders.Add(header);
            return this;
        }

        public CabecerasDeSeguridadPolicy Build()
        {
            return _policy;
        }
    }
}
