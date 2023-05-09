using Microsoft.Data.SqlClient;
using System.Text;

namespace FzCommon
{
    public class PushNotificationManager
    {
        public static async Task SendNotification(SqlConnection sqlcn,
                                                  List<string> tokens,
                                                  string? title,
                                                  string? subtitle,
                                                  string? body,
                                                  string? data)
        {
            PushNotificationClient client = new();
            DateTime sendTime = DateTime.UtcNow;
            List<PushNotificationAttempt> attempts = new();
            foreach (string token in tokens)
            {
                PushNotificationAttempt
                        att = await PushNotificationAttempt.CreateAsync(sqlcn, 
                                                                        token,
                                                                        title,
                                                                        subtitle,
                                                                        body,
                                                                        data,
                                                                        sendTime,
                                                                        sendTime, // lastCheckTime == sendTime
                                                                        PushNotificationAttemptStatus.New);
                attempts.Add(att);
            }

            await TrySend(sqlcn, client, attempts, tokens, title, subtitle, body, data);

        }

        //$ TODO: If we're ever concerned about either batch size or rate limiting,
        //$ this could enqueue all of these for separate processing.  For now, it's
        //$ not really a concern.
        private static async Task TrySend(SqlConnection sqlcn,
                                          PushNotificationClient client,
                                          List<PushNotificationAttempt> attempts,
                                          List<string> tokens,
                                          string? title,
                                          string? subtitle,
                                          string? body,
                                          string? data)
        {
            PushNotificationSendResponse? response = await client.SendPushNotification(tokens, title, subtitle, body, data);
            if (response == null)
            {
                string error = String.Format("Empty response from PushNotificationClient. Attempt id {0}", attempts[0].Id);
                ErrorManager.ReportError(ErrorSeverity.Major, "PushNotificationManager", error);

                foreach (PushNotificationAttempt att in attempts)
                {
                    await att.Retry(sqlcn);
                }
                return;
            }

            foreach (PushNotificationAttempt att in attempts)
            {
                PushTokenSendResponse? tsr = null;
                foreach (PushTokenSendResponse candidate in response.Results)
                {
                    if (candidate.Token == att.Token)
                    {
                        tsr = candidate;
                        break;
                    }
                }
                if (tsr == null)
                {
                    string error = String.Format("No push notification response for token {0}, attempt id {1}", att.Token, att.Id);
                    ErrorManager.ReportError(ErrorSeverity.Major, "PushNotificationManager", error);
                    await att.Fail(sqlcn);
                }
                else
                {
                    switch (tsr.Result)
                    {
                        case PushNotificationSendResult.Success:
                            att.TicketId = tsr.TicketId;
                            await att.AttemptWasSent(sqlcn);
                            break;

                            //$ TODO: Is it possible that some of these failures should be retried?
                            //$ If so, how do we know which ones are retryable?
                        case PushNotificationSendResult.Failure:
                            await att.Fail(sqlcn);
                            break;

                        case PushNotificationSendResult.DeviceNotRegistered:
                            await ProcessDeviceNotRegistered(sqlcn, att);
                            break;
                    }
                    await att.SaveAsync(sqlcn);
                }
            }
        }

        public static async Task ProcessAttempts(SqlConnection sqlcn, StringBuilder sbDetails, StringBuilder sbSummary)
        {
            PushNotificationClient client = new();
            DateTime checkTime = DateTime.UtcNow;
            List<PushNotificationAttempt> attempts = await PushNotificationAttempt.GetActiveAttemptsAsync(sqlcn, checkTime);

            // Might as well batch these up.
            List<PushNotificationAttempt> sentAttempts = new();
            List<string> sentTicketIds = new();

            foreach (PushNotificationAttempt att in attempts)
            {
                att.LastCheckTime = checkTime;
                switch (att.Status)
                {
                    case PushNotificationAttemptStatus.New:
                        List<PushNotificationAttempt> retryAttempts = new();
                        retryAttempts.Add(att);
                        List<string> retryTokens = new();
                        retryTokens.Add(att.Token);

                        await TrySend(sqlcn, client, retryAttempts, retryTokens, att.Title, att.Subtitle, att.Body, att.Data);
                        break;

                    case PushNotificationAttemptStatus.Sent:
                        sentAttempts.Add(att);
                        sentTicketIds.Add(att.TicketId);
                        break;
                }
            }

            

            if (sentAttempts.Count > 0)
            {
                PushNotificationReceiptResponse? response = await client.GetPushReceipts(sentTicketIds);
                if (response == null)
                {
                    string error = "Empty response from PushNotificationClient.GetPushReceipts";
                    ErrorManager.ReportError(ErrorSeverity.Major, "PushNotificationManager", error);

                    foreach (PushNotificationAttempt att in sentAttempts)
                    {
                        await att.Retry(sqlcn);
                    }
                    return;
                }
                foreach (PushNotificationAttempt att in sentAttempts)
                {
                    PushNotificationReceipt? found = null;
                    foreach (PushNotificationReceipt candidate in response.Receipts)
                    {
                        if (candidate.TicketId == att.TicketId)
                        {
                            found = candidate;
                            break;
                        }
                    }

                    if (found == null || found.Result == PushNotificationReceiptResult.Missing)
                    {
                        string error = String.Format("No push notification receipt for ticket {0}, attempt id {1}", att.TicketId, att.Id);
                        ErrorManager.ReportError(ErrorSeverity.Major, "PushNotificationManager", error);
                        await att.Fail(sqlcn);
                    }

                    switch (found.Result)
                    {
                        case PushNotificationReceiptResult.Success:
                            // yay
                            await att.Succeed(sqlcn);
                            break;
                            
                        case PushNotificationReceiptResult.Error:
                            await att.Retry(sqlcn);
                            break;

                        case PushNotificationReceiptResult.DeviceNotRegistered:
                            await ProcessDeviceNotRegistered(sqlcn, att);
                            break;
                    }
                }
            }
        }

        private static async Task ProcessDeviceNotRegistered(SqlConnection sqlcn, PushNotificationAttempt att)
        {
            try
            {
                await PushDeviceLog.Create(sqlcn,
                                           DateTime.UtcNow,
                                           Environment.MachineName,
                                           PushDeviceLog.EntryType_Removed,
                                           att.Token,
                                           null,
                                           null,
                                           null,
                                           String.Format("PushNotificationAttempt id {0}", att.Id));
            }
            catch
            {
                // Eat the exception -- if the log fails we still want to survive
            }
            await UserDevicePushToken.RemoveToken(sqlcn, att.Token);
            await att.Fail(sqlcn);
        }
    }
}
