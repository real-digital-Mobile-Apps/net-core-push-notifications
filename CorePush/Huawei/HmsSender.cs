using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CorePush.Utils;
using Newtonsoft.Json.Linq;

namespace CorePush.Huawei
{
    public class HmsSender
    {
        private readonly string oAuthUrl = "https://oauth-login.cloud.huawei.com/oauth2/v2/token";
        private readonly int clientId;
        private readonly string clientSecret;
        private readonly string hmsUrl = "https://push-api.cloud.huawei.com/v1/{0}/messages:send";
        private readonly Lazy<HttpClient> lazyHttp = new Lazy<HttpClient>();

        public HmsSender(int clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        /// <summary>
        /// Request Client Password Mode Open Platform Authentication
        /// See for reference:
        /// https://developer.huawei.com/consumer/en/doc/development/HMS-Guides/38054564#h2-1580973380498
        /// </summary>
        /// <returns>OAuth Response</returns>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<HmsOAuthResponse> AuthenticateAsync()
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, oAuthUrl);
            var payload = new List<KeyValuePair<string, string>>();
            payload.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            payload.Add(new KeyValuePair<string, string>("client_id", $"{clientId}"));
            payload.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            httpRequest.Content = new FormUrlEncodedContent(payload);

            using var response = await lazyHttp.Value.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonHelper.Deserialize<HmsOAuthResponse>(responseString);
        }

        /// <summary>
        /// Refreshes a Open Platform Authentication Token
        /// See for reference:
        /// https://developer.huawei.com/consumer/en/doc/development/HMS-Guides/38054564#h1-1579159447537
        /// </summary>
        /// <param name="refreshToken">Enter refresh_token obtained in authorization code mode</param>
        /// <returns>OAuth Response</returns>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<HmsOAuthResponse> RefreshTokenAsync(string refreshToken)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, oAuthUrl);
            var payload = new List<KeyValuePair<string, string>>();
            payload.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            payload.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
            payload.Add(new KeyValuePair<string, string>("client_id", $"{clientId}"));
            payload.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            httpRequest.Content = new FormUrlEncodedContent(payload);

            using var response = await lazyHttp.Value.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonHelper.Deserialize<HmsOAuthResponse>(responseString);
        }

        /// <summary>
        /// Send HMS Core Push Message.
        /// See for reference:
        /// https://developer.huawei.com/consumer/en/doc/development/HMS-References/push-sendapi#h1-1576153506293
        /// </summary>
        /// <param name="appId">The Huawei App ID</param>
        /// <param name="accessToken">The OAuth Access Token</param>
        /// <param name="payload">Push Message</param>
        /// <returns>Send response</returns>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<HmsSendResponse> SendAsync(string appId, string accessToken, object payload)
        {
            var jsonObject = JObject.FromObject(payload);
            var json = jsonObject.ToString();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, hmsUrl);
            httpRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await lazyHttp.Value.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonHelper.Deserialize<HmsSendResponse>(responseString);
        }
    }
}