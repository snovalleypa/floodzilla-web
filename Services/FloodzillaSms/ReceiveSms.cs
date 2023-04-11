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
            SqlConnection? sqlcn = null;
            SmsLog? smsLog = null;

            string fromNumber = req.Form["From"];
            string toNumber = req.Form["To"];
            string text = req.Form["Text"];

            // If the SQL connection and/or logging attempt fails, we don't want to fail the whole request.
            try
            {
                sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                smsLog = await SmsLog.Create(sqlcn,
                                             DateTime.UtcNow,
                                             Environment.MachineName,
                                             "ReceiveSms",
                                             fromNumber,
                                             toNumber,
                                             text);
            }
            catch
            {
                if (sqlcn != null)
                {
                    sqlcn.Close();
                }
                sqlcn = null;
            }

            if (String.Compare(text, "STOP", true) == 0)
            {
                try
                {
                    bool unsubscribed = false;
                    if (sqlcn == null)
                    {
                        throw new ApplicationException("Can't unsubscribe user -- no SQL connection");
                    }
                    unsubscribed = await UserBase.UnsubscribeSms(sqlcn, fromNumber);

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
                    if (smsLog != null)
                    {
                        try
                        {
                            await smsLog.UpdateStatus(sqlcn, "Unsubscribed", null);
                        }
                        catch
                        {
                            // Just eat this exception
                        }
                    }
                }
                catch (Exception e)
                {
                    if (smsLog != null)
                    {
                        try
                        {
                            await smsLog.UpdateStatus(sqlcn, "Exception", e.Message);
                        }
                        catch
                        {
                            // Just eat this exception
                        }
                    }
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
            else
            {
                if (smsLog != null)
                {
                    try
                    {
                        await smsLog.UpdateStatus(sqlcn, "Ignored", null);
                    }
                    catch
                    {
                        // Just eat this exception
                    }
                }
            }

            if (sqlcn != null)
            {
                sqlcn.Close();
            }
            return new ContentResult() { Content = response.ToString(), ContentType = "application/xml" };
        }
    }
}
