using Controllers;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;

namespace Utils.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ETagFilterControllers : ActionFilterAttribute, IAsyncActionFilter
    {
        private void AntesDelActionDelController(ActionExecutingContext context)
        {
            
        }
        private void DespuesDelActionDelController(ActionExecutingContext context)
        {
            if (context?.Controller != null)
            {
                //Casteamos a dynamic porque no queremos usar reflexion para ver qué controller es
                ETag eTag = ((dynamic)context.Controller).ETag;

                if(eTag != null)
                {
                    var request = context.HttpContext.Request;
                    var response = context.HttpContext.Response;

                    if (
                        request.Method == HttpMethod.Get.Method &&
                        //response.StatusCode == (int)HttpStatusCode.OK && //Aquí la respuesta del action del controller aún no se ha generado, porque lo que será siempre 200 (por defecto)
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

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //Antes de ejecutar el action del controller.ini
            //AntesDelActionDelController(context);
            //Antes de ejecutar el action del controller.fin
            //Ejecución del action del controller.ini
            await next();
            //Ejecución del action del controller.fin
            //Después de ejecutar el action del controller.ini
            DespuesDelActionDelController(context);
            //Después de ejecutar el action del controller.fin
        }

        //public override void OnActionExecuted(ActionExecutedContext context)
        //{
        //    //Después de ejecutar el action del controller.ini
        //    DespuesDelActionDelControllerAsync(context);
        //    //Después de ejecutar el action del controller.fin
        //}

        //public override void OnResultExecuted(ResultExecutedContext context)
        //{
            
        //    //Solo aquí el status code está generado
        //    //Pero aquí no podemo modificar el context.Result, pues ya está generado y es de solo lectura
        //}
    }
}
