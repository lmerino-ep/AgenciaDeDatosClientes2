using AutoLogin;
using AutoLogin.Generators;
using AutoLogin.Url;
using AutoLogin.Url.Descriptors;
using Microsoft.AspNetCore.WebUtilities;

namespace Utils.Autologin
{
    // Genera URLs /auth?t=…&returnUrl=… hacia epdata.es, simétrico a CreadorEnlaceAutologin de EpData (hacia Api/Edit/Clientes).
    public class CreadorEnlaceAutologin
    {
        private readonly Modelos.InyeccionDeDependencias.AutoLogin _configuracion;
        private readonly Modelos.InyeccionDeDependencias.Configuracion _configuracionApp;
        private readonly ILogger<CreadorEnlaceAutologin> _logger;

        public CreadorEnlaceAutologin(
            Modelos.InyeccionDeDependencias.AutoLogin configuracion,
            Modelos.InyeccionDeDependencias.Configuracion configuracionApp,
            ILogger<CreadorEnlaceAutologin> logger)
        {
            _configuracion = configuracion;
            _configuracionApp = configuracionApp;
            _logger = logger;
        }

        public string ObtenerUrlAutologinHaciaEpData(int fkUsuario, string returnUrl)
        {
            var baseUrl = (_configuracionApp.UrlBasePublicacionEpData ?? "https://www.epdata.es").TrimEnd('/');

            if (fkUsuario <= 0)
            {
                return $"{baseUrl}/";
            }

            var token = GenerarParametroToken(fkUsuario);
            if (string.IsNullOrWhiteSpace(token))
            {
                return $"{baseUrl}/";
            }

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = "/";
            }

            var auth = QueryHelpers.AddQueryString($"{baseUrl}/auth", "t", token);
            return QueryHelpers.AddQueryString(auth, "returnUrl", returnUrl);
        }

        private string? GenerarParametroToken(int fkUsuario)
        {
            try
            {
                var secretKey = _configuracion.SecretKeys?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    _logger.LogWarning("AutoLogin SecretKeys está vacío; no se puede generar token con EpDataUrlDescriptor.");
                    return null;
                }

                var descriptor = new EpDataUrlDescriptor
                {
                    Expires = DateTime.UtcNow.AddMinutes(2d),
                    Issuer = Issuer.EuropaPressEs,
                    SecretKey = secretKey,
                    UserId = fkUsuario
                };

                string urlGenerada = new UrlHandler(new UrlGenerator()).CreateUrl(descriptor);
                if (string.IsNullOrWhiteSpace(urlGenerada) || !Uri.TryCreate(urlGenerada, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("UrlHandler.CreateUrl no devolvió una URL absoluta válida: {Url}", urlGenerada);
                    return null;
                }

                var query = QueryHelpers.ParseQuery(uri.Query);
                if (!query.TryGetValue("t", out var valoresT) || valoresT.Count == 0 || string.IsNullOrWhiteSpace(valoresT[0]))
                {
                    _logger.LogWarning("La URL generada no contiene el parámetro t: {Url}", urlGenerada);
                    return null;
                }

                return valoresT[0];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generando token AutoLogin con EpDataUrlDescriptor.");
                return null;
            }
        }
    }
}
