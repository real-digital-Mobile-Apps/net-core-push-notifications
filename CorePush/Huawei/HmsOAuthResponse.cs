using Newtonsoft.Json;

namespace CorePush.Huawei
{
    public class HmsOAuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("error")]
        public int Error { get; set; }

        [JsonProperty("sub_error")]
        public int SubError { get; set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}
