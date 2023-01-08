using FzCommon;

public class EmailHelpers
{
    public static string GetGageLink(RegionBase region, SensorLocationBase location)
    {
        return GetGageLink(region, location.PublicLocationId);
    }

    public static string GetGageLink(RegionBase region, string publicLocationId)
    {
        return String.Format("{0}/gage/{1}", region.BaseURL, publicLocationId);
    }

    public static string GetGageAdminLink(RegionBase region, SensorLocationBase location)
    {
        return GetGageAdminLink(region, location.Id);
    }

    public static string GetGageAdminLink(RegionBase region, int locationId)
    {
        return String.Format("{0}/Locations/Edit/{1}", region.BaseURL, locationId);
    }

    public static string GetGageStatsLink(RegionBase region, int locationId)
    {
        return String.Format("{0}/Reports/Stats?locationId={1}", region.BaseURL, locationId);
    }

    public static string GetForecastLink(RegionBase region, string gageIds)
    {
        return String.Format("{0}/forecast?gageIds={1}", region.BaseURL, gageIds);
    }

    public static string GetForecastLink(RegionBase region)
    {
        return String.Format("{0}/forecast", region.BaseURL);
    }

    public static string GetBatteryColor(int? millivolt)
    {
        double percent = FzCommonUtility.CalculateBatteryVoltPercentage(millivolt) ?? 100;
        if (percent > 50)
        {
            return "green";
        }
        else if (percent > 25)
        {
            return "#c0c000";
        }
        else
        {
            return "red";
        }
    }

    public static string RenderDayOfWeek(RegionBase region, DateTime timestamp)
    {
        DateTime local = region.ToRegionTimeFromUtc(timestamp);
        return local.ToString("ddd");
    }
    
    public static string RenderTimestampNoDay(RegionBase region, DateTime timestamp)
    {
        DateTime local = region.ToRegionTimeFromUtc(timestamp);
        return local.ToString("M/d hh:mm tt");
    }
    
    public static string RenderTimestamp(RegionBase region, DateTime timestamp)
    {
        DateTime local = region.ToRegionTimeFromUtc(timestamp);
        return local.ToString("ddd M/d hh:mm tt");
    }
    
    public static string RenderFeet(double f)
    {
        return String.Format("{0:0.00} ft", f);
    }

    public static string RenderTimeDate(RegionBase region, DateTime dt)
    {
        DateTime local = region.ToRegionTimeFromUtc(dt);
        return local.ToString("h:mm tt, M/d/yyyy");
    }

    public static string RenderTime(RegionBase region, DateTime dt)
    {
        DateTime local = region.ToRegionTimeFromUtc(dt);
        return local.ToString("h:mm tt");
    }

}
