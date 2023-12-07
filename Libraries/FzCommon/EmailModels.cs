using System.Diagnostics;
using System.Text;
using System.Web;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace FzCommon
{
    public class PushNotificationContents
    {
        public string? Title;
        public string? Subtitle;
        public string? Body;
        public string? Path;
    }

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

        public virtual string Serialize()
        {
            return JsonConvert.SerializeObject(this, modelSerializerSettings);
        }

        // If the model supports a SMS version, it should return the text here.
        public virtual string? GetSmsText()
        {
            throw new ApplicationException("GetSmsText not supported");
        }

        // If the model supports a push notification version, it should return the details here.
        public virtual PushNotificationContents? GetPushNotificationContents()
        {
            throw new ApplicationException("GetPushNotificationContents not supported");
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
                                         DateTime.UtcNow,
                                         true);
                throw;
            }
        }

        protected static JsonSerializerSettings modelSerializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            },
        };
    }

    public class ResetPasswordEmailModel : EmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/ResetPassword";
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

        internal class EventNotificationInfo
        {
            internal string GageStatusText;
            internal string RoadStatusText;
            internal string GageRateText;
            internal string GageLink;
        }

        private EventNotificationInfo GetNotificationInfo()
        {
            EventNotificationInfo eni = new();
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
            eni.GageStatusText = status + ": " + locName;
            eni.RoadStatusText = GetLevelAndRoadDelta() + " @ " + RenderTimeDate(this.GageEvent.EventTime) + ".";
            eni.GageRateText = "";
            if (detail.Trends != null && detail.Trends.TrendValue.HasValue)
            {
                double trend = detail.Trends.TrendValue.Value;
                eni.GageRateText = (trend > 0) ? "+" : "-";
                eni.GageRateText += String.Format("{0:0.0} ft/hr.", Math.Abs(trend));
                if (detail.RoadCrossing.HasValue)
                {
                    eni.GageRateText += " Road level @ " + RenderTime(detail.RoadCrossing.Value) + ".";
                }
            }
            eni.GageLink = "/gage/" + this.Location.PublicLocationId;
            return eni;
        }

        public override string? GetSmsText()
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            string ret = eni.GageStatusText + "\n" + eni.RoadStatusText + "\n";
            if (!String.IsNullOrWhiteSpace(eni.GageRateText))
            {
                ret += eni.GageRateText + "\n";
            }
            ret += String.Format("{0}{1}\n", this.Region.SmsFormatBaseURL, eni.GageLink);
            return ret;
        }

        public override PushNotificationContents? GetPushNotificationContents()
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            PushNotificationContents pnc = new();
            pnc.Title = eni.GageStatusText;
            pnc.Body = eni.RoadStatusText;
            if (!String.IsNullOrWhiteSpace(eni.GageRateText))
            {
                pnc.Body += "\n" + eni.GageRateText;
            }
            pnc.Path = eni.GageLink;
            return pnc;
        }
    }

    public class GageOnlineStatusEventEmailModel : GageEventEmailModel
    {
        public override string GetSourcePath()
        {
            return "/Email/GageOnlineStatusEvent";
        }
        public static GageOnlineStatusEventEmailModel Deserialize(string body)
        {
            return JsonConvert.DeserializeObject<GageOnlineStatusEventEmailModel>(body);
        }

        internal class EventNotificationInfo
        {
            internal string Text;
            internal string Link;
        }

        private EventNotificationInfo GetNotificationInfo()
        {
            EventNotificationInfo eni = new();
            string locName = this.Location.ShortName;
            if (String.IsNullOrEmpty(locName))
            {
                locName = this.Location.LocationName;
            }
            eni.Text = locName + " - ";
            switch (this.GageEvent.EventType)
            {
                case GageEventTypes.MarkedOffline:
                    eni.Text += "Offline for Maintenance";
                    break;
                case GageEventTypes.MarkedOnline:
                    eni.Text += "Online";
                    break;
            }
            eni.Link = "/gage/" + this.Location.PublicLocationId;
            return eni;
        }

        public override string? GetSmsText()
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            string line1 = eni.Text + "\n";
            string line2 = String.Format("{0}{1}\n", this.Region.SmsFormatBaseURL, eni.Link);
            return line1 + line2;
        }

        public override PushNotificationContents? GetPushNotificationContents()
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            PushNotificationContents pnc = new();
            pnc.Title = eni.Text;
            pnc.Path = eni.Link;
            return pnc;
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

        internal class EventNotificationInfo
        {
            internal string? Headline;
            internal List<string>? Details;
        }

        private EventNotificationInfo GetNotificationInfo()
        {
            EventNotificationInfo eni = new();
            if (!this.HasFlooding)
            {
                if (this.OldForecastHadFlooding)
                {
                    // If we had a flood last time around but don't any more, send an all-clear.
                    eni.Headline = "Flooding no longer predicted.";
                }
                else
                {
                    // Otherwise, there's no flooding in this forecast, so don't send anything.
                    eni.Headline = null;
                }
                return eni;
            }

            eni.Headline = "Flooding predicted";
            eni.Details = new();
            foreach (ForecastEmailModel.ModelGageData mgd in this.GageForecasts)
            {
                bool addedEntryForGauge = false;
                foreach (NoaaForecastItem peak in mgd.Forecast.Peaks)
                {
                    if (peak.Discharge >= mgd.WarningCfsLevel)
                    {
                        eni.Details.Add(String.Format("{0}: {1} {2:0}",
                                                      mgd.GageShortName,
                                                      EmailModel.FormatSmartDate(this.Region, peak.Timestamp),
                                                      peak.Discharge));
                        addedEntryForGauge = true;
                    }
                }
                // Special case (read: hack): if we haven't added any entries for this gauge, but the
                // first predicted value is above our warning level, that means we're likely in a situation
                // where the water is going down (we didn't add a peak for the first prediction because the
                // previous real data was above the predicted value).  We still want to make sure that we're
                // indicating that this gauge is still predicted to be above flood stage.
                if (!addedEntryForGauge)
                {
                    if (mgd.Forecast.Data.Count > 0 && mgd.Forecast.Data[0].Discharge >= mgd.WarningCfsLevel)
                    {
                        eni.Details.Add(String.Format("{0}: {1} {2:0}",
                                                      mgd.GageShortName,
                                                      EmailModel.FormatSmartDate(this.Region, mgd.Forecast.Data[0].Timestamp),
                                                      mgd.Forecast.Data[0].Discharge));
                    }
                }
            }
            return eni;
        }

        // https://trello.com/c/kKgRQNeQ/188-flood-forecast-sms gives the format we're looking for here.
        // NOTE: slackFormat is currently unused, but it might come in handy...
        public string? GetShortText(bool slackFormat)
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            if (String.IsNullOrEmpty(eni.Headline))
            {
                return null;
            }
            string message = eni.Headline + "\n";
            if (eni.Details != null && eni.Details.Count > 0)
            {
                foreach (string d in eni.Details)
                {
                    message += d + "\n";
                }
            }
            message += String.Format("{0}/forecast\n", this.Region.SmsFormatBaseURL);
            return message;
        }

        public override string? GetSmsText()
        {
            return this.GetShortText(false);
        }

        public override PushNotificationContents? GetPushNotificationContents()
        {
            EventNotificationInfo eni = this.GetNotificationInfo();
            if (String.IsNullOrEmpty(eni.Headline))
            {
                return null;
            }
            PushNotificationContents pnc = new();
            pnc.Title = eni.Headline;
            if (eni.Details != null && eni.Details.Count > 0)
            {
                pnc.Body = String.Join("\n", eni.Details);
            }
            pnc.Path = "/forecast";
            return pnc;
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
