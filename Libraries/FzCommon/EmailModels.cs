using System.Diagnostics;
using System.Text;
using System.Web;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace FzCommon
{
    public abstract class EmailModel
    {
        // Logic taken from Old Floodzilla.
        public static string FormatSmartDate(RegionBase region, DateTime utc)
        {
            DateTime local = region.ToRegionTimeFromUtc(utc);
            DateTime utcNow = DateTime.UtcNow;
            string ret = null;
            if (Math.Abs((utc - utcNow).TotalDays) < 6)
            {
                ret = local.ToString("ddd h");
            }
            else
            {
                ret = local.ToString("M/d h");
            }
            if (local.Minute != 0)
            {
                ret += ":" + local.ToString("mm");
            }
            ret += local.ToString("tt").ToLower();
            return ret;
        }

        public const string SubjectHeader = "x-email-subject";

        public abstract string GetSourcePath();
        public abstract string Serialize();

        // If the model supports a SMS version, it should return the text here.
        public virtual string? GetSmsText()
        {
            throw new ApplicationException("GetSmsText not supported");
        }

        public class EmailText
        {
            public string Subject;
            public string Body;
        }

        //$ TODO: Error handling here -- send a generic something-is-broken message?
        public async Task<EmailText> GetEmailText()
        {
            string requestBody = this.Serialize();
            string url = String.Format("{0}{1}", FzConfig.Config[FzConfig.Keys.EmailSiteRoot], this.GetSourcePath());
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            string body = "";
            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content);
                body = await response.Content.ReadAsStringAsync();
                string subject = response.Headers.GetValues(SubjectHeader).First();
                return new EmailText() { Subject = subject, Body = body };
            }
            catch
            {
                // Dump the full details into the errors table in the DB for future diagnosis.
                ErrorManager.ReportError(ErrorSeverity.Major,
                                         "EmailModel.GetEmailText",
                                         String.Format("url: {0}\nrequest body:\n{1}\nresponse:\n{2}", url, requestBody, body),
                                         true);
                throw;
            }
        }

        public async Task SendEmail(string from, string recipientList)
        {
            EmailText text = await this.GetEmailText();
            using (EmailClient client = new EmailClient())
            {
                foreach (string recipient in recipientList.Split(','))
                {
                    await client.SendEmailAsync(from, recipient, text.Subject, text.Body, true);
                }
            }
        }

    }

    public class ResetPasswordEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/ResetPassword";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static ResetPasswordEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<ResetPasswordEmailModel>(body);
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CallbackUrl { get; set; }
    }

    public class VerifyEmailEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/VerifyEmail";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static VerifyEmailEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<VerifyEmailEmailModel>(body);
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CallbackUrl { get; set; }
    }

    public class VerifyPhoneSmsEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            throw new ApplicationException("Email delivery not supported for VerifyPhoneEmailModel");
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static VerifyPhoneSmsEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<VerifyPhoneSmsEmailModel>(body);
        }

        public override string? GetSmsText()
        {
            return "From Floodzilla: Please enter the following code on Floodzilla to verify your phone number: " + Code;
        }

        public string Code { get; set; }
    }

    public class LocationDownEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/LocationDown";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static LocationDownEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<LocationDownEmailModel>(body);
        }

        public RegionBase Region { get; set; }
        public SensorLocationBase Location { get; set; }
        public SensorReading LastReading { get; set; }
    }

    public class LocationRecoveredEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/LocationRecovered";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static LocationRecoveredEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<LocationRecoveredEmailModel>(body);
        }

        public RegionBase Region { get; set; }
        public SensorLocationBase Location { get; set; }
        public SensorReading LastReading { get; set; }
        public DateTime OfflineDetected { get; set; }
    }

    public class DailyStatusEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/DailyStatus";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static DailyStatusEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<DailyStatusEmailModel>(body);
        }

        public RegionBase Region { get; set; }
        public List<LocationStatus> Statuses { get; set; }
        public List<LocationStatistics> LatestStatistics { get; set; }

        // NOTE: All water-level-related fields are in feet above sea level
        [DebuggerDisplay("{LocationName} ({LocationId} / {PublicLocationId})")]
        public class LocationStatus
        {
            public int LocationId { get; set; }
            public string PublicLocationId { get; set; }
            public string LocationName { get; set; }
            public DateTime LastUpdate { get; set; }
            public int BatteryMillivolt { get; set; }
            public double WaterLevel { get; set; }
            public double? GroundHeight { get; set; }
            public double? Green { get; set; }
            public double? Brown { get; set; }
            public double? RoadSaddleHeight { get; set; }
            public bool IsActive { get; set; }
            public bool IsPublic { get; set; }
            public bool IsOffline { get; set; }
        }

        public class LocationStatistics
        {
            public int LocationId { get; set; }
            public string LocationName { get; set; }
            public DateTime DateInRegionTime { get; set; }
            public GageStatistics Stats { get; set; }
        }
    }

    public abstract class NotificationEmailModel : EmailModel
    {
        public string GetUnsubscribeLink()
        {
            string url = this.Region.BaseURL;
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            return String.Format("{0}user/unsubscribe?user={1}&email={2}", url, this.AspNetUser.AspNetUserId, HttpUtility.UrlEncode(this.AspNetUser.Email));
        }

        public async Task SendEmailToUserList(SqlConnection sqlcn,
                                              string from,
                                              List<UserBase> users,
                                              bool sendSms,
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
            SmsClient smsClient = new SmsClient();

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

                this.User = user;
                this.AspNetUser = aspNetUser;

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
                            await this.SendEmail(FzConfig.Config[FzConfig.Keys.EmailFromAddress], aspNetUser.Email);
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
                
                if (sendSms)
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
                                SmsSendResult smsResult = await smsClient.SendSms(aspNetUser.PhoneNumber, aspNetUser.Email, this);
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
            }
                

            if (sbResult != null)
            {
                sbResult.AppendFormat("Processed: {0} subscriptions: {1} notified by email, {2} notified by SMS, {3} email errors, {4} SMS errors, {5} invalid users, {6} email unconfirmed, {7} phone unconfirmed",
                                      users.Count,
                                      emailNotificationCount,
                                      smsNotificationCount,
                                      emailErrorCount,
                                      smsErrorCount,
                                      invalidCount,
                                      unconfirmedEmailCount,
                                      unconfirmedPhoneCount);
            }
        }

        public RegionBase Region { get; set; }
        public UserBase User { get; set; }
        public AspNetUserBase AspNetUser { get; set; }
    }

    public class GageEventEmailModel : NotificationEmailModel
    {
        public GageEvent GageEvent { get; set; }
        public SensorLocationBase Location { get; set; }

        public override string GetSourcePath()
        {
            throw new ApplicationException("GageEventEmailModels must implement GetSourcePath()");
        }

        public override string Serialize()
        {
            throw new ApplicationException("GageEventEmailModels must implement Serialize()");
        }

        protected string RenderFeet(double val)
        {
            return String.Format("{0:0.00} ft", val);
        }

        protected string RenderTimeDate(DateTime dt)
        {
            DateTime local = this.Region.ToRegionTimeFromUtc(dt);
            return local.ToString("h:mm tt, M/d");
        }

        protected string RenderTime(DateTime dt)
        {
            DateTime local = this.Region.ToRegionTimeFromUtc(dt);
            return local.ToString("h:mm tt");
        }
    }

    public class GageThresholdEventEmailModel : GageEventEmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/GageThresholdEvent";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static GageThresholdEventEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<GageThresholdEventEmailModel>(body);
        }

        // Helpers for SMS and Email
        public string GetLevelAndRoadDelta()
        {
            GageThresholdEventDetails detail = this.GageEvent.GageThresholdEventDetails;
            string ret = RenderFeet(detail.CurWaterLevel);
            if (detail.RoadSaddleHeight.HasValue)
            {
                double delta = detail.RoadSaddleHeight.Value - detail.CurWaterLevel;
                if (delta == 0)
                {
                    ret += " at road";
                }
                else if (delta > 0)
                {
                    ret += " / " + RenderFeet(delta) + " below road";
                }
                else if (delta < 0)
                {
                    ret += " / " + RenderFeet(-delta) + " over road";
                }
            }
            return ret;
        }

        public override string? GetSmsText()
        {
            string locName = this.Location.ShortName;
            if (String.IsNullOrEmpty(locName))
            {
                locName = this.Location.LocationName;
            }
            GageThresholdEventDetails detail = this.GageEvent.GageThresholdEventDetails;
            string status = "";
            switch (detail.NewStatus)
            {
                case ApiFloodLevel.Dry:
                    status = "Dry";
                    break;
                case ApiFloodLevel.Normal:
                    status = "Normal";
                    break;
                case ApiFloodLevel.NearFlooding:
                    status = "Near Flooding";
                    break;
                case ApiFloodLevel.Flooding:
                    status = "Flooding";
                    break;

                // these two shouldn't happen here...
                case ApiFloodLevel.Offline:
                    status = "Offline";
                    break;
                case ApiFloodLevel.Online:
                    status = "Online";
                    break;
            }
            string line1 = status + ": " + this.Location.PublicLocationId + " " + locName + " - Gage Status Changed\n";
            string line2 = GetLevelAndRoadDelta() + " @ " + RenderTimeDate(this.GageEvent.EventTime) + ".\n";
            string line3 = "";
            if (detail.Trends != null && detail.Trends.TrendValue.HasValue)
            {
                double trend = detail.Trends.TrendValue.Value;
                line3 = (trend > 0) ? "+" : "-";
                line3 += String.Format("{0:0.0} ft/hr.", Math.Abs(trend));
                if (detail.RoadCrossing.HasValue)
                {
                    line3 += " Road level @ " + RenderTime(detail.RoadCrossing.Value) + ".";
                }
                line3 += "\n";
            }
            string line4 = "floodzilla.com/gage/" + this.Location.PublicLocationId + "\n";
            string line5 = "STOP to opt out.\n";
            return line1 + line2 + line3 + line4 + line5;
        }

    }

    public class GageOnlineStatusEventEmailModel : GageEventEmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/GageOnlineStatusEvent";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static GageOnlineStatusEventEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<GageOnlineStatusEventEmailModel>(body);
        }

        public override string? GetSmsText()
        {
            string locName = this.Location.ShortName;
            if (String.IsNullOrEmpty(locName))
            {
                locName = this.Location.LocationName;
            }
            string line1 = this.Location.PublicLocationId + " " + locName + " - ";
            if (this.GageEvent.EventType == GageEventTypes.MarkedOffline)
            {
                line1 += "Offline for Maintenance\n";
            }
            else if (this.GageEvent.EventType == GageEventTypes.MarkedOnline)
            {
                line1 += "Online\n";
            }
            string line2 = "floodzilla.com/gage/" + this.Location.PublicLocationId + "\n";
            string line3 = "STOP to opt out.\n";
            return line1 + line2 + line3;
        }
    }

    public class ForecastEmailModel : NotificationEmailModel
    {
        public ForecastEmailModel()
        {
            this.GageForecasts = new List<ModelGageData>();
        }
        public override string GetSourcePath()
        {
            return "/Email/Forecast";
        }
        public override string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static ForecastEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<ForecastEmailModel>(body);
        }

        public enum ForecastContext
        {
            Alert,
            AsPublished,
            Daily,
        }

        public bool IsCurrentlyFlooding()
        {
            foreach (ModelGageData mgd in this.GageForecasts)
            {
                SensorReading max = mgd.GetRecentMax();
                if (max != null)
                {
                    if (max.WaterDischarge >= mgd.WarningCfsLevel)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // https://trello.com/c/kKgRQNeQ/188-flood-forecast-sms gives the format we're looking for here.
        public string? GetShortText(bool slackFormat)
        {
            if (!this.HasFlooding)
            {
                if (this.OldForecastHadFlooding)
                {
                    // If we had a flood last time around but don't any more, send an all-clear.
                    return "Flooding no longer predicted.";
                }
                else
                {
                    // Otherwise, there's no flooding in this forecast, so don't send anything.
                    return null;
                }
            }
            
            string message = "Flooding predicted:\n";
            foreach (ForecastEmailModel.ModelGageData mgd in this.GageForecasts)
            {
                foreach (NoaaForecastItem peak in mgd.Forecast.Peaks)
                {
                    if (peak.Discharge >= mgd.WarningCfsLevel)
                    {
                        message += String.Format("{0}: {1} {2:0}\n",
                                                 mgd.GageShortName,
                                                 EmailModel.FormatSmartDate(this.Region, peak.Timestamp),
                                                 peak.Discharge);
                    }
                }
            }
            message += String.Format("{0}/forecast", this.Region.SmsFormatBaseURL);
            return message;
        }
        
        public override string? GetSmsText()
        {
            return this.GetShortText(false);
        }
        
        public class ModelGageData
        {
            public string GageName { get; set; }
            public string GageId { get; set; }
            public string GageShortName { get; set; }
            public double GageRank { get; set; }
            public int UsgsSiteId { get; set; }
            public List<SensorReading> Readings { get; set; }
            public NoaaForecast Forecast { get; set; }
            public double? WarningCfsLevel { get; set; }
            public double? PredictedCfsPerHour { get; set; }

            public SensorReading GetRecentMax()
            {
                if (this.Readings == null || this.Readings.Count == 0)
                {
                    return null;
                }
                SensorReading maxReading = this.Readings[0];
                double maxDischarge = this.Readings[0].WaterDischarge.Value;
                foreach (SensorReading reading in this.Readings)
                {
                    if (reading.WaterDischarge.Value > maxDischarge)
                    {
                        maxReading = reading;
                        maxDischarge = reading.WaterDischarge.Value;
                    }
                }
                return maxReading;
            }
        }

        public ForecastContext Context { get; set; }
        public List<ModelGageData> GageForecasts { get; set; }
        public bool HasFlooding { get; set; }
        public bool OldForecastHadFlooding { get; set; }
    }
}
