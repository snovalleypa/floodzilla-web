using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Plivo;

using FzCommon;

[assembly: FunctionsStartup(typeof(FloodzillaSms.ReceiveSms))]

namespace FloodzillaSms
{
    public class ReceiveSms : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();

            //$ TODO: Any other configuration/initialization?
        }

        [FunctionName("ReceiveSms")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Plivo.XML.Response response = new Plivo.XML.Response();

            string fromNumber = req.Form["From"];
            string toNumber = req.Form["To"];
            string text = req.Form["Text"];

            if (String.Compare(text, "STOP", true) == 0)
            {

                try
                {
                    bool unsubscribed = false;
                    using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                    {
                        await sqlcn.OpenAsync();
                        unsubscribed = await UserBase.UnsubscribeSms(sqlcn, fromNumber);
                        sqlcn.Close();
                    }

                    // If we didn't find a matching number to unsubscribe, don't send any messages
                    if (unsubscribed)
                    {
                        response.AddMessage(FzConfig.Config[FzConfig.Keys.SmsStopResponse],
                                            new Dictionary<string, string>()
                        {
                            {"src", toNumber},
                            {"dst", fromNumber},
                            {"type", "sms"},
                            {"callbackUrl", FzConfig.Config[FzConfig.Keys.SmsReceiveServiceUrl]},
                            {"callbackMethod", "POST"}
                        });
                    }
                }
                catch
                {
                    //$ TODO: Log this?
                    response.AddMessage("There was an error processing your request. Contact floodzilla.support@svpa.us",
                                        new Dictionary<string, string>()
                    {
                        {"src", toNumber},
                        {"dst", fromNumber},
                        {"type", "sms"},
                        {"callbackUrl", FzConfig.Config[FzConfig.Keys.SmsReceiveServiceUrl]},
                        {"callbackMethod", "POST"}
                    });
                }
            }

            return new ContentResult() { Content = response.ToString(), ContentType = "application/xml" };
        }
    }
}
