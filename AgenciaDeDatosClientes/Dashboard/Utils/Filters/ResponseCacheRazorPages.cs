using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using Polly.Caching;

namespace Utils.Filters
{
    public class ResponseCacheRazorPages : IAsyncPageFilter
    {
        private readonly string _cacheProfileName;
        private readonly int _duration;
        private readonly ResponseCacheLocation _location;
        private readonly bool _noStore;
        private readonly string _varyByHeader;

        public ResponseCacheRazorPages(string cacheProfileName, int duration, bool noStore, string varyByHeader, ResponseCacheLocation location)
        {
            _duration = duration;
            _cacheProfileName = cacheProfileName;
            _location = location;
            _noStore = noStore;
            _varyByHeader = varyByHeader;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            //Ejecuta el onGet de la página Razor:
            var resultContext = await next();

            if (resultContext.Result != null && resultContext.Result is PageResult)
            {
                var response = context.HttpContext.Response;
                switch (_cacheProfileName)
                {
                    case "Public":                        
                        response.Headers["Cache-Control"] = $"public, max-age={_duration}, s-maxage={_duration}, stale-while-revalidate=60, stale-if-error=7200";
                        break;
                    case "NoCache":
                        response.Headers["Cache-Control"] = "no-cache, no-store";
                        break;
                    case "Private":
                        response.Headers["Cache-Control"] = $"private, max-age={_duration}, stale-while-revalidate=60, stale-if-error={Math.Min(_duration, 900d)}";
                        break;
                    default:
                        throw new NotImplementedException();
                }
                
            }
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
