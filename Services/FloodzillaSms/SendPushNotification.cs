using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


using FzCommon;
using Expo.Server.Client;
using Expo.Server.Models;

[assembly: FunctionsStartup(typeof(FloodzillaSms.SendPushNotification))]

namespace FloodzillaSms
{
    public class SendPushNotification : FunctionsStartup
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

        [FunctionName("SendPushNotification")]
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
            PushNotificationSendRequest sendRequest = PushNotificationSendRequest.Deserialize(body);
            PushApiClient pushClient = new();

            //$ TODO: params
            PushTicketRequest pushReq = new()
            {
                PushTo = sendRequest.Tokens,
                PushTitle = sendRequest.Title,
                PushSubTitle = sendRequest.Subtitle,
                PushBody = sendRequest.Body,
                PushData = sendRequest.Data,
                PushBadgeCount = 33,
            };

            // If any of the logging-related stuff fails, we don't want to fail the overall send attempt.
            SqlConnection? sqlcn = null;
            PushNotificationLog? pnLog = null;
            try
            {
                sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                pnLog = await PushNotificationLog.Create(sqlcn,
                                                         DateTime.UtcNow,
                                                         Environment.MachineName,
                                                         "SendPushNotification",
                                                         String.Join(",", sendRequest.Tokens),
                                                         sendRequest.Title,
                                                         sendRequest.Subtitle,
                                                         sendRequest.Body,
                                                         sendRequest.Data);
            }
            catch
            {
                if (sqlcn != null)
                {
                    sqlcn.Close();
                }
                sqlcn = null;
            }

            try
            {
                PushTicketResponse pushResp = await pushClient.PushSendAsync(pushReq);
                try
                {
                    if (sqlcn != null)
                    {
                        if (pnLog != null)
                        {
                            await pnLog.UpdateStatus(sqlcn, "Success", JsonConvert.SerializeObject(pushResp));
                        }
                        sqlcn.Close();
                    }
                }
                catch
                {
                    // Just eat this exception.
                }

                if (pushResp == null || pushResp.PushTicketStatuses == null)
                {
                    return new ObjectResult("An error occurred processing the response from the server.")
                    {
                        StatusCode = 500,
                    };
                }
                if (pushResp.PushTicketErrors != null)
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
                if (pushResp.PushTicketStatuses.Count != sendRequest.Tokens.Count)
                {
                    // I'm leaving these vaguely-worded because it seems like bad practice to be too
                    // specific in error messages that might end up accidentally being user-visible
                    return new ObjectResult("An error occurred processing the server's responses.")
                    {
                        StatusCode = 500,
                    };
                }
                List<PushTokenSendResponse> results = new();
                for (int i = 0; i < sendRequest.Tokens.Count; i++)
                {
                    PushTokenSendResponse sr = new()
                    {
                        Token = sendRequest.Tokens[i],
                    };
                    PushTicketStatus status = pushResp.PushTicketStatuses[i];
                    if (status.TicketStatus.Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sr.Result = PushNotificationSendResult.Success;
                        sr.TicketId = status.TicketId;
                    }
                    else if (status.TicketStatus.Equals("error", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // This isn't very nice.
                        JObject jobj = (JObject)status.TicketDetails;
                        if (jobj == null || !jobj.HasValues)
                        {
                            // Not sure what to do here....
                            sr.Result = PushNotificationSendResult.Failure;
                        }
                        else
                        {
                            JToken errorToken = jobj["error"];
                            if (errorToken == null)
                            {
                                sr.Result = PushNotificationSendResult.Failure;
                            }
                            else
                            {
                                string errorMsg = errorToken.Value<string>();
                                if (errorMsg != null && errorMsg.Equals("DeviceNotRegistered", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    sr.Result = PushNotificationSendResult.DeviceNotRegistered;
                                }
                                else
                                {
                                    sr.Result = PushNotificationSendResult.Failure;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Don't know what to do here.  Just treat it as an error.
                        sr.Result = PushNotificationSendResult.Failure;
                    }

                    results.Add(sr);
                }

                PushNotificationSendResponse finalResponse = new()
                {
                    Results = results,
                };
                // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
                // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
                // quoting the way OkObjectResult does.
                return new ContentResult() { Content = JsonConvert.SerializeObject(finalResponse), ContentType = "application/json" };
            }
            catch (Exception e)
            {
                try
                {
                    if (sqlcn != null)
                    {
                        if (pnLog != null)
                        {
                            await pnLog.UpdateStatus(sqlcn, "Exception", e.Message);
                        }
                        sqlcn.Close();
                    }
                }
                catch
                {
                    // Just eat this exception.
                }

                //$ log this somewhere?
            }

            return new ObjectResult("An error occurred.")
            {
                StatusCode = 500,
            };
        }
    }
}
