using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Text;

namespace FzCommon
{
    public class NotificationManager
    {
        public NotificationManager()
        {
            this.smsClient = new();
            this.emailClient = new();
        }

        public async Task SendEmailModelToRecipientList(SqlConnection sqlcn,
                                                        EmailModel model,
                                                        string from,
                                                        string recipientList)
        {
            EmailModel.EmailText text = await model.GetEmailText();
            foreach (string recipient in recipientList.Split(','))
            {
                await this.emailClient.SendEmailAsync(sqlcn, from, recipient, text.Subject, text.Body, true);
            }
        }

        public async Task NotifyUserList(SqlConnection sqlcn, 
                                         NotificationEmailModel model,
                                         string from,
                                         List<UserBase> users,
                                         bool allowEmail,
                                         bool allowSms,
                                         bool allowPush,
                                         StringBuilder sbResult,
                                         StringBuilder sbDetails)
        {
            int invalidCount = 0;
            int unconfirmedEmailCount = 0;
            int emailNotificationCount = 0;
            int emailErrorCount = 0;
            int unconfirmedPhoneCount = 0;
            int smsNotificationCount = 0;
            int smsErrorCount = 0;
            int pushNotificationCount = 0;
            SmsClient smsClient = new SmsClient();

            string fromAddr = FzConfig.Config[FzConfig.Keys.EmailFromAddress];

            foreach (UserBase user in users)
            {
                AspNetUserBase aspNetUser = AspNetUserBase.GetAspNetUser(sqlcn, user.AspNetUserId);
                if (aspNetUser == null)
                {
                    invalidCount++;
                    continue;
                }
                if (user.IsDeleted)
                {
                    // do we want to count this?
                    continue;
                }

                model.User = user;
                model.AspNetUser = aspNetUser;

                if (allowEmail)
                {
                    if (user.NotifyViaEmail)
                    {
                        if (!aspNetUser.EmailConfirmed)
                        {
                            unconfirmedEmailCount++;
                        }
                        else
                        {
                            try
                            {
                                // NOTE: This will fetch a new copy of the HTML email text every time, which is
                                // currently required because the email body will contain customized pieces like an
                                // unsubscribe link.  It might be nice to separate those parts out so we don't have
                                // to fully fetch the email text each time, but that would require a more complicated
                                // system.
                                await this.SendEmailModelToRecipientList(sqlcn,
                                                                         model,
                                                                         fromAddr,
                                                                         aspNetUser.Email);
                                if (sbDetails != null)
                                {
                                    sbDetails.AppendFormat("Email sent to {0}\n", aspNetUser.Email);
                                }
                                emailNotificationCount++;
                            }
                            catch (Exception ex)
                            {
                                ErrorManager.ReportException(ErrorSeverity.Major, "EmailClient.SendEmailToUserList", ex);
                                if (sbDetails != null)
                                {
                                    sbDetails.AppendFormat("Email ERROR to {0}: {1}\n", aspNetUser.Email, ex.Message);
                                }
                                emailErrorCount++;
                            }
                        }
                    }
                }
                
                if (allowSms)
                {
                    if (user.NotifyViaSms)
                    {
                        if (!aspNetUser.PhoneNumberConfirmed)
                        {
                            unconfirmedPhoneCount++;
                        }
                        else
                        {
                            try
                            {
                                SmsSendResult smsResult = await smsClient.SendSms(aspNetUser.PhoneNumber, aspNetUser.Email, model);
                                switch (smsResult)
                                {
                                    case SmsSendResult.Success:
                                        smsNotificationCount++;
                                        if (sbDetails != null)
                                        {
                                            sbDetails.AppendFormat("SMS sent to {0}: {1}\n", aspNetUser.Email, smsResult);
                                        }
                                        break;

                                    case SmsSendResult.NotSending:
                                        // No message to send; just ignore this.
                                        break;

                                    case SmsSendResult.InvalidNumber:
                                    case SmsSendResult.Failure:
                                        if (sbDetails != null)
                                        {
                                            sbDetails.AppendFormat("SMS ERROR to {0}: {1}\n", aspNetUser.Email, smsResult);
                                        }
                                        smsErrorCount++;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorManager.ReportException(ErrorSeverity.Major, "EmailClient.SendEmailToUserList", ex);
                                if (sbDetails != null)
                                {
                                    sbDetails.AppendFormat("SMS ERROR to {0}: {1}\n", aspNetUser.Email, ex.Message);
                                }
                                smsErrorCount++;
                            }
                        }
                    }
                }

                if (allowPush)
                {
                    List<UserDevicePushToken> udpts = await UserDevicePushToken.GetTokensForUser(sqlcn, user.Id);
                    if (udpts.Count > 0)
                    {
                        pushNotificationCount++;
                        List<string> tokens = new();
                        foreach (UserDevicePushToken udpt in udpts)
                        {
                            tokens.Add(udpt.Token);
                        }
                        PushNotificationContents? pnc = model.GetPushNotificationContents();
                        if (pnc != null)
                        {
                            Dictionary<string, object> pushData = new();
                            pushData["path"] = pnc.Path;
                            await PushNotificationManager.SendNotification(sqlcn,
                                                                           tokens,
                                                                           pnc.Title,
                                                                           pnc.Subtitle,
                                                                           pnc.Body,
                                                                           JsonConvert.SerializeObject(pushData));
                        }
                    }
                }
            }
                

            if (sbResult != null)
            {
                sbResult.AppendFormat("Processed: {0} subscriptions: {1} notified by email, {2} notified by SMS, {3} email errors, {4} SMS errors, {5} invalid users, {6} email unconfirmed, {7} phone unconfirmed, {8} push notification users",
                                      users.Count,
                                      emailNotificationCount,
                                      smsNotificationCount,
                                      emailErrorCount,
                                      smsErrorCount,
                                      invalidCount,
                                      unconfirmedEmailCount,
                                      unconfirmedPhoneCount,
                                      pushNotificationCount);
            }
        }

        private SmsClient smsClient;
        private EmailClient emailClient;
    }
}
