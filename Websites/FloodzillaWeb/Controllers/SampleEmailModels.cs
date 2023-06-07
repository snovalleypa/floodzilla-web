using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

using FzCommon;
using FzCommon.Processors;

// The object IDs loaded here are from the SVPA database.  It would certainly be
// possible to build mock objects here instead.

namespace FloodzillaWeb.Controllers
{
    public class SampleEmailModels
    {
        public const int REGIONID_SVPA = 1;
        public const int EVENTID_REDRISING = 869;
        public const int EVENTID_MARKED_OFFLINE = 848;
        public const int FORECASTID_FLOOD = 10949;
        public const int FORECASTID_ALLCLEAR = 10973;

        public static async Task<GageEventEmailModel> BuildGageThresholdEventEmailModel(SqlConnection sqlcn)
        {
            RegionBase region = await RegionBase.GetRegionAsync(sqlcn, REGIONID_SVPA);
            GageEvent evt = await GageEvent.LoadAsync(sqlcn, EVENTID_REDRISING);
            SensorLocationBase location = await SensorLocationBase.GetLocationAsync(sqlcn, evt.LocationId);
            return new GageThresholdEventEmailModel()
            {
                GageEvent = evt,
                Region = region,
                Location = location,
            };
        }

        public static GageEventEmailModel DeserializeGageThresholdEvent(string json)
        {
            GageEventEmailModel emailModel = JsonConvert.DeserializeObject<GageThresholdEventEmailModel>(json);
            if (emailModel.GageEvent == null)
            {
                throw new ApplicationException("Invalid email model");
            }
            return emailModel;
        }

        public static async Task<GageEventEmailModel> BuildGageOnlineStatusEventEmailModel(SqlConnection sqlcn)
        {
            RegionBase region = await RegionBase.GetRegionAsync(sqlcn, REGIONID_SVPA);
            GageEvent evt = await GageEvent.LoadAsync(sqlcn, EVENTID_MARKED_OFFLINE);
            SensorLocationBase location = await SensorLocationBase.GetLocationAsync(sqlcn, evt.LocationId);
            return new GageOnlineStatusEventEmailModel()
            {
                GageEvent = evt,
                Region = region,
                Location = location,
            };
        }

        public static GageEventEmailModel DeserializeGageOnlineStatusEventEmailModel(string json)
        {
            GageEventEmailModel emailModel = JsonConvert.DeserializeObject<GageOnlineStatusEventEmailModel>(json);
            if (emailModel.GageEvent == null)
            {
                throw new ApplicationException("Invalid email model");
            }
            return emailModel;
        }

        public static async Task<ForecastEmailModel> BuildFloodingForecastEmailModel(SqlConnection sqlcn)
        {
            NoaaForecastSet current = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, FORECASTID_FLOOD);
            return await BuildForecastEmailModel(sqlcn, current);
        }

        public static async Task<ForecastEmailModel> BuildAllClearForecastEmailModel(SqlConnection sqlcn)
        {
            NoaaForecastSet current = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, FORECASTID_ALLCLEAR);
            return await BuildForecastEmailModel(sqlcn, current);
        }

        private static async Task<ForecastEmailModel> BuildForecastEmailModel(SqlConnection sqlcn, NoaaForecastSet current)
        {
            int minId = current.Forecasts[0].ForecastId;
            foreach (var forecast in current.Forecasts)
            {
                if (forecast.ForecastId < minId)
                {
                    minId = forecast.ForecastId;
                }
            }

            // Assume that we didn't skip any IDs.  This is test code, it's fine.
            NoaaForecastSet prev = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, minId - 1);
            return await NoaaForecastProcessor.BuildEmailModel(sqlcn, current, prev);
        }

        public static ForecastEmailModel DeserializeForecastEmailModel(string json)
        {
            ForecastEmailModel emailModel = JsonConvert.DeserializeObject<ForecastEmailModel>(json);
            if (emailModel.GageForecasts == null || emailModel.GageForecasts.Count < 1)
            {
                throw new ApplicationException("Invalid email model");
            }
            return emailModel;
        }

    }
}

