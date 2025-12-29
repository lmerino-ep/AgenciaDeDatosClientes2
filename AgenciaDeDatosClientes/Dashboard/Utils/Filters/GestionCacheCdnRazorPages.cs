using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Utils.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class GestionCacheCdnRazorPages : ActionFilterAttribute
    {
        //public override void OnResultExecuting(ResultExecutingContext context)
        //{
        //    base.OnResultExecuting(context);
        //}
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            //Cdn(context); //No necesitamos de momento hacer nada específico más allá de lo implementado en ProxyHeadersMiddleware
            base.OnResultExecuted(context);
        }
               
        private static void Cdn(ResultExecutedContext oResultado)
        {
            try
            {
                if (string.Compare(oResultado.HttpContext.Request.Method, "GET", StringComparison.InvariantCultureIgnoreCase) == 0 || string.Compare(oResultado.HttpContext.Request.Method, "HEAD", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (oResultado.HttpContext.User.Identity.IsAuthenticated)
                    {
                        oResultado.HttpContext.Response.Headers["Cache-Control"] = "private";
                    }
                    else
                    {
                        string sCdn = oResultado.HttpContext.Request.Headers["X-CDN"];
                        if (string.Compare(sCdn, "Cloudfront", StringComparison.InvariantCultureIgnoreCase) == 0 || string.Compare(sCdn, "StackPath", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            string sCacheControl = oResultado.HttpContext.Response.Headers["Cache-Control"];
                            bool bSoyPublic = false;
                            int iMaxAge = 0;
                            List<string> aCacheControl = new List<string>();
                            if (!String.IsNullOrEmpty(sCacheControl))
                            {
                                foreach (string sValor in sCacheControl.Split(","))
                                {
                                    if (sValor == "public")
                                    {
                                        bSoyPublic = true;
                                        aCacheControl.Add("public");
                                    }
                                    else
                                    {
                                        string[] aClaveValor = sValor.Split("=");
                                        if (aClaveValor.Count() == 2)
                                        {
                                            if (string.Compare(aClaveValor[0], "max-age", StringComparison.InvariantCultureIgnoreCase) == 0)
                                            {
                                                Int32.TryParse(aClaveValor[1], out iMaxAge);
                                            }
                                            if (string.Compare(aClaveValor[0], "s-maxage", StringComparison.InvariantCultureIgnoreCase) != 0)
                                            {
                                                aCacheControl.Add(sValor);
                                            }
                                        }
                                    }
                                }
                                if (bSoyPublic && iMaxAge > 0)
                                {
                                    aCacheControl.Add($"s-maxage={Convert.ToInt32(iMaxAge * 1.2)}");
                                    oResultado.HttpContext.Response.Headers["Cache-Control"] = String.Join(",", aCacheControl);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //No lo cacheamos nunca pues somos post, o similar
                    oResultado.HttpContext.Response.Headers["Cache-Control"] = "no-cache";
                }
            }
            catch (Exception e)
            {
                //No hacemos nada
            }
        }
    }
}
