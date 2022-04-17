using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

using FzCommon;

namespace FloodzillaEmail.Controllers
{
    public class EmailController : Controller
    {
        public EmailController()
        {
        }

        private IActionResult EmailView(string viewName, string subject, object model)
        {
            ViewData["Subject"] = subject;
            HttpContext.Response.Headers.Add(FzCommon.EmailModel.SubjectHeader, subject);
            return View(viewName, model);
        }

        // This is for testing, so that we manually deserialize using the same System.Text.Json
        // settings used by the framework internally.
        private JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        //////////////////////////////////////////////////////////////
        // DailyStatus
        //////////////////////////////////////////////////////////////
        public class DailyStatusEmailModelWrapper
        {
            public DailyStatusEmailModelWrapper(DailyStatusEmailModel model) { Model = model; }
            public DailyStatusEmailModel Model;
            public readonly int LateSensorThreshold = 12;
            public readonly int BatteryThresholdPercent = 70;
            public List<DailyStatusEmailModel.LocationStatus> LateSensors;
            public List<DailyStatusEmailModel.LocationStatus> LowBattery;
        }
        public IActionResult DailyStatusForm(string postData)
        {
            DailyStatusEmailModel emailModel = JsonSerializer.Deserialize<DailyStatusEmailModel>(postData, GetOptions());
            return DailyStatusCore(new DailyStatusEmailModelWrapper(emailModel));
        }
        public IActionResult DailyStatus([FromBody]DailyStatusEmailModel model)
        {
            return DailyStatusCore(new DailyStatusEmailModelWrapper(model));
        }
        private IActionResult DailyStatusCore(DailyStatusEmailModelWrapper wrapper)
        {
            string subject = "Floodzilla daily status for " + wrapper.Model.Region.RegionName;
            wrapper.LateSensors = wrapper.Model.Statuses.Where(s => s.LastUpdate < DateTime.UtcNow.AddHours(-wrapper.LateSensorThreshold)).ToList();
            wrapper.LowBattery = wrapper.Model.Statuses.Where(s => (FzCommonUtility.CalculateBatteryVoltPercentage(s.BatteryMillivolt) ?? 100) < wrapper.BatteryThresholdPercent).ToList();
            return EmailView("DailyStatus", subject, wrapper);
        }

        //////////////////////////////////////////////////////////////
        // Forecast
        //////////////////////////////////////////////////////////////
        public class ForecastEmailModelWrapper
        {
            public ForecastEmailModelWrapper(ForecastEmailModel model) { Model = model; }
            public ForecastEmailModel Model;
            public string EmailSubject;
            public string EmailTitle;
            public string EmailSubtitle;
        }
        public IActionResult ForecastForm(string postData)
        {
            ForecastEmailModel emailModel = JsonSerializer.Deserialize<ForecastEmailModel>(postData, GetOptions());
            return ForecastCore(new ForecastEmailModelWrapper(emailModel));
        }
        public IActionResult Forecast([FromBody]ForecastEmailModel model)
        {
            return ForecastCore(new ForecastEmailModelWrapper(model));
        }
        private IActionResult ForecastCore(ForecastEmailModelWrapper wrapper)
        {
            switch (wrapper.Model.Context)
            {
                case ForecastEmailModel.ForecastContext.Daily:
                    wrapper.EmailSubject = "Floodzilla Daily Forecast";
                    wrapper.EmailTitle = "Floodzilla Daily Forecast";
                    wrapper.EmailSubtitle = "- Current Snoqualmie River forecast from the NWRFC";
                    break;
                case ForecastEmailModel.ForecastContext.AsPublished:
                    wrapper.EmailSubject = "Floodzilla Current Forecast";
                    wrapper.EmailTitle = "Floodzilla Current Forecast";
                    wrapper.EmailSubtitle = "- Newly published Snoqualmie River forecast from the NWRFC";
                    break;
                case ForecastEmailModel.ForecastContext.Alert:
                    wrapper.EmailSubject = "Floodzilla Forecast Alert";
                    wrapper.EmailTitle = "Floodzilla Forecast Alert";
                    wrapper.EmailSubtitle = "- Snoqualmie River forecast alerts from the NWRFC";
                    break;
            }
            return EmailView("Forecast", wrapper.EmailSubject, wrapper);
        }

        //////////////////////////////////////////////////////////////
        // GageOnlineStatusEvent
        //////////////////////////////////////////////////////////////
        public IActionResult GageOnlineStatusEventForm(string postData)
        {
            GageOnlineStatusEventEmailModel emailModel = JsonSerializer.Deserialize<GageOnlineStatusEventEmailModel>(postData, GetOptions());
            return GageOnlineStatusEventCore(emailModel);
        }
        public IActionResult GageOnlineStatusEvent([FromBody]GageOnlineStatusEventEmailModel model)
        {
            return GageOnlineStatusEventCore(model);
        }
        private IActionResult GageOnlineStatusEventCore(GageOnlineStatusEventEmailModel model)
        {
            string subject = model.Location.PublicLocationId + " " + model.Location.LocationName + " - Gage ";
            subject += (model.GageEvent.EventType == GageEventTypes.MarkedOffline ? "Offline for Maintenance" : "Online");
            return EmailView("GageOnlineStatusEvent", subject, model);
        }

