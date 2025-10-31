using System.Security.Cryptography;

namespace Dashboard.Utils.Middlewares.GestionNonce
{
    public class NonceGenerationMiddleware
    {
        private readonly RequestDelegate _next;

        public NonceGenerationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Items.ContainsKey("CSP_NONCE"))
            {
                var nonceBytes = new byte[32];
                RandomNumberGenerator.Fill(nonceBytes);
                var nonce = Convert.ToBase64String(nonceBytes);
                context.Items["CSP_NONCE"] = nonce;
            }
            await _next(context);
        }
    }
}
