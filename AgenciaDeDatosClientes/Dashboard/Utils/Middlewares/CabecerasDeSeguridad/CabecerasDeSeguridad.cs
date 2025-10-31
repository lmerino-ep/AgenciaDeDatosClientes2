using System.Security.Cryptography;

namespace Dashboard.Utils.Middlewares.CabecerasDeSeguridad
{
    public class CabecerasDeSeguridad
    {
        private readonly RequestDelegate _next;
        private readonly CabecerasDeSeguridadPolicy _policy;

        public CabecerasDeSeguridad(RequestDelegate next, CabecerasDeSeguridadPolicy policy)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            _next = next;
            _policy = policy;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var response = context.Response;

            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var headers = response.Headers;

            foreach (var headerValuePair in _policy.SetHeaders)
            {
                if (headerValuePair.Key == "Content-Security-Policy")
                {
                    // No creamos uno, en su lugar, usamos el nonce ya generado en el middleware anterior
                    var nonce = context.Items["CSP_NONCE"]?.ToString();

                    if (string.IsNullOrEmpty(nonce))
                    {
                        // Fallback si no se generó (no debería pasar)
                        var nonceBytes = new byte[32];
                        RandomNumberGenerator.Fill(nonceBytes);
                        nonce = Convert.ToBase64String(nonceBytes);
                    }

                    headers[headerValuePair.Key] = string.Format(headerValuePair.Value, nonce);
                    //headers["X-CSP"] = nonce; //Generamos y guardamos en el contexto, así no añadimos una cabecera a todas las requests
                }
                else
                {
                    headers[headerValuePair.Key] = headerValuePair.Value;
                }
            }

            foreach (var header in _policy.RemoveHeaders)
            {
                headers.Remove(header);
            }

            await _next(context);
        }
    }
}
