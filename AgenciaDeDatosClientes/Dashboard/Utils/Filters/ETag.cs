using System.Globalization;
using System;
using Microsoft.Net.Http.Headers;

namespace Dashboard.Utils.Filters
{
    public class ETag
    {
        public DateTime? LastModified { get; set; }

        public static DateTime _fechaActualCompilacion;
        private static string _formatoFecha = "r"; //"ddd, dd MMM yyy HH':'mm':'ss 'GMT'";  //Tue, 17 Oct 2023 12:34:32 GMT
        private static CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        public static DateTime? GetEtagRequest(string parametroCabeceraRequest)
        {
            DateTime? fecha = null;
            if (!String.IsNullOrEmpty(parametroCabeceraRequest))
            {
                string parametro = parametroCabeceraRequest;
                if (parametro.StartsWith("\"")) // si empieza por comillas
                {
                    parametro = parametro.TrimStart('"').TrimEnd('"');
                }
                DateTime fechaConvertida;
                if (DateTime.TryParseExact(parametro, _formatoFecha, _cultureInfo, DateTimeStyles.None, out fechaConvertida))
                {
                    fecha = fechaConvertida;
                }
            }
            return fecha;
        }

        public static string GetEtagResponse(DateTime fecha)
        {
            return fecha.ToString(_formatoFecha, _cultureInfo);
        }

        internal DateTime? GetLastModifiedCliente(HttpRequest request)
        {
            DateTime? lastModifiedCliente = null;
            if (lastModifiedCliente == null && request.Headers.ContainsKey(HeaderNames.IfNoneMatch))
            {
                DateTime? fecha = ETag.GetEtagRequest(request.Headers[HeaderNames.IfNoneMatch].ToString());
                if (fecha != null)
                {
                    lastModifiedCliente = lastModifiedCliente == null || lastModifiedCliente > fecha ? fecha : lastModifiedCliente;
                }
            }
            if (lastModifiedCliente == null && request.Headers.ContainsKey(HeaderNames.IfModifiedSince))
            {
                DateTime? fecha = ETag.GetEtagRequest(request.Headers[HeaderNames.IfModifiedSince].ToString());
                if (fecha != null)
                {
                    lastModifiedCliente = lastModifiedCliente == null || lastModifiedCliente > fecha ? fecha : lastModifiedCliente;
                }
            }
            if (lastModifiedCliente == null && request.Headers.ContainsKey(HeaderNames.ETag))
            {
                DateTime? fecha = ETag.GetEtagRequest(request.Headers[HeaderNames.ETag].ToString());
                if (fecha != null)
                {
                    lastModifiedCliente = lastModifiedCliente == null || lastModifiedCliente > fecha ? fecha : lastModifiedCliente;
                }
            }
            if (lastModifiedCliente == null && request.Headers.ContainsKey(HeaderNames.LastModified))
            {
                DateTime? fecha = ETag.GetEtagRequest(request.Headers[HeaderNames.LastModified].ToString());
                if (fecha != null)
                {
                    lastModifiedCliente = lastModifiedCliente == null || lastModifiedCliente > fecha ? fecha : lastModifiedCliente;
                }
            }
            return lastModifiedCliente;
        }

        internal bool Devolver304(HttpRequest request)
        {
            bool devolver304;
            try
            {
                DateTime? lastModifiedCliente = GetLastModifiedCliente(request);
                if (lastModifiedCliente != null && LastModified != null)
                {
                    DateTime fecha = (DateTime)LastModified;
                    if (lastModifiedCliente.Equals(new DateTime(fecha.Year, fecha.Month, fecha.Day, fecha.Hour, fecha.Minute, fecha.Second)))
                    {
                        devolver304 = true;
                    }
                    else
                    {
                        devolver304 = false;
                    }
                }
                else
                {
                    devolver304 = false;
                }
            }
            catch (Exception e)
            {
                devolver304 = false;
            }
            
            return devolver304;
        }
        internal DateTime GetLastModifiedConCaducidadCadaXMinutos(HttpRequest request, int minutos)
        {
            DateTime lastModifiedConCaducidadCadaXMinutos;
            try
            {
                DateTime? lastModifiedCliente = GetLastModifiedCliente(request);
                if (lastModifiedCliente != null)
                {
                    double minutosTranscurridos = DateTime.UtcNow.Subtract(lastModifiedCliente.Value).TotalMinutes;
                    if (minutosTranscurridos > minutos)
                    {
                        //Han pasado más minutos de los permitidos:
                        lastModifiedConCaducidadCadaXMinutos = DateTime.UtcNow;
                    }
                    else
                    {
                        lastModifiedConCaducidadCadaXMinutos = lastModifiedCliente.Value;
                    }
                }
                else
                {
                    lastModifiedConCaducidadCadaXMinutos = DateTime.UtcNow;
                }
            }
            catch (Exception e)
            {
                lastModifiedConCaducidadCadaXMinutos = DateTime.UtcNow;
            }
            return lastModifiedConCaducidadCadaXMinutos;
        }        
    }
}
