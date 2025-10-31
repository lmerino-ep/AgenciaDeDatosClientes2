using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Modelos.Autenticacion
{
    public class TokenGoogle
    {
        [JsonProperty("access_token")]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonProperty("expires_in")]
        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("id_token")]
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }
}
