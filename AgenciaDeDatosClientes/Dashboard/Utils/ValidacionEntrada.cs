using System.Text.RegularExpressions;

namespace Dashboard.Utils
{
    /// <summary>
    /// Clase para validación segura de entrada de datos
    /// </summary>
    public static class ValidacionEntrada
    {
        /// <summary>
        /// Valida y sanitiza texto de búsqueda para prevenir inyecciones
        /// </summary>
        /// <param name="texto">Texto a validar</param>
        /// <param name="maxLength">Longitud máxima permitida (default: 100)</param>
        /// <returns>Texto sanitizado y seguro</returns>
        public static string SanitizarTextoBusqueda(string texto, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Limitar longitud
            if (texto.Length > maxLength)
                texto = texto.Substring(0, maxLength);

            // Eliminar caracteres peligrosos pero mantener funcionalidad de búsqueda
            texto = Regex.Replace(texto, @"[<>""'%;()&+]", "", RegexOptions.Compiled);
            
            // Eliminar múltiples espacios
            texto = Regex.Replace(texto, @"\s+", " ", RegexOptions.Compiled);
            
            // Trim espacios
            return texto.Trim();
        }

        /// <summary>
        /// Valida que un texto no contenga caracteres peligrosos para SQL
        /// </summary>
        /// <param name="texto">Texto a validar</param>
        /// <returns>True si es seguro, False si contiene caracteres peligrosos</returns>
        public static bool EsTextoSeguro(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            // Patrones peligrosos para SQL injection
            var patronesPeligrosos = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|SCRIPT)\b)",
                @"(--|/\*|\*/)",
                @"(xp_|sp_)",
                @"(CHAR|NCHAR|VARCHAR|NVARCHAR)\s*\(",
                @"(CAST|CONVERT)\s*\(",
                @"(BULK|OPENROWSET|OPENDATASOURCE)",
                @"(WAITFOR|DELAY)",
                @"(SHUTDOWN|RESTORE|BACKUP)"
            };

            foreach (var patron in patronesPeligrosos)
            {
                if (Regex.IsMatch(texto, patron, RegexOptions.IgnoreCase))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Valida y sanitiza un email
        /// </summary>
        /// <param name="email">Email a validar</param>
        /// <returns>Email sanitizado o string vacío si no es válido</returns>
        public static string SanitizarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            // Validar formato de email
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailRegex.IsMatch(email))
                return string.Empty;

            // Sanitizar
            email = email.Trim().ToLowerInvariant();
            
            // Limitar longitud
            if (email.Length > 254)
                email = email.Substring(0, 254);

            return email;
        }

        /// <summary>
        /// Valida y sanitiza un nombre de usuario
        /// </summary>
        /// <param name="nombre">Nombre a validar</param>
        /// <param name="maxLength">Longitud máxima (default: 50)</param>
        /// <returns>Nombre sanitizado</returns>
        public static string SanitizarNombre(string nombre, int maxLength = 50)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return string.Empty;

            // Eliminar caracteres no permitidos en nombres
            nombre = Regex.Replace(nombre, @"[^a-zA-ZáéíóúÁÉÍÓÚñÑ\s\-'\.]", "", RegexOptions.Compiled);
            
            // Eliminar múltiples espacios
            nombre = Regex.Replace(nombre, @"\s+", " ", RegexOptions.Compiled);
            
            // Trim y limitar longitud
            nombre = nombre.Trim();
            if (nombre.Length > maxLength)
                nombre = nombre.Substring(0, maxLength);

            return nombre;
        }

        /// <summary>
        /// Valida que un ID numérico sea válido
        /// </summary>
        /// <param name="id">ID a validar</param>
        /// <param name="minValue">Valor mínimo (default: 1)</param>
        /// <param name="maxValue">Valor máximo (default: int.MaxValue)</param>
        /// <returns>True si es válido</returns>
        public static bool EsIdValido(int id, int minValue = 1, int maxValue = int.MaxValue)
        {
            return id >= minValue && id <= maxValue;
        }

        /// <summary>
        /// Valida que un GUID sea válido
        /// </summary>
        /// <param name="guid">GUID a validar</param>
        /// <returns>True si es válido</returns>
        public static bool EsGuidValido(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            return Guid.TryParse(guid, out _);
        }


        public static string NormalizarValorNumerico(string sValor)
        {
            if (string.IsNullOrEmpty(sValor))
                return sValor;

            // Contar puntos y comas
            int puntos = sValor.Count(c => c == '.');
            int comas = sValor.Count(c => c == ',');

            if (puntos == 0 && comas == 0)
            {
                // Sin separadores decimales, devolver tal como está
                return sValor;
            }
            else if (puntos == 1 && comas == 0)
            {
                // Formato americano: "1234.56" - ya está normalizado
                return sValor;
            }
            else if (puntos == 0 && comas == 1)
            {
                // Formato europeo: "1234,56" - reemplazar coma por punto
                return sValor.Replace(",", ".");
            }
            else if (puntos >= 1 && comas == 1)
            {
                // Formato mixto: "1.234,56" - eliminar puntos (miles) y reemplazar coma por punto
                return sValor.Replace(".", "").Replace(",", ".");
            }
            else
            {
                // Casos complejos o múltiples separadores - intentar limpiar
                // Eliminar todos los puntos y reemplazar la última coma por punto
                string resultado = sValor.Replace(".", "");
                int ultimaComa = resultado.LastIndexOf(',');
                if (ultimaComa >= 0)
                {
                    resultado = resultado.Substring(0, ultimaComa) + "." + resultado.Substring(ultimaComa + 1);
                }
                return resultado;
            }
        }
    }
}
