using System.Data;
using System.Net.Http.Headers;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace FzCommon
{
    public class SlackClient
    {
        public class SlackTextMessage
        {
            public string text;
        }

        public class SlackMrkdwnMessage
        {
            public SlackMrkdwnMessage(string message)
            {
                this.text = "Unused";
                this.blocks = new List<Section>();
                this.blocks.Add(new Section()
                {
                    type = "section",
                    text = new Submessage()
                    {
                        type = "mrkdwn",
                        text = message,
                    },
                });
            }
            public string text;
            public List<Section> blocks;
            public class Section
            {
                public string type;
                public Submessage text;
            }
            public class Submessage
            {
                public string text;
                public string type;
            }
        }

        public const string MrkdwnType = "mrkdwn";         // abbreviation courtesy of Slack
        public const string TextType = "text";

        // If you call this with mrkdwn text, you are expected to escape the string correctly...
        public static async Task NotifySlack(string url, string message, string messageType = "text")
        {
#if DEBUG
            string urlOverride = FzConfig.Config[FzConfig.Keys.SlackUrlOverride];
            if (!String.IsNullOrEmpty(urlOverride))
            {
                if (urlOverride == "none")
                {
                    url = null;
                }
                else
                {
                    url = urlOverride;
                }
            }
#endif
            if (!String.IsNullOrEmpty(url))
            {
                string requestBody;
                if (messageType == MrkdwnType)
                {
                    SlackMrkdwnMessage msg = new SlackMrkdwnMessage(message);
                    requestBody = JsonConvert.SerializeObject(msg);
                }
                else
                {
                    SlackTextMessage msg = new SlackTextMessage() { text = message };
                    requestBody = JsonConvert.SerializeObject(msg);
                }
                HttpClient client = new HttpClient();
                HttpContent content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
            }
        }

        public static string EscapeForMrkdwn(string s)
        {
            return (s == null) ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string FormatGageName(SensorLocationBase location)
        {
            string escapedName = EscapeForMrkdwn(location.ShortName ?? location.LocationName);
            return String.Format("{0} ({1})", escapedName, location.PublicLocationId);
        }

        private static string FormatLink(string title, string url)
        {
            return String.Format("<{0}|{1}>", EscapeForMrkdwn(url), EscapeForMrkdwn(title));
        }

        private static string FormatGageLink(string title, RegionBase region, SensorLocationBase location)
        {
            string url = String.Format("{0}/gage/{1}", region.BaseURL, location.PublicLocationId);
            return FormatLink(title, url);
        }

        private static string FormatReadingsLink(string title, RegionBase region, string ids)
        {
            string url = String.Format("{0}/Reports/ViewReadings?readings={1}", region.BaseURL, ids);
            return FormatLink(title, url);
        }

        private static string FormatLocationReadingsLink(string title, RegionBase region, SensorLocationBase location)
        {
            string url = String.Format("{0}/Reports/?locationId={1}", region.BaseURL, location.Id);
            return FormatLink(title, url);
        }

        // Logic taken from Old Floodzilla.
        private static string FormatSmartDate(RegionBase region, DateTime utc)
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

        //$ TODO: Region?
        //$ TODO: link to log
        public static void SendLogChangeNotification(int userId, string email, string objName, string changeDescription, string reason)
        {
            try
            {
                string message = String.Format("Site change by {0}: {1}: {2} {3}",
                                               email,
                                               EscapeForMrkdwn(objName),
                                               EscapeForMrkdwn(changeDescription),
                                               String.IsNullOrEmpty(reason) ? "" : "for reason: " + EscapeForMrkdwn(reason));

                string url = FzConfig.Config[FzConfig.Keys.SlackLogBookNotificationUrl];
                Task.WaitAll(NotifySlack(url, message, TextType));
            }
            catch
            {
                // just eat this, it's not business-critical
            }
        }

        //$ TODO: Region?
        //$ TODO: link to log
        public static async Task SendCreateLogBookEntryNotification(int userId, string email, List<string> tags, string text)
        {
            try
            {
                string message = String.Format("Log Book entry created by {0}: {1} (Tags: {2})",
                                               email,
                                               EscapeForMrkdwn(text),
                                               String.Join(", ", tags.Select(s => EscapeForMrkdwn(s))));

                string url = FzConfig.Config[FzConfig.Keys.SlackLogBookNotificationUrl];
                await NotifySlack(url, message, TextType);
            }
            catch
            {
                // just eat this, it's not business-critical
            }
        }

        public static async Task SendGageThresholdEventNotification(GageThresholdEventEmailModel model)
        {
            try
            {
                GageThresholdEventDetails detail = model.GageEvent.GageThresholdEventDetails;
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
                string message = String.Format("Gage status change: {0} is now {1}.  Reading {2:0.00} ft @{3}  {4} - {5}",
                                               FormatGageName(model.Location),
                                               status,
                                               detail.CurWaterLevel,
                                               model.Region.ToRegionTimeFromUtc(model.GageEvent.EventTime),
                                               FormatGageLink("Gage Link", model.Region, model.Location),
                                               FormatLocationReadingsLink("Readings", model.Region, model.Location));
                string url = FzConfig.Config[FzConfig.Keys.SlackGageNotificationUrl];
                await NotifySlack(url, message, TextType);
            }
            catch
            {
                // just eat this, it's not business-critical
            }
        }

        public static async Task SendFilteredReadingNotification(SqlConnection sqlcn,
                                                                 SensorLocationBase location,
                                                                 SensorReading badReading,
                                                                 double delta,
                                                                 double threshold)
        {
            try
            {
                RegionBase region = RegionBase.GetRegion(sqlcn, location.RegionId);
                DateTime regionTime = region.ToRegionTimeFromUtc(badReading.Timestamp);
                
                string escapedName = EscapeForMrkdwn(location.ShortName ?? location.LocationName);
                string message = String.Format("Bad reading at {0}: {1:0.00}ft @{2} is delta of {3:0.00} ft/hr, but threshold is {4:0.00} ft/hr -- "
                                               +"{5} - {6}",
                                               FormatGageName(location),
                                               badReading.WaterHeightFeet,
                                               regionTime,
                                               delta,
                                               threshold,
                                               FormatGageLink("Gage Link", region, location),
                                               FormatLocationReadingsLink("Readings", region, location));
                string url = FzConfig.Config[FzConfig.Keys.SlackGageNotificationUrl];
                await NotifySlack(url, message, TextType);
            }
            catch
            {
                // just eat this, it's not business-critical
            }
        }

        //$ TODO: Do I want to move all SQL usage out into the caller?
        private static async Task SendReadingsNotification(string action, int locationId, string ids, string user, string reason)
        {
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    SensorLocationBase location = SensorLocationBase.GetLocation(sqlcn, locationId);
                    RegionBase region = RegionBase.GetRegion(sqlcn, location.RegionId);
                    int idCount = ids.Split(',').Length;
                    string message = String.Format("Data Admin: {0}: {1} {2} {3} by {4}: {5} -- {6} - {7}",
                                                   FormatGageName(location),
                                                   idCount,
                                                   idCount == 1 ? "reading" : "readings",
                                                   action,
                                                   EscapeForMrkdwn(user),
                                                   EscapeForMrkdwn(reason),
                                                   FormatGageLink("Gage Link", region, location),
                                                   FormatReadingsLink("Readings", region, ids));
                    string url = FzConfig.Config[FzConfig.Keys.SlackGageNotificationUrl];
                    await NotifySlack(url, message, TextType);
                }
            }
            catch
            {
                // just eat this, it's not business-critical
            }
        }

        public static async Task SendReadingsDeletedNotification(int locationId, string deleteReadingIds, string user, string deleteReason)
        {
            await SendReadingsNotification("deleted", locationId, deleteReadingIds, user, deleteReason);
        }

        public static async Task SendReadingsUndeletedNotification(int locationId, string undeleteReadingIds, string user, string undeleteReason)
        {
            await SendReadingsNotification("undeleted", locationId, undeleteReadingIds, user, undeleteReason);
        }

        public static async Task SendForecastNotification(ForecastEmailModel emailModel)
        {
            string message = "Flooding? ";
            string url = FzConfig.Config[FzConfig.Keys.SlackForecastNotificationUrl];
            if (emailModel.IsCurrentlyFlooding() && !emailModel.HasFlooding)
            {
                message += "Yes, but new forecast is clear";
            }
            else if (emailModel.IsCurrentlyFlooding())
            {
                message += "Yes";
            }
            else if (emailModel.HasFlooding)
            {
                message += "Not now, but soon...";
            }
            else if (emailModel.OldForecastHadFlooding)
            {
                message += "Not any more...";
            }
            else
            {
                message += "No";
            }
            foreach (ForecastEmailModel.ModelGageData mgd in emailModel.GageForecasts)
            {
                foreach (NoaaForecastItem peak in mgd.Forecast.Peaks)
                {
                    if (peak.Discharge >= mgd.WarningCfsLevel)
                    {
                        message += String.Format("\n {0}: {1} {2:0}! {3}",
                                                 mgd.GageShortName,
                                                 FormatSmartDate(emailModel.Region, peak.Timestamp),
                                                 peak.Discharge,
                                                 FormatLink("View", String.Format("{0}/forecast?gageIds={1}", emailModel.Region.BaseURL, mgd.GageId)));
                    }
                }
            }
            await NotifySlack(url, message, TextType);
        }
    }
}
