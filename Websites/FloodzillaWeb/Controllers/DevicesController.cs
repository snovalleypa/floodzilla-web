using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin,Organization Admin")]
    public class DevicesController : FloodzillaController
    {
        public DevicesController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions) :
            base(context, memoryCache, userPermissions)
        {
        }

        [NonAction]
        private List<SelectListItem> GetRegions(int regionId=0)
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var regions = _applicationCache.GetRegions().Where(e => e.IsActive == true).ToList();

            if (User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                regions = regions.Where(e => e.OrganizationsId == user.OrganizationsId).ToList();
            }

            var selectListItems = new List<SelectListItem>();
            foreach (var item in regions)
            {
                selectListItems.Add(new SelectListItem { Text = item.RegionName, Value = item.RegionId.ToString() });
            }

            selectListItems.Insert(0, new SelectListItem { Text = "-- Select Region --", Value = "" });

            if (regionId != 0)
                if (selectListItems.Any(e => e.Value == regionId.ToString()))
                    selectListItems.Find(e => e.Value == regionId.ToString()).Selected = true;

            return selectListItems;
        }
        
        public List<SelectListItem> GetLocations(int regionId, int locationId=0)
        {

            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var locations = _applicationCache.GetLocations().Where(e => e.IsActive == true && e.RegionId==regionId).OrderBy(s => s.Latitude).OrderBy(s => s.Longitude).ToList();

            List<SelectListItem> selectListItems = new List<SelectListItem>();

            if (!User.IsInRole("Admin"))
            {
                if(!_userPermissions.CheckPermission(regionId, GetAspNetUserId(), PermissionOptions.Region))
                {
                    return selectListItems;
                }
            }

            foreach (var item in locations)
            {
                selectListItems.Add(new SelectListItem() { Text = item.LocationName, Value = item.Id.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "--Select Location--", Value = "" });

            if (locationId != 0)
                if (selectListItems.Any(e => e.Value == locationId.ToString()))
                    selectListItems.Find(e => e.Value == locationId.ToString()).Selected = true;

            return selectListItems;
        }

        [NonAction]
        private void SetDropdownList()
        {
            List<SelectListItem> deviceTypes = GetDeviceTypes();
            ViewBag.DeviceTypes = deviceTypes;
            ViewBag.AllowedNewDeviceTypes = deviceTypes.Where(e => e.Text != "SVPA");
        }

        [NonAction]
        private async Task SetUsgsSites()
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                ViewBag.UsgsSites = await UsgsSite.GetUsgsSites(sqlcn);
                sqlcn.Close();
            }
        }

        [NonAction]
        private async Task SetUsgsSiteChoices()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                foreach (var usgsSite in await UsgsSite.GetUsgsSites(sqlcn))
                {
                    selectListItems.Add(new SelectListItem { Text = usgsSite.SiteName, Value = usgsSite.SiteId.ToString() });
                }

                selectListItems.Insert(0, new SelectListItem { Text = "-- Select Usgs Site --", Value = "" });
                sqlcn.Close();
            }
            ViewBag.UsgsSiteChoices = selectListItems;
        }

        public async Task<IActionResult> GetDevices(bool includeLocations, bool showDeleted)
        {
            List<DeviceBase> devices;
            List<SensorLocationBase> locations = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                devices = await DeviceBase.GetDevicesAsync(sqlcn);
                if (includeLocations)
                {
                    locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                }
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                devices = devices.Where(e => !e.IsDeleted).ToList();
            }
            return Ok(JsonConvert.SerializeObject(new { Data = devices, Locations = locations }));
        }

        [NonAction]
        private List<SelectListItem> GetDeviceTypes()
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var deviceTypes = _applicationCache.GetDeviceTypes();

            var selectListItems = new List<SelectListItem>();
            foreach (var item in deviceTypes)
            {
                if (item.DeviceTypeId == 1)
                {
                    item.DeviceTypeName = "SVPA";
                }

                selectListItems.Add(new SelectListItem { Text = item.DeviceTypeName, Value = item.DeviceTypeId.ToString() });
            }

            selectListItems.Insert(0, new SelectListItem { Text = "-- Select Device Type --", Value = "" });
            return selectListItems;
        }

        public async Task<IActionResult> Index()
        {
            SetDropdownList();
            await SetUsgsSites();
            return View();
        }

        public async Task<IActionResult> Create()
        {
            SetDropdownList();
            await SetUsgsSiteChoices();
            ViewBag.Regions = GetRegions();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Devices device)
        {
            if (device.Version >= 3)
            {
                ModelState.Remove("AdctestsCount");
            }
            else
            {
                ModelState.Remove("SenseIterationMinutes");
                ModelState.Remove("SendIterationCount");
                ModelState.Remove("GPSIterationCount");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (device.LocationId != null)
                    {
                        if (!User.IsInRole("Admin"))
                        {

                            if (!_userPermissions.CheckPermission(device.LocationId ?? 0, GetAspNetUserId(), PermissionOptions.Location))
                            {
                                return Redirect("~/NotAuthorized");
                            }
                        }
                    }

                    device.Min = FzCommonUtility.GetRoundValue(device.Min);
                    device.Max = FzCommonUtility.GetRoundValue(device.Max);
                    device.MaxStDev = FzCommonUtility.GetRoundValue(device.MaxStDev);

                    _context.Devices.Add(device);

                    var newDeviceConfig = new DevicesConfiguration();
                    newDeviceConfig.DeviceId = device.DeviceId;
                    newDeviceConfig.MinutesBetweenBatches = 0;
                    newDeviceConfig.SecBetweenAdcsense = 0;
                    newDeviceConfig.AdctestsCount = device.AdctestsCount;
                    if (device.Version == null || device.Version < 3)
                    {
                        newDeviceConfig.GPSIterationCount = null;
                        newDeviceConfig.SendIterationCount = null;
                        newDeviceConfig.SenseIterationMinutes = null;
                    }
                    else
                    {
                        if (device.SendIterationCount == null || device.SenseIterationMinutes == null)
                        {
                            TempData["error"] = "Please fill all the required fields";
                            return RedirectToAction("Create");
                        }
                        newDeviceConfig.GPSIterationCount = device.GPSIterationCount;
                        newDeviceConfig.SendIterationCount = device.SendIterationCount;
                        newDeviceConfig.SenseIterationMinutes = device.SenseIterationMinutes;
                    }

                    _context.DevicesConfiguration.Add(newDeviceConfig);

                    _context.SaveChanges();
                    TempData["success"] = "Device successfully saved!";
                    _applicationCache.RemoveCache(CacheOptions.Devices);
                    _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                    _applicationCache.RemoveCache(CacheOptions.Locations);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    var sqlException = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
                    if (sqlException.Number == 2601 || sqlException.Number == 2627)
                    {
                        if (sqlException.Message.Contains("LocationUnique"))
                            TempData["error"] = "Location is already assign to another device.";
                        else
                            TempData["error"] = "Cannot insert duplicate Device ID.";
                    }
                    else
                        TempData["error"] = "Something went wrong.";
                }
            }
            SetDropdownList();
            await SetUsgsSiteChoices();
            ViewBag.Regions = GetRegions();
            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            var record = _context.Devices.Include(e=>e.Location).Include(e=>e.DevicesConfiguration).SingleOrDefault(e => e.DeviceId == id);

            if (record == null)
            {
                return NotFound();
            }

            SetDropdownList();
            await SetUsgsSiteChoices();
            if (record.LocationId != null)
            {
                if (!User.IsInRole("Admin"))
                {
                    if (!_userPermissions.CheckPermission(record.LocationId ?? 0, GetAspNetUserId(), PermissionOptions.Location))
                    {
                        return Redirect("~/NotAuthorized");
                    }
                }
                var regionId = _applicationCache.GetRegions().SingleOrDefault(e => e.RegionId == record.Location.RegionId).RegionId;
                ViewBag.Regions = GetRegions(regionId);
            }
            else
            {
                ViewBag.Regions = GetRegions();
            }

            record.Min = FzCommonUtility.GetRoundValue(record.Min);
            record.Max = FzCommonUtility.GetRoundValue(record.Max);
            record.MaxStDev = FzCommonUtility.GetRoundValue(record.MaxStDev);

            if (record.DevicesConfiguration != null)
            {
                record.AdctestsCount = record.DevicesConfiguration.AdctestsCount;
                record.GPSIterationCount = record.DevicesConfiguration.GPSIterationCount;
                record.SendIterationCount = record.DevicesConfiguration.SendIterationCount;
                record.SenseIterationMinutes = record.DevicesConfiguration.SenseIterationMinutes;
            }

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Devices device)
        {
            if (device.Version >= 3)
            {
                ModelState.Remove("AdctestsCount");
            }
            else
            {
                ModelState.Remove("SenseIterationMinutes");
                ModelState.Remove("SendIterationCount");
                ModelState.Remove("GPSIterationCount");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (device.LocationId != null)
                    {
                        if (!User.IsInRole("Admin"))
                        {
                            if (!_userPermissions.CheckPermission(device.LocationId ?? 0, GetAspNetUserId(), PermissionOptions.Location))
                            {
                                return Redirect("~/NotAuthorized");
                            }
                        }
                    }

                    device.Min = FzCommonUtility.GetRoundValue(device.Min);
                    device.Max = FzCommonUtility.GetRoundValue(device.Max);
                    device.MaxStDev = FzCommonUtility.GetRoundValue(device.MaxStDev);

                    // Detaching any old device attached with _context.
                    _context.Entry<Devices>(device).State = EntityState.Detached;

                    _context.Devices.Update(device);
                    _context.SaveChanges();

                    var deviceConfiguration = _context.DevicesConfiguration.SingleOrDefault(e => e.DeviceId == device.DeviceId);
                    if (deviceConfiguration != null)
                    {
                        deviceConfiguration.AdctestsCount = device.AdctestsCount;
                        if (device.Version == null || device.Version < 3)
                        {
                            deviceConfiguration.GPSIterationCount = null;
                            deviceConfiguration.SendIterationCount = null;
                            deviceConfiguration.SenseIterationMinutes = null;
                        }
                        else
                        {
                            if (device.SendIterationCount == null || device.SenseIterationMinutes == null)
                            {
                                TempData["error"] = "Please fill all the required fields";
                                return RedirectToAction("Create");
                            }

                            deviceConfiguration.GPSIterationCount = device.GPSIterationCount;
                            deviceConfiguration.SendIterationCount = device.SendIterationCount;
                            deviceConfiguration.SenseIterationMinutes = device.SenseIterationMinutes;
                        }
                        _context.DevicesConfiguration.Update(deviceConfiguration);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var newDeviceConfig = new DevicesConfiguration();
                        newDeviceConfig.DeviceId = device.DeviceId;
                        newDeviceConfig.MinutesBetweenBatches = 0;
                        newDeviceConfig.SecBetweenAdcsense = 0;
                        newDeviceConfig.AdctestsCount = device.AdctestsCount;
                        if (device.Version == null || device.Version < 3)
                        {
                            newDeviceConfig.GPSIterationCount = null;
                            newDeviceConfig.SendIterationCount = null;
                            newDeviceConfig.SenseIterationMinutes = null;
                        }
                        else
                        {
                            newDeviceConfig.GPSIterationCount = device.GPSIterationCount;
                            newDeviceConfig.SendIterationCount = device.SendIterationCount;
                            newDeviceConfig.SenseIterationMinutes = device.SenseIterationMinutes;
                        }

                        _context.DevicesConfiguration.Add(newDeviceConfig);
                        _context.SaveChanges();
                    }

                    LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), device, Request.Form["ChangeReason"]);

                    TempData["success"] = "Device successfully updated!";
                    _applicationCache.RemoveCache(CacheOptions.Devices);
                    _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                    _applicationCache.RemoveCache(CacheOptions.Locations);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    var sqlException = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
                    if (sqlException.Number == 2601 || sqlException.Number == 2627)
                    {
                        if (sqlException.Message.Contains("LocationUnique"))
                            TempData["error"] = "Location is already assigned to another device.";
                        else
                            TempData["error"] = "Cannot insert duplicate Device ID.";
                    }
                    else
                        TempData["error"] = "Something went wrong. Please contact SVPA.\n" + ex.Message;
                }
            }
            SetDropdownList();
            await SetUsgsSiteChoices();
            if (device.LocationId != null)
            {
                var regionId = _applicationCache.GetLocations().SingleOrDefault(e => e.Id == device.LocationId).RegionId;
                ViewBag.Regions = GetRegions(regionId);
            }
            else
                ViewBag.Regions = GetRegions();

            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {
            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();
                var devices = _context.Devices.Where(e => ids.Contains(e.DeviceId)).ToList();
                
                if (devices.Count > 0)
                {
                    foreach (var device in devices)
                    {
                        if (device.LocationId != null)
                        {
                            if (!User.IsInRole("Admin"))
                            {
                                if (!_userPermissions.CheckPermission(device.LocationId ?? 0, GetAspNetUserId(), PermissionOptions.Location))
                                {
                                    continue;
                                }
                            }
                        }

                        device.LocationId = null;
                        device.IsDeleted = true;
                        _context.Devices.Update(device);
                    }

                    await _context.SaveChangesAsync();
                    _applicationCache.RemoveCache(CacheOptions.Devices, true);
                    _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                    _applicationCache.RemoveCache(CacheOptions.Locations, true);
                    if (ids.Count == 1)
                    {
                        TempData["success"] = $"Device successfully deleted!";
                    }
                    else
                    {
                        TempData["success"] = $"{ids.Count} Devices successfully deleted!";
                    }
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Undelete(string undeleteList)
        {
            IEnumerable<int> undeleteIds = undeleteList.Split(',').Select(int.Parse);
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    await DeviceBase.MarkDevicesAsUndeleted(sqlcn, undeleteIds);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Device successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Devices successfully restored!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> GetLatestDeviceReading(int deviceId)
        {
            List<SensorReading> readings = await SensorReading.GetReadingsForDevice(deviceId, 1, null, null, 0);
            if (readings == null || readings.Count == 0)
            {
                return Ok(new { NoData = true });
            }

            //$ TODO: Figure out why default settings aren't get picked up here if
            //$ JsonConvert.SerializeObject() is used...
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore 
            };
            return new JsonResult(new { Reading = readings[0], TimeZone = FzCommon.FzCommonUtility.IanaTimeZone}, settings);
        }

        public bool CheckDeviceIdExist(int DeviceId)
        {
            var devices = _applicationCache.GetDevices();
            return devices.Any(e => e.DeviceId == DeviceId);
        }

    }
}
