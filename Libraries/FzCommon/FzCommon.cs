namespace FzCommon
{
    public class Constants
    {
        public const double BenchmarkOffsetFeet = 100.0;
        public const double BenchmarkOffsetInches = (BenchmarkOffsetFeet * 12.0);

        // These numbers are based on liquidlevels.com's battery display, which I know is
        // not particularly scientific...
        public const double BatteryLowMillivolts = 3000.0;
        public const double BatteryHighMillivolts = 4000.0;

        // If this ends up changing with any regularity at all it'll probably move into config.
        // Measured in feet per hour.  Any readings that represent a change bigger than this
        // amount will be marked as deleted.  Note that SensorLocationBase.MaxChangeThreshold can
        // override this on a per-gage basis.
        public const double DefaultMaxValidChangeThreshold = 2.0;

        // When possibly filtering out incoming readings, if the most recent
        // reading was more than this many minutes ago, it's ignored (i.e. any incoming
        // reading will be considered valid).
        public const int ChangeThresholdMaxMinutes = 120;

        public const int SvpaRegionId = 1;

        public const string FacebookLoginProviderName = "Facebook";
        public const string GoogleLoginProviderName = "Google";
        public const string AppleLoginProviderName = "Apple";
    }

    //$ TODO: This will eventually be moved to Azure App Configuration / Key Vaults.  For
    //$ now, it's here because at least this way it's only in one place instead of in 
    //$ a bunch of separate pieces of code/config.
    public class StorageConfiguration
    {
        public const string UploadsBlobContainer = "uploads";
        public const string MonitorBlobContainer = "monitor";
        public const string JobStatusBlobContainer = "jobstatus";

        public const string AzureImageUploadBaseUrl = "https://svpastorage.blob.core.windows.net/uploads";
    }

    public class FzCommonUtility
    {
        public static double? GetRoundValue(double? value)
        {
            if (value == null)
                return null;
            else
                return Math.Round(value ?? 0, 2);
        }

        public static double GetRoundValue(double value)
        {
            return Math.Round(value, 2);
        }

        //$ TODO: Allow different kinds of sensors to have different battery voltage ranges
        public static double? CalculateBatteryVoltPercentage(double? millivolts)
        {
            if (!millivolts.HasValue)
            {
                return null;
            }
            double percentage = 0;
            if (millivolts >= Constants.BatteryHighMillivolts)
            {
                percentage = 100;
            }
            else if (millivolts <= Constants.BatteryLowMillivolts)
            {
                percentage = 0;
            }
            else
            {
                percentage = (millivolts.Value - Constants.BatteryLowMillivolts) / (Constants.BatteryHighMillivolts - Constants.BatteryLowMillivolts);
                percentage *= 100f;
            }
            return GetRoundValue(percentage);
        }

        //$ TODO: Make timezone be per-region
        public const string WindowsTimeZone = "Pacific Standard Time";
        public const string IanaTimeZone = "America/Los_Angeles";

        //$ TODO: cache the TZI
        
        public static DateTime ToRegionTimeFromUtc(DateTime utc)
        {
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tzi);
        }

        public static DateTime ToUtcFromRegionTime(DateTime regionTime)
        {
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(regionTime, tzi);
        }
    }
}
