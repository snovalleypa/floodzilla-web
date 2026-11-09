using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

using FzCommon;
using System.Net.Http;
using System.Text;
using System;

namespace FloodzillaSms
{
    public class ExpoPushRequest
    {
        [JsonProperty("to")]                public required List<string> To;
        [JsonProperty("data")]              public required object Data;
        [JsonProperty("title")]             public required string Title;
        [JsonProperty("body")]              public required string Body;
        [JsonProperty("ttl")]               public int? Ttl;
        [JsonProperty("expiration")]        public int? Expiration;
        [JsonProperty("priority")]          public string? Priority;
        [JsonProperty("subtitle")]          public string? Subtitle;
        [JsonProperty("sound")]             public string? Sound;
        [JsonProperty("badge")]             public int? BadgeCount;
        [JsonProperty("channelId")]         public string? ChannelId;
        [JsonProperty("categoryId")]        public string? CategoryId;
        [JsonProperty("mutableContent")]    public bool? MutableContent;
    }

    public class ExpoPushTicketResponse
    {
        [JsonProperty("status")]            public required string Status;
        [JsonProperty("id")]                public required string TicketId;
        [JsonProperty("message")]           public required string ErrorMessage;
        [JsonProperty("details")]           public required object ErrorDetails;
    }

    public class ExpoErrorDetail
    {
        [JsonProperty("code")]              public required string ErrorCode;
        [JsonProperty("message")]           public required string ErrorMessage;
        [JsonProperty("isTransient")]       public bool? IsTransient;
        [JsonProperty("details")]           public object? ErrorDetails;
    }

    public class ExpoPushResponse
    {
        [JsonProperty("data")]              public required List<ExpoPushTicketResponse> Statuses;
        [JsonProperty("errors")]            public required List<ExpoErrorDetail> Errors;
    }

    public class ExpoReceiptRequest
    {
        [JsonProperty("ids")]               public required List<string> TicketIds;
    }

    public class ExpoReceiptDetail
    {
        [JsonProperty("status")]            public required string Status;
        [JsonProperty("message")]           public string? ErrorMessage;
        [JsonProperty("details")]           public object? ErrorDetails;
    }
    
    public class ExpoReceiptResponse
    {
        [JsonProperty("data")]              public required Dictionary<string, ExpoReceiptDetail> Receipts;
        [JsonProperty("errors")]            public required List<ExpoErrorDetail> Errors;
    }
    
    public class ExpoPushClient
    {
        private readonly string pushRequestUrl;
        private readonly string receiptRequestUrl;
        
        public ExpoPushClient()
        {
            this.pushRequestUrl = FzConfig.Config[FzConfig.Keys.ExpoPushRequestUrl]!;
            this.receiptRequestUrl = FzConfig.Config[FzConfig.Keys.ExpoReceiptRequestUrl]!;
        }

        public async Task<ExpoPushResponse> SendPushRequestAsync(ExpoPushRequest request)
        {
            return await this.Post<ExpoPushRequest, ExpoPushResponse>(request, this.pushRequestUrl);
        }

        public async Task<ExpoReceiptResponse> SendGetReceiptsRequestAsync(ExpoReceiptRequest request)
        {
            return await this.Post<ExpoReceiptRequest, ExpoReceiptResponse>(request, this.receiptRequestUrl);
        }

        private async Task<TRsp> Post<TReq, TRsp>(TReq req, string url)
        {
            string body = JsonConvert.SerializeObject(req);
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await client.PostAsync(url, content);
            if (!httpResponse.IsSuccessStatusCode)
            {
                // If we have a body, go ahead and try to return it.
                try
                {
                    string errorBody = await httpResponse.Content.ReadAsStringAsync();
                    TRsp? errorRsp = JsonConvert.DeserializeObject<TRsp>(errorBody);
                    if (errorRsp != null)
                    {
                        return errorRsp;
                    }
                }
                catch
                {
                    // This probably failed because the response didn't have a body.  Just ignore it. 
                }
                throw new ApplicationException(String.Format("Error posting Expo request to {0}: {1}", url, httpResponse.StatusCode));
            }
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            TRsp? rsp = JsonConvert.DeserializeObject<TRsp>(responseBody);
            if (rsp == null)
            {
                throw new ApplicationException(String.Format("Could not deserialize Expo response body from {0}: {1}", url, responseBody));
            }
            return rsp;
        }
    }
}

