using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public enum ErrorSeverity
    {
        Critical,
        Major,
        Minor,
    }

    public class ErrorManager
    {
        public static void ReportError(ErrorSeverity sev, string source, string error, DateTime? errorTime = null, bool saveOnly = false)
        {
            SqlConnection sqlcn = null;
            DateTime reportTime;
            if (errorTime.HasValue)
            {
                reportTime = errorTime.Value;
            }
            else
            {
                reportTime = DateTime.UtcNow;
            }

            string errorText = String.Format("Site error: Severity {0}\r\nsource: {1}\r\nError: {2}", sev, source, error);
            
            // try/catch around all this stuff; if anything here fails, we're probably just dead anyway
            try
            {
                // Try to get a SQL connection, but if it fails, muddle through as well as possible.
                try
                {
                    sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                    sqlcn.Open();
                }
                catch
                {
                    sqlcn = null;
                }                
                
                // First: if this is a severe issue, try to report it via email/slack.
                if (sev != ErrorSeverity.Minor && !saveOnly)
                {
                    string email = GetNotifyEmailAddress(sqlcn);
                    if (!String.IsNullOrEmpty(email))
                    {
                        try
                        {
                            //$ TODO: Figure out if we care about the "from" address
                            new EmailClient().SendEmail(sqlcn, "SiteErrors@svpa.org", email, "Site Error", errorText, false);
                        }
                        catch
                        {
                            // oh well
                        }
                    }

                    string url = GetNotifySlackUrl(sqlcn);
                    if (!String.IsNullOrEmpty(url))
                    {
                        try
                        {
                            Task task = SlackClient.NotifySlack(url, errorText);
                            task.Wait();                            
                        }
                        catch
                        {
                            // oh well
                        }
                    }
                }

                // Now try to record the error.
                if (sqlcn != null)
                {
                    try
                    {
                        SiteError.SaveSiteError(sqlcn, reportTime, sev.ToString(), source, error);
                    }
                    catch
                    {
                        // sigh
                    }
                }

                if (sqlcn != null)
                {
                    try
                    {
                        sqlcn.Close();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        public static void ReportException(ErrorSeverity sev, string source, Exception ex, DateTime? exceptionTime = null)
        {
            ReportError(sev, source, String.Format("Exception: {0}", ex.ToString()), exceptionTime);
        }

        // remember sqlcn can be null, and FzConfig could throw...
        private static string GetNotifyEmailAddress(SqlConnection sqlcn)
        {
            try
            {
                if (sqlcn != null)
                {
                    SiteSettings ss = SiteSettings.GetSiteSettings(sqlcn);
                    if (ss != null)
                    {
                        return ss.SiteAdminEmail;
                    }
                }
            }
            catch
            {
            }

            try
            {
                // Fall back to looking in appconfig.
                var appSettings = FzConfig.GetAppSettingsFromFile();
                if (appSettings != null)
                {
                    return appSettings.SiteAdminEmail;
                }
            }
            catch
            {
            }
            return null;
        }

        // remember sqlcn can be null, and FzConfig could throw...
        private static string GetNotifySlackUrl(SqlConnection sqlcn)
        {
            try
            {
                if (sqlcn != null)
                {
                    SiteSettings ss = SiteSettings.GetSiteSettings(sqlcn);
                    if (ss != null)
                    {
                        return ss.SiteAdminSlackUrl;
                    }
                }
            }
            catch
            {
            }

            try
            {
                // Fall back to looking in appconfig.
                var appSettings = FzConfig.GetAppSettingsFromFile();
                if (appSettings != null)
                {
                    return appSettings.SiteAdminSlackUrl;
                }
            }
            catch
            {
            }
            return null;
        }

    }
}
