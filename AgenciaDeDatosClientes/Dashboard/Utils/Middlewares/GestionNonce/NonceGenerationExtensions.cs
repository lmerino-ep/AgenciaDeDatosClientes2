namespace Utils.Middlewares.GestionNonce
{
    public static class NonceGenerationExtensions
    {
        public static IApplicationBuilder UseNonceGeneration(this IApplicationBuilder app)
        {
            return app.UseMiddleware<NonceGenerationMiddleware>();
        }
    }
}
