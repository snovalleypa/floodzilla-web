using System.Net;
using System.Net.Mail;
using Microsoft.Data.SqlClient;

using SendGrid;
using SendGrid.Helpers.Mail;

namespace FzCommon
{
    //$ TODO: Consider offloading the SendGrid stuff into a separate service the way
    //$ we do for the SMS and Push notification clients.
    public class EmailClient : IDisposable
    {
        public EmailClient()
        {
            m_sgClient = this.CreateSendGridClient();

#if DEBUG
            m_smtpClient = this.CreateSmtpClient();
#endif
        }

        public void SendEmail(SqlConnection sqlcn, string from, string recipient, string subject, string body, bool bodyIsHtml)
        {
            Task task = this.SendEmailAsync(sqlcn, from, recipient, subject, body, bodyIsHtml);
            task.Wait();
        }
        
        public async Task SendEmailAsync(SqlConnection sqlcn, string from, string recipient, string subject, string body, bool bodyIsHtml)
        {
            SendGridMessage msg = new SendGridMessage()
            {
                From = new EmailAddress(from),
                Subject = subject
            };
#if DEBUG
            msg.SetSandBoxMode(FzConfig.Config[FzConfig.Keys.UseSendGridSandboxMode].Equals("true", StringComparison.InvariantCultureIgnoreCase));
            string toOverride = FzConfig.Config[FzConfig.Keys.EmailToAddressOverride];
            if (!String.IsNullOrEmpty(toOverride))
            {
                if (toOverride == "none")
                {
                    return;
                }
                recipient = toOverride;
            }
#endif
            // If any of the logging-related stuff fails, we don't want to fail the overall send attempt.
            EmailLog? emailLog = null;
            try
            {
                emailLog = await EmailLog.Create(sqlcn,
                                                 DateTime.UtcNow,
                                                 Environment.MachineName,
                                                 from,
                                                 recipient,
                                                 subject,
                                                 body,
                                                 bodyIsHtml);

            }
            catch
            {
                // Nothing here.
            }

            msg.AddTo(recipient);
            if (bodyIsHtml)
            {
                msg.HtmlContent = body;
            }
            else
            {
                msg.PlainTextContent = body;
            }
            try
            {
                Response rsp = await m_sgClient.SendEmailAsync(msg);
                if (!rsp.IsSuccessStatusCode)
                {
                    throw new ApplicationException(String.Format("Unexpected response {0} sending mail: {1}", rsp.StatusCode, await rsp.Body.ReadAsStringAsync()));
                }

                try
                {
                    if (emailLog != null)
                    {
                        await emailLog.UpdateStatus(sqlcn, "Success", null);
                    }
                }
                catch
                {
                    // Nothing here.
                }
            }
            catch (Exception e)
            {
                try
                {
                    if (emailLog != null)
                    {
                        await emailLog.UpdateStatus(sqlcn, "Exception", e.Message);
                    }
                }
                catch
                {
                    // Nothing here.
                }

                // Rethrow the original exception
                throw;
            }

#if DEBUG
            // Note that 'recipient' is already overidden from above...
            this.SendSmtpEmail(from, recipient, subject, body, bodyIsHtml);
#endif
        }

#if DEBUG
        public void SendSmtpEmail(string from, string recipient, string subject, string body, bool bodyIsHtml)
        {
            if (m_smtpClient == null)
            {
                return;
            }
            using (MailMessage msg = new MailMessage(from, recipient, subject, body))
            {
                msg.IsBodyHtml = bodyIsHtml;
                m_smtpClient.Send(msg);
            }
        }
#endif

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
#if DEBUG
            if (disposing)
            {
                if (m_smtpClient != null)
                {
                    m_smtpClient.Dispose();
                    m_smtpClient = null;
                }
            }
#endif
        }

        private SendGridClient CreateSendGridClient()
        {
            return new SendGridClient(FzConfig.Config[FzConfig.Keys.SendGridApiKey]);
        }

        private SmtpClient CreateSmtpClient()
        {
            string localSmtpHost = FzConfig.Config[FzConfig.Keys.LocalSmtpHost];
            string localSmtpUser = FzConfig.Config[FzConfig.Keys.LocalSmtpUser];
            string localSmtpPass = FzConfig.Config[FzConfig.Keys.LocalSmtpPass];
            if (!String.IsNullOrEmpty(localSmtpHost) && !String.IsNullOrEmpty(localSmtpUser) && !String.IsNullOrEmpty(localSmtpPass))
            {
                SmtpClient client = new SmtpClient(localSmtpHost);
                client.Credentials = new NetworkCredential(localSmtpUser, localSmtpPass);
                return client;
            }

            return null;
        }

        private SendGridClient m_sgClient;
#if DEBUG
        private SmtpClient m_smtpClient;
#endif
    }
}
