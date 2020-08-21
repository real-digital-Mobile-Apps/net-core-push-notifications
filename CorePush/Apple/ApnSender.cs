﻿using CorePush.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CorePush.Apple
{
    /// <summary>
    /// HTTP2 Apple Push Notification sender
    /// </summary>
    public class ApnSender : IDisposable
    {
        private static readonly Dictionary<ApnServerType, string> servers = new Dictionary<ApnServerType, string>
        {
            {ApnServerType.Development, "https://api.development.push.apple.com:443" },
            {ApnServerType.Production, "https://api.push.apple.com:443" }
        };

        private const string apnidHeader = "apns-id";

        private readonly string p8privateKey;
        private readonly string p8privateKeyId;
        private readonly string teamId;
        private readonly string appBundleIdentifier;
        private readonly ApnServerType server;
        private readonly Lazy<string> jwtToken;
        private readonly Lazy<HttpClient> http;

        /// <summary>
        /// Initialize sender
        /// </summary>
        /// <param name="p8privateKey">p8 certificate string</param>
        /// <param name="privateKeyId">10 digit p8 certificate id. Usually a part of a downloadable certificate filename</param>
        /// <param name="teamId">Apple 10 digit team id</param>
        /// <param name="appBundleIdentifier">App slug / bundle name</param>
        /// <param name="server">Development or Production server</param>
        public ApnSender(string p8privateKey, string p8privateKeyId, string teamId, string appBundleIdentifier, ApnServerType server)
        {
            var tag = this + ".Ctor";
            try
            {
                this.p8privateKey = p8privateKey;
                this.p8privateKeyId = p8privateKeyId;
                this.teamId = teamId;
                this.server = server;
                this.appBundleIdentifier = appBundleIdentifier;
                this.jwtToken = new Lazy<string>(() => CreateJwtToken());
                this.http = new Lazy<HttpClient>(() => new HttpClient());
            }
            catch (Exception ex)
            {
                Backend.Track.Error(tag, ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Serialize and send notification to APN. Please see how your message should be formatted here:
        /// https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CreatingtheNotificationPayload.html#//apple_ref/doc/uid/TP40008194-CH10-SW1
        /// Payload will be serialized using Newtonsoft.Json package.
        /// !IMPORTANT: If you send many messages at once, make sure to retry those calls. Apple typically doesn't like 
        /// to receive too many requests and may ocasionally respond with HTTP 429. Just try/catch this call and retry as needed.
        /// </summary>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<ApnsResponse> SendAsync(
            object notification,
            string deviceToken,
            string apnsId = null,
            int apnsExpiration = 0,
            int apnsPriority = 10,
            bool isBackground = false)
        {
            var tag = this + ".SendAsync";
            try
            {
                var path = $"/3/device/{deviceToken}";
                var json = JsonHelper.Serialize(notification);

                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(servers[server] + path))
                {
                    Version = new Version(2, 0),
                    Content = new StringContent(json)
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", jwtToken.Value);
                request.Headers.TryAddWithoutValidation(":method", "POST");
                request.Headers.TryAddWithoutValidation(":path", path);
                request.Headers.Add("apns-topic", appBundleIdentifier);
                request.Headers.Add("apns-expiration", apnsExpiration.ToString());
                request.Headers.Add("apns-priority", apnsPriority.ToString());
                request.Headers.Add("apns-push-type", isBackground ? "background" : "alert"); // for iOS 13 required
                if (!string.IsNullOrWhiteSpace(apnsId))
                {
                    request.Headers.Add(apnidHeader, apnsId);
                }

                using var response = await http.Value.SendAsync(request);
                var succeed = response.IsSuccessStatusCode;
                var content = await response.Content.ReadAsStringAsync();
                var error = JsonHelper.Deserialize<ApnsError>(content);

                return new ApnsResponse
                {
                    IsSuccess = succeed,
                    Error = error
                };
            }
            catch (Exception ex)
            {
                Backend.Track.Error(tag, ex.Message + "\r\n" + ex.StackTrace);
            }
            return null;
        }

        private string CreateJwtToken()
        {
            var tag = this + ".CreateJwtToken";
            try
            {
                var header = JsonHelper.Serialize(new { alg = "ES256", kid = p8privateKeyId });
                var payload = JsonHelper.Serialize(new { iss = teamId, iat = ToEpoch(DateTime.UtcNow) });

                using var dsa = ECDsa.Create("ECDsa");

                var keyBytes = Convert.FromBase64String(p8privateKey);
                dsa.ImportPkcs8PrivateKey(keyBytes, out _);

                var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));
                var payloadBasae64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
                var unsignedJwtData = $"{headerBase64}.{payloadBasae64}";
                var unsignedJwtBytes = Encoding.UTF8.GetBytes(unsignedJwtData);
                var signature = dsa.SignData(unsignedJwtBytes, 0, unsignedJwtBytes.Length, HashAlgorithmName.SHA256);

                return $"{unsignedJwtData}.{Convert.ToBase64String(signature)}";
            }
            catch (Exception ex)
            {
                Backend.Track.Error(tag, ex.Message + "\r\n" + ex.StackTrace);
            }
            return null;
        }

        private static int ToEpoch(DateTime time)
        {
            var span = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToInt32(span.TotalSeconds);
        }

        public void Dispose()
        {
            if (http.IsValueCreated)
            {
                http.Value.Dispose();
            }
        }
    }
}
