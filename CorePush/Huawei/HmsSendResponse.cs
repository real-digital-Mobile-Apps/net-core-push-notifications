using Newtonsoft.Json;
namespace CorePush.Huawei
{
    public class HmsSendResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
    }
}
