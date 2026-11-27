using System.Net;
using Newtonsoft.Json;

namespace FzCommon
{
    //$ TODO: consider a multiple-recipient model

    public class PushNotificationSendRequest
    {
        public List<string> Tokens = [];
        public string? Title;
        public string? Subtitle;
        public string? Body;
        public string? Data;

        public static PushNotificationSendRequest Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<PushNotificationSendRequest>(body);
        }
    }

    public class PushTokenSendResponse
    {
        public string Token;
        public PushNotificationSendResult Result;
        public string TicketId;
    }

    public class PushNotificationSendResponse
    {
        public List<PushTokenSendResponse> Results;
    }

    public enum PushNotificationSendResult
    {
        Success,
        Failure,
        DeviceNotRegistered,
    }

    //$ TODO: This can be removed once we've settled on the proper settings
    //$ for all of these fields.
    public class TestPushNotificationRequest
    {
        public string Token;
        public string JsonData;
        public string? Title;
        public string? TextBody;
        public int? Ttl;
        public int? Expiration;
        public string? Priority;
        public string? SubTitle;
        public string? Sound;
        public int? BadgeCount;
        public string? ChannelId;

        public static TestPushNotificationRequest Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<TestPushNotificationRequest>(body);
        }
    }

    public enum PushNotificationReceiptResult
    {
        Success,
        Missing,
        Error,
        DeviceNotRegistered,
    }

    public class PushNotificationReceipt
    {
        public string TicketId;
        public PushNotificationReceiptResult Result;
        public string? Message;
        public string? Error;
    }

    public class PushNotificationReceiptResponse
    {
        public List<PushNotificationReceipt> Receipts;
    }

    public class PushNotificationClient
    {
        public PushNotificationClient()
        {
        }

        public async Task<PushNotificationSendResponse?> SendPushNotification(List<string> tokens, string? title, string? subtitle, string? body, string? data)
        {
            if (String.IsNullOrEmpty(title) && String.IsNullOrEmpty(subtitle) && String.IsNullOrEmpty(body) && String.IsNullOrEmpty(data))
            {
                return null;
            }
            if (tokens.Count < 1)
            {
                return null;
            }
            PushNotificationSendRequest req = new()
            {
                Tokens = tokens,
                Title = title,
                Subtitle = subtitle,
                Body = body,
                Data = data,
            };

            string sendUrl = FzConfig.Config[FzConfig.Keys.SecurePushNotificationServiceUrl]!;
            string sendKey = FzConfig.Config[FzConfig.Keys.SecurePushNotificationServiceKey]!;
            string reqBody = JsonConvert.SerializeObject(req);
            using (HttpClient client = new())
            {
                HttpContent content = new StringContent(reqBody);
                content.Headers.Add("x-functions-key", sendKey);
                HttpResponseMessage response = await client.PostAsync(sendUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                string responseBody = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<PushNotificationSendResponse>(responseBody);
            }
        }

        public async Task<PushNotificationReceiptResponse?> GetPushReceipts(List<string> ticketIds)
        {
            if (ticketIds == null || ticketIds.Count < 1)
            {
                return null;
            }

            string sendUrl = FzConfig.Config[FzConfig.Keys.SecurePushReceiptsServiceUrl]!;
            string sendKey = FzConfig.Config[FzConfig.Keys.SecurePushReceiptsServiceKey]!;
            string body = JsonConvert.SerializeObject(ticketIds);
            using (HttpClient client = new())
            {
                HttpContent content = new StringContent(body);
                content.Headers.Add("x-functions-key", sendKey);
                HttpResponseMessage response = await client.PostAsync(sendUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                string responseBody = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<PushNotificationReceiptResponse>(responseBody);
            }
        }
    }
}
