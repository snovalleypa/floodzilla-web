using System.Net;
using Newtonsoft.Json;

namespace FzCommon
{
    //$ TODO: consider a multiple-recipient model

    // for now we re-use EmailModel as the content payload for these...
    public class SmsSendRequest
    {
        public string Phone;
        public string SmsText;

        // This is really only necessary for testing...
        public string Email;
        
        public static SmsSendRequest Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<SmsSendRequest>(body);
        }
    }

    public enum SmsSendResult
    {
        Success,
        InvalidNumber,
        Failure,
    }

    public class SmsClient
    {
        public SmsClient()
        {
        }

        // email is only used for testing...
        public async Task<SmsSendResult> SendSms(string phone, string email, EmailModel model)
        {
            SmsSendRequest req = new SmsSendRequest()
            {
                Phone = phone,
                Email = email,
                SmsText = model.GetSmsText(),
            };

            string smsSendUrl = FzConfig.Config[FzConfig.Keys.SmsSendServiceUrl];
            string body = JsonConvert.SerializeObject(req);
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(body);
                HttpResponseMessage response = await client.PostAsync(smsSendUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    return SmsSendResult.Success;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return SmsSendResult.InvalidNumber;
                }
            }
            
            return SmsSendResult.Failure;
        }
    }
}
