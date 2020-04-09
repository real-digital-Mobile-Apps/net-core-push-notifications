using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace CorePush.Google
{
    /// <summary>
    /// Firebase message sender
    /// </summary>
    public class FcmSender : IDisposable
    {
        /// <summary>
        /// Creates a new FcmSender instance.
        /// For reference for credentialsJson see:
        /// https://firebase.google.com/docs/cloud-messaging/auth-server
        /// </summary>
        /// <param name="credentialsJson">Service Account JSON File</param>
        public FcmSender(string credentialsJson)
        {
            try
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromJson(credentialsJson)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
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
        public async Task<string> SendAsync(string deviceId, Message payload)
        {
            try
            {
                payload.Token = deviceId;
                return await FirebaseMessaging.DefaultInstance.SendAsync(payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return "";
        }

        /// <summary>
        /// Send firebase notification to multiple targets.
        /// Please check out payload formats:
        /// https://firebase.google.com/docs/cloud-messaging/concept-options#notifications
        /// The SendMultipleAsync method will add/replace "to" value with deviceId
        /// </summary>
        /// <param name="multiplePayloads">key = deviceId, value = payload</param>
        /// <exception cref="HttpRequestException">Throws exception when not successful</exception>
        public async Task<List<string>> SendMultipleAsync(List<string> deviceIds, MulticastMessage payload)
        {
            try
            {
                payload.Tokens = deviceIds;

                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(payload);
                if (response.FailureCount > 0)
                {
                    var failedTokens = new List<string>();
                    for (var i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            // The order of responses corresponds to the order of the registration tokens.
                            failedTokens.Add(deviceIds[i]);
                        }
                    }

                    return failedTokens;
                }
                else
                    return new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return new List<string>();
        }

        public void Dispose()
        {
            FirebaseApp.DefaultInstance.Delete();
        }
    }
}
