using System.Security.Cryptography;
using System.Text;

namespace Utils
{
    public static class Hash
    {
        public static string HashText(string textoEnClaro)
        {
            MD5 md5 = MD5.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            StringBuilder sb = new StringBuilder();
            var stream = md5.ComputeHash(encoding.GetBytes(textoEnClaro));
            foreach (byte t in stream)
            {
                sb.AppendFormat("{0:x2}", t);
            }
            return sb.ToString();
        }
    }
}
