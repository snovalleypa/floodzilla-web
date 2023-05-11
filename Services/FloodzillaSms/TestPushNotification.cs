using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using FzCommon;

[assembly: FunctionsStartup(typeof(FloodzillaSms.TestPushNotification))]

namespace FloodzillaSms
{
    public class TestPushNotification : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() { AllowIntegerValues = false }},
            };

            //$ TODO: Any other configuration/initialization?
        }

        [FunctionName("TestPushNotification")]
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
            TestPushNotificationRequest testRequest = TestPushNotificationRequest.Deserialize(body);
            ExpoPushClient pushClient = new();

            //$ TODO: params
            List<string> tokens = new List<string>();
            tokens.Add(testRequest.Token);
            ExpoPushRequest pushReq = new()
            {
                To = tokens,
                Data = testRequest.JsonData,
                Title = testRequest.Title,
                Body = testRequest.TextBody,
                Ttl = testRequest.Ttl,
                Expiration = testRequest.Expiration,
                Priority = testRequest.Priority,
                Subtitle = testRequest.SubTitle,
                Sound = testRequest.Sound,
                BadgeCount = testRequest.BadgeCount,
                ChannelId = testRequest.ChannelId,
            };

            try
            {
                ExpoPushResponse pushResp = await pushClient.SendPushRequestAsync(pushReq);
                if (pushResp == null || pushResp.Statuses == null)
                {
                    return new ObjectResult("An error occurred processing the response from the server.")
                    {
                        StatusCode = 500,
                    };
                }
                if (pushResp.Errors != null)
                {
                    // There are no examples of what this looks like, and I can't find a way to force
                    // a response here.  For now I'm just going to assume there's an error that affects
                    // all tokens, and look in the log table to figure it out later.
                    return new ObjectResult("One or more push ticket errors occurred.")
                    {
                        StatusCode = 500,
                    };
                }

                // The PushTicketStatuses responses don't have the ticket, so I guess I just
                // have to assume they're in the same order?
                if (pushResp.Statuses.Count != 1)
                {
                    // I'm leaving these vaguely-worded because it seems like bad practice to be too
                    // specific in error messages that might end up accidentally being user-visible
                    return new ObjectResult("An error occurred processing the server's responses.")
                    {
                        StatusCode = 500,
                    };
                }

                string response = "OK";
                ExpoPushTicketResponse status = pushResp.Statuses[0];
                if (status.Status.Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                {
                    response = String.Format("Sent successfully. Ticket = {0}", status.TicketId);
                }
                else if (status.Status.Equals("error", StringComparison.InvariantCultureIgnoreCase))
                {
                    response = String.Format("Push ticket error: Error = {0}", JsonConvert.SerializeObject(status.ErrorDetails));
                }
                
                return new ContentResult() { Content = JsonConvert.SerializeObject(response), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                // nothing here
            }

            return new ObjectResult("An error occurred.")
            {
                StatusCode = 500,
            };
        }
    }
}
