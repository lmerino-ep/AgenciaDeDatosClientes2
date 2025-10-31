using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace Dashboard.Utils.Middlewares
{
    public class RedirectWwwRule : IRule
    {
        public int StatusCode { get; } = (int)HttpStatusCode.MovedPermanently;
        public bool ExcludeLocalhost { get; set; } = true;

        public void ApplyRule(RewriteContext context)
        {
            
        }
    }
}
