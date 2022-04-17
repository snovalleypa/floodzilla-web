using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Plivo;

using FzCommon;
using System.Web.Http;

[assembly: FunctionsStartup(typeof(FloodzillaSms.SendSms))]

namespace FloodzillaSms
{
    public class SendSms : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            //$ TODO: Any other configuration/initialization?
        }

        [FunctionName("SendSms")]
        [Consumes("application/json")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string body;
            Stream inputStream = req.Body;
            using (StreamReader sr = new StreamReader(inputStream))
            {
                body = sr.ReadToEnd();
            }
            SmsSendRequest smsSendRequest = SmsSendRequest.Deserialize(body);
            
            string plivoId = FzConfig.Config[FzConfig.Keys.PlivoAuthId];
            string plivoToken = FzConfig.Config[FzConfig.Keys.PlivoAuthToken];
            string smsFrom = FzConfig.Config[FzConfig.Keys.SmsFromPhone];

            string phone = smsSendRequest.Phone;
            if (phone.StartsWith("999"))
            {
                if (phone == "9990000000")
                {
                    return new BadRequestResult();
                }

                await SendTestEmail(smsSendRequest);
                return new OkObjectResult("Message Sent");
            }

            if (!phone.StartsWith("1") && !phone.StartsWith("0"))
            {
                phone = "1" + phone;
            }

            List<string> dest = new List<string>();
            dest.Add(phone);

            PlivoApi api = new PlivoApi(plivoId, plivoToken);

            try
            { 
                Plivo.Resource.Message.MessageCreateResponse response = await api.Message.CreateAsync(src: smsFrom, dst: dest, text: smsSendRequest.SmsText);
                return new OkObjectResult("Message Sent");
            }
            catch (Exception e)
            {
                if (e is Plivo.Exception.PlivoValidationException)
                {
                    return new BadRequestResult();
                }

                //$ log this somewhere?
            }
            return new InternalServerErrorResult();
        }

        private static async Task SendTestEmail(SmsSendRequest smsSendRequest)
        {
            using (EmailClient client = new EmailClient())
            {
                string from = FzConfig.Config[FzConfig.Keys.EmailFromAddress];
                string subject = "Floodzilla Test SMS to " + smsSendRequest.Phone;
                string body = "Floodzilla Test SMS:\r\n" + smsSendRequest.SmsText;

                int at = smsSendRequest.Email.IndexOf('@');
                if (at == -1)
                {
                    return;
                }
                string recipient = smsSendRequest.Email.Substring(0, at) + "+" + smsSendRequest.Phone + smsSendRequest.Email.Substring(at);
                await client.SendEmailAsync(from, recipient, subject, body, false);
            }
        }
    }
}
