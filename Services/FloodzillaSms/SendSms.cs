using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

using Plivo;

using FzCommon;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FloodzillaSms
{
    public class SendSms
    {
        [Function("SendSms")]
        [Consumes("application/json")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string body;
            Stream inputStream = req.Body;
            using (StreamReader sr = new(inputStream))
            {
                body = sr.ReadToEnd();
            }
            SmsSendRequest smsSendRequest = SmsSendRequest.Deserialize(body);

            string plivoId = FzConfig.Config[FzConfig.Keys.PlivoAuthId]!;
            string plivoToken = FzConfig.Config[FzConfig.Keys.PlivoAuthToken]!;
            string smsFrom = FzConfig.Config[FzConfig.Keys.SmsFromPhone]!;

            string phone = smsSendRequest.Phone;
            if (phone.StartsWith("999"))
            {
                if (phone == "9990000000")
                {
                    return new BadRequestResult();
                }

                using SqlConnection testSqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await testSqlcn.OpenAsync();
                await SendTestEmail(testSqlcn, smsSendRequest);
                return new OkObjectResult("Message Sent");
            }

            if (!phone.StartsWith("1") && !phone.StartsWith("0"))
            {
                phone = "1" + phone;
            }

            List<string> dest = [phone];

            PlivoApi api = new(plivoId, plivoToken);

            // If any of the logging-related stuff fails, we don't want to fail the overall send attempt.
            SqlConnection? sqlcn = null;
            SmsLog? smsLog = null;
            try
            {
                sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                smsLog = await SmsLog.Create(sqlcn,
                                             DateTime.UtcNow,
                                             Environment.MachineName,
                                             "SendSms",
                                             smsFrom,
                                             String.Join(",", dest),
                                             smsSendRequest.SmsText);
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
                Plivo.Resource.Message.MessageCreateResponse response = await api.Message.CreateAsync(src: smsFrom, dst: dest, text: smsSendRequest.SmsText);
                try
                {
                    if (sqlcn != null)
                    {
                        if (smsLog != null)
                        {
                            await smsLog.UpdateStatus(sqlcn, "Success", null);
                        }
                        sqlcn.Close();
                    }
                }
                catch
                {
                    // Just eat this exception.
                }
                return new OkObjectResult("Message Sent");
            }
            catch (Exception e)
            {
                try
                {
                    if (sqlcn != null)
                    {
                        if (smsLog != null)
                        {
                            await smsLog.UpdateStatus(sqlcn, "Exception", e.Message);
                        }
                        sqlcn.Close();
                    }
                }
                catch
                {
                    // Just eat this exception.
                }

                if (e is Plivo.Exception.PlivoValidationException)
                {
                    if (e.Message.Trim().StartsWith("{"))
                    {
                        Dictionary<string, object> errorData = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Message)!;
                        if (errorData.TryGetValue("error", out object? value))
                        {
                            string errorMessage = value.ToString()!;
                            if (errorMessage.Contains("not a valid phone", StringComparison.CurrentCultureIgnoreCase))
                            {
                                return new BadRequestResult();
                            }
                        }
                    }
                }

                //$ log this somewhere?
            }
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        private static async Task SendTestEmail(SqlConnection? sqlcn, SmsSendRequest smsSendRequest)
        {
            if (sqlcn == null)
            {
                return;
            }
            using (EmailClient client = new EmailClient())
            {
                string from = FzConfig.Config[FzConfig.Keys.EmailFromAddress]!;
                string subject = "Floodzilla Test SMS to " + smsSendRequest.Phone;
                string body = "Floodzilla Test SMS:\r\n" + smsSendRequest.SmsText;

                int at = smsSendRequest.Email.IndexOf('@');
                if (at == -1)
                {
                    return;
                }
                string recipient = smsSendRequest.Email.Substring(0, at) + "+" + smsSendRequest.Phone + smsSendRequest.Email.Substring(at);
                await client.SendEmailAsync(sqlcn, from, recipient, subject, body, false);
            }
        }
    }
}
