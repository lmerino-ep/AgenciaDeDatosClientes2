using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Dashboard.Utils
{
    public static class TransparentKeysHelper
    {
        private const string NombreHeaderSurrogate = "Surrogate-Keys";

        /// <summary>
        /// Añade una clave surrogate al header Surrogate-Keys de la respuesta
        /// </summary>
        /// <param name="valor">Valor que se va a añadir al header Surrogate-Keys</param>
        /// <param name="context">Opcional. HttpContext de la solicitud</param>
        /// <remarks></remarks>
        public static void AddSurrogateKey(string valor, HttpContext context)
        {
            // Header Surrogate-Keys para caché en cdn
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(valor))
                {
                    return;
                }
                string surrogateActual = context.Response.Headers[NombreHeaderSurrogate];
                if (!String.IsNullOrEmpty(surrogateActual))
                {
                    var surrogateKeys = surrogateActual.Trim().Split(' ').ToList();
                    if (!surrogateKeys.Contains(valor)) // no repetimos label
                    {
                        context.Response.Headers[NombreHeaderSurrogate] = string.Join(" ", valor, surrogateActual); // concatenamos con lo que había
                    }
                }
                else
                {
                    context.Response.Headers.Add(NombreHeaderSurrogate, valor);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error al escribir SurrogateKey: {ex.Message} url: {context.Request.Path.ToString()} {ex.StackTrace}");
            }
        }
    }
}
