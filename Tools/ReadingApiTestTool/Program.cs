using FzCommon;
using Microsoft.Data.SqlClient;

const int DELAY = 1000 * 60 * 5;

FzConfig.Initialize();

using SqlConnection sqlcn = new(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);

await sqlcn.OpenAsync();

List<UsgsSite> usgsSites = await UsgsSite.GetUsgsSites(sqlcn);
List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);

List<SiteToCheck> sites = [];
foreach (UsgsSite site in usgsSites)
{
    List<DeviceBase> matches = devices.FindAll((d) => d.UsgsSiteId == site.SiteId);
    if (matches.Count == 0)
    {
        // Console.WriteLine("No device for {0} ({1})", site.SiteId, site.SiteName);
        continue;
    }
    foreach (DeviceBase dev in matches)
    {
        SensorLocationBase? loc = locations.Find((l) => l.Id == dev.LocationId);
        if (loc == null)
        {
            // Console.WriteLine("No location for {0} (device {1}: {2})", dev.LocationId, dev.DeviceId, dev.Name);
            continue;
        }
        if (!loc.IsActive || !loc.IsPublic)
        {
            continue;
        }
        sites.Add(new SiteToCheck(site, dev, loc));
        Console.WriteLine("Checking site {0} ({1})", site.SiteId, loc.LocationName);
    }
}

int tries = 0;
while (tries++ < 10000)
{
    Console.Write(".");
    foreach (SiteToCheck site in sites)
    {
        await site.CheckSite();
    }
    await Task.Delay(DELAY);
}

sqlcn.Close();

