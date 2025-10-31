using Dashboard.Controllers;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;

namespace Dashboard.Utils.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ETagFilterRazorPages : ActionFilterAttribute, IAsyncPageFilter
    {
        private void AntesDelOnGetDeLaPaginaRazor(PageHandlerExecutingContext context)
        {

        }
        private void DespuesDelOnGetDeLaPaginaRazor(PageHandlerExecutingContext context)
        {
            if (context?.HandlerInstance != null)
            {
                //Casteamos a dynamic porque no queremos usar reflexion para ver qué página razor es
                ETag eTag = ((dynamic)context.HandlerInstance).ETag;

                if (eTag != null)
                {
                    var request = context.HttpContext.Request;
                    var response = context.HttpContext.Response;

                    if (
                        request.Method == HttpMethod.Get.Method &&
                        //response.StatusCode == (int)HttpStatusCode.OK && //Aquí la respuesta de la página razor aún no se ha generado, porque lo que será siempre 200 (por defecto)
                        eTag.Devolver304(request)
                    )
                    {
                        response.StatusCode = (int)HttpStatusCode.NotModified;
                        response.ContentLength = 0; //Importante: hacemos esto antes de establecer a vacío el contenido
                        //context.Result = new EmptyResult(); //Devolvemos body vacío
                        context.Result = new StatusCodeResult(304);

                        //Como devolvemos body vacío, el html cacheado en el navegador del cliente tendrá un nonce que no es el nuestro. 
                        //Asumimos que si la petición 200 que cacheó era legítima y pasó nuestra política de CSP, entonces podemos dejar la 304 sin enviar CSP:
                        response.Headers.Remove("Content-Security-Policy");
                    }

                    if (eTag.LastModified != null)
                    {
                        // Añadimos siempre la etiqueta de ETag en la cabecera de la respuesta
                        response.Headers.Add(HeaderNames.LastModified, new[] { ETag.GetEtagResponse((DateTime)eTag.LastModified) });
                    }
                }
            }
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            //Antes de ejecutar el onget de la página razor.ini
            //AntesDelOnGetDeLaPaginaRazor(context);
            //Antes de ejecutar el onget de la página razor.fin
            //Ejecución del onget de la página razor.ini
            await next.Invoke();
            //Ejecución del onget de la página razor.fin
            //Después de ejecutar el onget de la página razor.ini
            DespuesDelOnGetDeLaPaginaRazor(context);
            //Después de ejecutar el onget de la página razor.fin
        }
    }
}
