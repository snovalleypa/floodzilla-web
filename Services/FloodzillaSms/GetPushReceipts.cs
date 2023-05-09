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

[assembly: FunctionsStartup(typeof(FloodzillaSms.GetPushReceipts))]

namespace FloodzillaSms
{
    public class GetPushReceipts : FunctionsStartup
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

        [FunctionName("GetPushReceipts")]
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
            List<string> ticketIds = JsonConvert.DeserializeObject<List<string>>(body);
            PushApiClient pushClient = new();

            PushReceiptRequest receiptReq = new()
            {
                PushTicketIds = ticketIds,
            };

            // If any of the logging-related stuff fails, we don't want to fail the overall send attempt.
            SqlConnection? sqlcn = null;
            PushReceiptLog? prLog = null;
            try
            {
                sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                prLog = await PushReceiptLog.Create(sqlcn,
                                                    DateTime.UtcNow,
                                                    Environment.MachineName,
                                                    "GetPushReceipts",
                                                    String.Join(",", ticketIds));
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
                // Note typo in response type name -- the library we're using hasn't published a new version with
                // this fix yet...
                PushResceiptResponse receiptResp = await pushClient.PushGetReceiptsAsync(receiptReq);
                try
                {
                    if (sqlcn != null)
                    {
                        if (prLog != null)
                        {
                            await prLog.UpdateStatus(sqlcn, "Success", JsonConvert.SerializeObject(receiptResp));
                        }
                        sqlcn.Close();
                    }
                }
                catch
                {
                    // Just eat this exception.
                }

                if (receiptResp == null || (receiptResp.PushTicketReceipts == null && receiptResp.ErrorInformations == null))
                {
                    return new ObjectResult("An error occurred processing the response from the server.")
                    {
                        StatusCode = 500,
                    };
                }

                if (receiptResp.ErrorInformations != null || receiptResp.PushTicketReceipts == null)
                {
                    // There are no examples of what this looks like, and I can't find a way to force
                    // a response here.  For now I'm just going to assume there's an error that affects
                    // all tickets, and look in the log table to figure it out later.
                    return new ObjectResult("One or more push ticket errors occurred.")
                    {
                        StatusCode = 500,
                    };
                }

                List<PushNotificationReceipt> receipts = new();
                foreach (string ticketId in ticketIds)
                {
                    PushNotificationReceipt receipt = new()
                    {
                        TicketId = ticketId,
                    };
                    if (receiptResp.PushTicketReceipts.ContainsKey(ticketId))
                    {
                        PushTicketDeliveryStatus status = receiptResp.PushTicketReceipts[ticketId];
                        if (status.DeliveryStatus.Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                        {
                            receipt.Result = PushNotificationReceiptResult.Success;
                        }
                        else if (status.DeliveryStatus.Equals("error", StringComparison.InvariantCultureIgnoreCase))
                        {
                            receipt.Message = status.DeliveryMessage;
                            // This isn't very nice.
                            JObject jobj = (JObject)status.DeliveryDetails;
                            if (jobj == null || !jobj.HasValues)
                            {
                                // Not sure what to do here....
                                receipt.Result = PushNotificationReceiptResult.Error;
                            }
                            else
                            {
                                JToken errorToken = jobj["error"];
                                if (errorToken == null)
                                {
                                    receipt.Result = PushNotificationReceiptResult.Error;
                                }
                                else
                                {
                                    string errorMsg = errorToken.Value<string>();
                                    receipt.Error = errorMsg;
                                    if (errorMsg != null && errorMsg.Equals("DeviceNotRegistered", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        receipt.Result = PushNotificationReceiptResult.DeviceNotRegistered;
                                    }
                                    else
                                    {
                                        receipt.Result = PushNotificationReceiptResult.Error;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Don't know what to do here.
                            receipt.Result = PushNotificationReceiptResult.Error;
                        }
                            
                    }
                    else
                    {
                        receipt.Result = PushNotificationReceiptResult.Missing;
                    }

                    receipts.Add(receipt);
                }

                PushNotificationReceiptResponse finalResponse = new()
                {
                    Receipts = receipts,
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
                        if (prLog != null)
                        {
                            await prLog.UpdateStatus(sqlcn, "Exception", e.Message);
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
