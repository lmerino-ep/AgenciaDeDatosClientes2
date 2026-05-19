using System.Text.Json;

namespace Modelos.DTO.Metricas
{
    /// <summary>
    /// Resultado de la ejecución de un KPI
    /// </summary>
    public class ResultadoKpi
    {
        public string KpiId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public Dictionary<string, object> Parametros { get; set; } = new Dictionary<string, object>();
        public DateTime TimestampGeneracion { get; set; }
        public int TotalRegistros { get; set; }
        public List<Dictionary<string, object>> Datos { get; set; } = new List<Dictionary<string, object>>();
        /// <summary>
        /// Datos solo para el gráfico cuando difieren de la tabla (p. ej. serie temporal vs. top por representación).
        /// </summary>
        public List<Dictionary<string, object>>? DatosGrafico { get; set; }
        public bool DesdCache { get; set; }
        public int DuracionMs { get; set; }

        /// <summary>
        /// Serializa los datos a JSON
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        /// <summary>
        /// Exporta los datos a formato CSV
        /// </summary>
        public string ToCsv()
        {
            if (Datos.Count == 0)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();

            // Encabezados
            var headers = Datos[0].Keys.ToList();
            sb.AppendLine(string.Join(";", headers));

            // Datos
            foreach (var fila in Datos)
            {
                var valores = headers.Select(h =>
                {
                    var valor = fila.ContainsKey(h) ? fila[h]?.ToString() ?? "" : "";
                    // Escapar comillas dobles
                    if (valor.Contains("\"") || valor.Contains(";") || valor.Contains("\n"))
                    {
                        valor = "\"" + valor.Replace("\"", "\"\"") + "\"";
                    }
                    return valor;
                });
                sb.AppendLine(string.Join(";", valores));
            }

            return sb.ToString();
        }
    }
}
