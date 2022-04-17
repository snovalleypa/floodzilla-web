using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FzCommon
{
    public class EmailClient : IDisposable
    {
        public EmailClient()
        {
            m_sgClient = this.CreateSendGridClient();

#if DEBUG
            m_smtpClient = this.CreateSmtpClient();
#endif
        }
        
        public void SendEmail(string from, string recipient, string subject, string body, bool bodyIsHtml)
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
            msg.AddTo(recipient);
            if (bodyIsHtml)
            {
                msg.HtmlContent = body;
            }
            else
            {
                msg.PlainTextContent = body;
            }
            Task task = m_sgClient.SendEmailAsync(msg);
            task.Wait();

#if DEBUG
            // Note that 'recipient' is already overidden from above...
            this.SendSmtpEmail(from, recipient, subject, body, bodyIsHtml);
#endif
        }

        public async Task SendEmailAsync(string from, string recipient, string subject, string body, bool bodyIsHtml)
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
            msg.AddTo(recipient);
            if (bodyIsHtml)
            {
                msg.HtmlContent = body;
            }
            else
            {
                msg.PlainTextContent = body;
            }
            await m_sgClient.SendEmailAsync(msg);

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
