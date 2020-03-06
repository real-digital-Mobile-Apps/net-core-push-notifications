﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CorePush.Utils;
using Newtonsoft.Json.Linq;

namespace CorePush.Google
{
    /// <summary>
    /// Firebase message sender
    /// </summary>
    public class FcmSender : IDisposable
    {
        private readonly string fcmUrl = "https://fcm.googleapis.com/fcm/send";
        private readonly string serverKey;
        private readonly string senderId;
        private readonly Lazy<HttpClient> lazyHttp = new Lazy<HttpClient>();

        public FcmSender(string serverKey, string senderId)
        {
            this.serverKey = serverKey;
            this.senderId = senderId;
        }

        /// <summary>
        /// Send firebase notification.
        /// Please check out payload formats:
        /// https://firebase.google.com/docs/cloud-messaging/concept-options#notifications
        /// The SendAsync method will add/replace "to" value with deviceId
        /// </summary>
        /// <param name="deviceId">Device token</param>
        /// <param name="payload">Notification payload that will be serialized using Newtonsoft.Json package</param>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<FcmResponse> SendAsync(string deviceId, object payload)
        {
            var jsonObject = JObject.FromObject(payload);
            jsonObject.Remove("to");
            jsonObject.Add("to", JToken.FromObject(deviceId));
            var json = jsonObject.ToString();
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, fcmUrl);
            httpRequest.Headers.Add("Authorization", $"key = {serverKey}");
            httpRequest.Headers.Add("Sender", $"id = {senderId}");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await lazyHttp.Value.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonHelper.Deserialize<FcmResponse>(responseString);
        }

        /// <summary>
        /// Send firebase notification to multiple targets.
        /// Please check out payload formats:
        /// https://firebase.google.com/docs/cloud-messaging/concept-options#notifications
        /// The SendMultipleAsync method will add/replace "to" value with deviceId
        /// </summary>
        /// <param name="multiplePayloads">key = deviceId, value = payload</param>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<FcmResponse> SendMultipleAsync(Dictionary<string, object> multiplePayloads)
        {
            var batchRequest = new HttpRequestMessage(HttpMethod.Post, fcmUrl);
            var batchContent = new MultipartContent("mixed", "--subrequest_boundary");
            batchRequest.Content = batchContent;

            foreach (var data in multiplePayloads)
            {
                var jsonObject = JObject.FromObject(data.Value);
                jsonObject.Remove("to");
                jsonObject.Add("to", JToken.FromObject(data.Key));
                var json = jsonObject.ToString();

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.Add("Authorization", $"key = {serverKey}");
                content.Headers.Add("Sender", $"id = {senderId}");
                batchContent.Add(content);
            }

            using var response = await lazyHttp.Value.SendAsync(batchRequest);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            return JsonHelper.Deserialize<FcmResponse>(responseString);
        }

        public void Dispose()
        {
            if (lazyHttp.IsValueCreated)
            {
                lazyHttp.Value.Dispose();
            }
        }
    }
}
