using System.Net;
using Newtonsoft.Json;

namespace FloodzillaWeb.Services
{
    public class GoogleReCaptcha
    {
        private const string VERIFY_URL = "https://www.google.com/recaptcha/api/siteverify";
        public static readonly string USER_RESPONSE_FORM_FIELD = "g-Recaptcha-Response";
        private bool m_Success;
        private List<string> m_ErrorCodes;
        public static GoogleReCaptcha GetResponse(string secretkey, string userresponse)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string result = client.DownloadString(VERIFY_URL + $"?secret={secretkey}&response={userresponse}");
                    return JsonConvert.DeserializeObject<GoogleReCaptcha>(result);
                }
            }
            catch { return null; }
        }

        [JsonProperty("success")]
        public bool Success
        {
            get { return m_Success; }
            set { m_Success = value; }
        }

        [JsonProperty("error-codes")]
        public List<string> ErrorCodes
        {
            get { return m_ErrorCodes; }
            set { m_ErrorCodes = value; }
        }
    }
}