        //////////////////////////////////////////////////////////////
        // GageThresholdEvent
        //////////////////////////////////////////////////////////////
        public class GageThresholdEventEmailModelWrapper
        {
            public GageThresholdEventEmailModelWrapper(GageThresholdEventEmailModel model) { Model = model; }
            public GageThresholdEventEmailModel Model;
            public GageEvent Evt;
            public GageThresholdEventDetails Detail;
            public string Status;
        }
        public IActionResult GageThresholdEventForm(string postData)
        {
            GageThresholdEventEmailModel emailModel = JsonSerializer.Deserialize<GageThresholdEventEmailModel>(postData, GetOptions());
            return GageThresholdEventCore(new GageThresholdEventEmailModelWrapper(emailModel));
        }
        public IActionResult GageThresholdEvent([FromBody]GageThresholdEventEmailModel model)
        {
            return GageThresholdEventCore(new GageThresholdEventEmailModelWrapper(model));
        }
        private IActionResult GageThresholdEventCore(GageThresholdEventEmailModelWrapper wrapper)
        {
            GageThresholdEventEmailModel model = wrapper.Model;
            wrapper.Evt = model.GageEvent;
            wrapper.Detail = wrapper.Evt.GageThresholdEventDetails;
            
            switch (wrapper.Detail.NewStatus)
            {
                case ApiFloodLevel.Dry:
                    wrapper.Status = "Dry";
                    break;
                case ApiFloodLevel.Normal:
                    wrapper.Status = "Normal";
                    break;
                case ApiFloodLevel.NearFlooding:
                    wrapper.Status = "Near Flooding";
                    break;
                case ApiFloodLevel.Flooding:
                    wrapper.Status = "Flooding";
                    break;

                // these two shouldn't happen here...
                case ApiFloodLevel.Offline:
                    wrapper.Status = "Offline";
                    break;
                case ApiFloodLevel.Online:
                    wrapper.Status = "Online";
                    break;
            }

            string subject = wrapper.Status + ": " + model.Location.PublicLocationId + " " + model.Location.LocationName + " - Gage Status Changed";
            return EmailView("GageThresholdEvent", subject, wrapper);
        }

        //////////////////////////////////////////////////////////////
        // LocationDown
        //////////////////////////////////////////////////////////////
        public IActionResult LocationDownForm(string postData)
        {
            LocationDownEmailModel emailModel = JsonSerializer.Deserialize<LocationDownEmailModel>(postData, GetOptions());
            return LocationDownCore(emailModel);
        }
        public IActionResult LocationDown([FromBody]LocationDownEmailModel model)
        {
            return LocationDownCore(model);
        }
        private IActionResult LocationDownCore(LocationDownEmailModel model)
        {
            string subject = "GAGE OFFLINE: " + model.Location.PublicLocationId + " - " + model.Location.LocationName;
            return EmailView("LocationDown", subject, model);
        }

        //////////////////////////////////////////////////////////////
        // LocationRecovered
        //////////////////////////////////////////////////////////////
        public IActionResult LocationRecoveredForm(string postData)
        {
            LocationRecoveredEmailModel emailModel = JsonSerializer.Deserialize<LocationRecoveredEmailModel>(postData, GetOptions());
            return LocationRecoveredCore(emailModel);
        }
        public IActionResult LocationRecovered([FromBody]LocationRecoveredEmailModel model)
        {
            return LocationRecoveredCore(model);
        }
        private IActionResult LocationRecoveredCore(LocationRecoveredEmailModel model)
        {
            string subject = "GAGE RECOVERED: " + model.Location.PublicLocationId + " - " + model.Location.LocationName;
            return EmailView("LocationRecovered", subject, model);
        }

        //////////////////////////////////////////////////////////////
        // ResetPassword
        //////////////////////////////////////////////////////////////
        public IActionResult ResetPasswordForm(string postData)
        {
            ResetPasswordEmailModel emailModel = JsonSerializer.Deserialize<ResetPasswordEmailModel>(postData, GetOptions());
            return ResetPasswordCore(emailModel);
        }
        public IActionResult ResetPassword([FromBody]ResetPasswordEmailModel model)
        {
            return ResetPasswordCore(model);
        }
        private IActionResult ResetPasswordCore(ResetPasswordEmailModel model)
        {
            string subject = "Reset Floodzilla Password";
            return EmailView("ResetPassword", subject, model);
        }

        //////////////////////////////////////////////////////////////
        // VerifyEmail
        //////////////////////////////////////////////////////////////
        public IActionResult VerifyEmailForm(string postData)
        {
            VerifyEmailEmailModel emailModel = JsonSerializer.Deserialize<VerifyEmailEmailModel>(postData, GetOptions());
            return VerifyEmailCore(emailModel);
        }
        public IActionResult VerifyEmail([FromBody]VerifyEmailEmailModel model)
        {
            return VerifyEmailCore(model);
        }
        private IActionResult VerifyEmailCore(VerifyEmailEmailModel model)
        {
            string subject = "Verify Floodzilla Email";
            return EmailView("VerifyEmail", subject, model);
        }
    }
}
