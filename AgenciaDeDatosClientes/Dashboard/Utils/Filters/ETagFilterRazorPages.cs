using Controllers;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;

namespace Utils.Filters
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
                try
                {
                    //Casteamos a dynamic porque no queremos usar reflexion para ver qu� p�gina razor es
                    //Usamos try-catch porque no todas las p�ginas tienen propiedad ETag (ej: Login no debe cachearse)
                    ETag eTag = ((dynamic)context.HandlerInstance).ETag;

                    if (eTag != null)
                    {
                        var request = context.HttpContext.Request;
                        var response = context.HttpContext.Response;

                        if (
                            request.Method == HttpMethod.Get.Method &&
                            //response.StatusCode == (int)HttpStatusCode.OK && //Aqu� la respuesta de la p�gina razor a�n no se ha generado, porque lo que ser� siempre 200 (por defecto)
                            eTag.Devolver304(request)
                        )
                        {
                            response.StatusCode = (int)HttpStatusCode.NotModified;
                            response.ContentLength = 0; //Importante: hacemos esto antes de establecer a vac�o el contenido
                            //context.Result = new EmptyResult(); //Devolvemos body vac�o
                            context.Result = new StatusCodeResult(304);

                            //Como devolvemos body vac�o, el html cacheado en el navegador del cliente tendr� un nonce que no es el nuestro.
                            //Asumimos que si la petici�n 200 que cache� era leg�tima y pas� nuestra pol�tica de CSP, entonces podemos dejar la 304 sin enviar CSP:
                            response.Headers.Remove("Content-Security-Policy");
                        }

                        if (eTag.LastModified != null)
                        {
                            // A�adimos siempre la etiqueta de ETag en la cabecera de la respuesta
                            response.Headers.Add(HeaderNames.LastModified, new[] { ETag.GetEtagResponse((DateTime)eTag.LastModified) });
                        }
                    }
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    // La p�gina no tiene propiedad ETag - esto es normal para p�ginas como Login
                    // No hacer nada, simplemente continuar sin ETag
                }
            }
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            //Antes de ejecutar el onget de la p�gina razor.ini
            //AntesDelOnGetDeLaPaginaRazor(context);
            //Antes de ejecutar el onget de la p�gina razor.fin
            //Ejecuci�n del onget de la p�gina razor.ini
            await next.Invoke();
            //Ejecuci�n del onget de la p�gina razor.fin
            //Despu�s de ejecutar el onget de la p�gina razor.ini
            DespuesDelOnGetDeLaPaginaRazor(context);
            //Despu�s de ejecutar el onget de la p�gina razor.fin
        }
    }
}
