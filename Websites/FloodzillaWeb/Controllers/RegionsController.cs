using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net.Http.Headers;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    //Google api class for converting address to lat and long
    public class AddressComponent
    {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public List<string> types { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Result
    {
        public List<AddressComponent> address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public string place_id { get; set; }
        public List<string> types { get; set; }
    }

    public class RootObject
    {
        public List<Result> results { get; set; }
        public string status { get; set; }
    } 
    
    [Authorize(Roles = "Admin,Organization Admin,Organization Member")]
    public class RegionsController : FloodzillaController
    {
        public RegionsController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
                : base(context, memoryCache, userPermissions)
        {
        }

        [NonAction]
        private List<SelectListItem> GetOrganizations()
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var orgs = _applicationCache.GetOrganizations().Where(e => e.IsActive == true).ToList();
            if (!User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                orgs = orgs.Where(e => e.OrganizationsId == user.OrganizationsId).ToList();
            }

            List<SelectListItem> selectListItems = new List<SelectListItem>();
            foreach (var item in orgs)
            {
                selectListItems.Add(new SelectListItem() { Text = item.Name, Value = item.OrganizationsId.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "-- Select Organization --", Value = "" });
            return selectListItems;
        }

        [NonAction]
        private async Task<Location> GeoCoordinates(string Address)
        {
            var location = new Location();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string gmapKey = FzConfig.Config[FzConfig.Keys.GoogleMapsApiKey];

            HttpResponseMessage response = await client.GetAsync("https://maps.googleapis.com/maps/api/geocode/json?address=" + Address + "&key="+ gmapKey);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                RootObject result = JsonConvert.DeserializeObject<RootObject>(json);
                if (result.status.ToLower() == "ok")
                {
                    location = result.results.First().geometry.location;
                }
            }
            return location;
        } 

        [NonAction]
        private void SetDropdownList(RegionBase currentRegion)
        {
            ViewBag.Organizations = GetOrganizations();
            ViewBag.Timezones = GetTimezones(currentRegion);
        }

        private bool RegionsExists(int id)
        {
            return _context.Regions.Any(e => e.RegionId == id);
        }

        public bool RegionsExistsByName(int id, string name)
        {
            return _context.Regions.Any(e => e.RegionId != id && e.RegionName == name);
        }

        // These are hardcoded because there's no great mapping available; the text
        // mangling is just a convenient hack.
        public List<SelectListItem> GetTimezones(RegionBase currentRegion)
        {
            List<SelectListItem> ret = new List<SelectListItem>();
            ret.Add(new SelectListItem()
            {
                Text = "Pacific",
                Value = "Pacific Standard Time|America/Los_Angeles",
                Selected = (currentRegion != null && currentRegion.WindowsTimeZone == "Pacific Standard Time"),
            });
            ret.Add(new SelectListItem()
            {
                Text = "Helsinki (For Testing)",
                Value = "FLE Standard Time|Europe/Helsinki",
                Selected = (currentRegion != null && currentRegion.WindowsTimeZone == "FLE Standard Time"),
            });
            return ret;
        }

        public async Task<IActionResult> GetRegions(bool showDeleted)
        {
            List<RegionBase> regions;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                regions = await RegionBase.GetAllRegions(sqlcn);
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                regions = regions.Where(e => !e.IsDeleted).ToList();
            }
            return Ok(JsonConvert.SerializeObject(new { Data = regions }));
        }
        
        // GET: Regions
        public IActionResult Index()
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var regions = _applicationCache.GetRegions();

            if (!User.IsInRole("Admin"))
            {
                var users = _applicationCache.GetUsers();

                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                regions = regions.Where(e => e.OrganizationsId == user.OrganizationsId).ToList();
            }

            return View(regions);
        }
        
        // GET: Regions/Create
        [Authorize(Roles = "Admin,Organization Admin")]
        public IActionResult Create()
        {
            SetDropdownList(null);
            return View();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Regions regions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var location= await GeoCoordinates(regions.Address);
                    if (location != null)
                    {
                        regions.Latitude = location.lat;
                        regions.Longitude = location.lng;
                    }

                    _context.Add(regions);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Region successfully added.";
                    _applicationCache.RemoveCache(CacheOptions.Regions);
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong. Please contact with service provider.";
                }
            }
            return View(regions);
        }

        // GET: Regions/Edit/5
        [Authorize(Roles = "Admin,Organization Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var regions = await _context.Regions.SingleOrDefaultAsync(m => m.RegionId == id);
            if (regions == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(regions.RegionId, GetAspNetUserId(), PermissionOptions.Region))
            {
                return Redirect("~/NotAuthorized");
            }

            SetDropdownList(regions);
            return View(regions);
        }

        
        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Regions regions, string isAddressChanged,int? CountValidTill, int? hiddenHours)
        {

            if (id != regions.RegionId)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(regions.RegionId, GetAspNetUserId(), PermissionOptions.Region))
            {
                return Redirect("~/NotAuthorized");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (isAddressChanged == "true")
                    {
                        var location = await GeoCoordinates(regions.Address);
                        if (location != null)
                        {
                            regions.Latitude = location.lat;
                            regions.Longitude = location.lng;
                        }
                    }

                    // Detaching any old region attached with _context.
                    _context.Entry<Regions>(regions).State = EntityState.Detached;

                    _context.Regions.Update(regions);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Region successfully updated.";
                    _applicationCache.RemoveCache(CacheOptions.Regions);
                    return RedirectToAction("Index");

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RegionsExists(regions.RegionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong. Please contact with service provider.";
                        return View();
                    }
                }
            }
            return View(regions);
        }

        /// <summary>
        /// It will soft delete the selected region as well as, soft delete all its dependent sub records
        /// in other tables. such as locations etc.
        /// </summary>
        /// <param name="deleteList"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {

            /* Map to delete records: 
             * Region-> (child)Flood events 
             * Region-> Location->(child)uploads-- device(optional.. need to discuss device deletion)
             * Right now devices are unlinking from the location but delete operation is not performing. 
             * Users will have to manually delete the devices.
             * Devices can be child and also can be independent. 
             */

            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();

                // Marking child regions delete
                var regions = _context.Regions.Where(e => ids.Contains(e.RegionId)).ToList();

                List<Regions> listToDeleteRegions = new List<Regions>();

                if (!User.IsInRole("Admin"))
                {
                    foreach (var item in regions)
                    {
                        if (_userPermissions.CheckPermission(item.RegionId, GetAspNetUserId(), PermissionOptions.Region))
                        {
                            listToDeleteRegions.Add(item);
                        }
                    }
                }
                else
                {
                    listToDeleteRegions.AddRange(regions);
                }

                if (listToDeleteRegions.Count > 0)
                {
                    listToDeleteRegions.ForEach(e => e.IsDeleted = true);
                    _context.Regions.UpdateRange(listToDeleteRegions);
                    _applicationCache.RemoveCache(CacheOptions.Regions, true);

                    var regionIds = listToDeleteRegions.Select(r => r.RegionId).ToList();

                    // Marking child flood events delete
                    var floodEvents = _context.FloodEvents.Where(e => regionIds.Contains(e.RegionId)).ToList();

                    if (floodEvents.Count > 0)
                    {
                        floodEvents.ForEach(e => e.IsDeleted = true);
                        _context.FloodEvents.UpdateRange(floodEvents);
                        _applicationCache.RemoveCache(CacheOptions.FloodEvents, true);
                    }

                    // Marking child locations delete
                    var locations = _context.Locations.Where(e => regionIds.Contains(e.RegionId)).ToList();

                    if (locations.Count > 0)
                    {
                        locations.ForEach(e => e.IsDeleted = true);
                        _context.Locations.UpdateRange(locations);
                        _applicationCache.RemoveCache(CacheOptions.Locations, true);
                        _applicationCache.RemoveCache(CacheOptions.UserLocations);
                        _applicationCache.RemoveCache(CacheOptions.UserNotifications);

                        var locationids = locations.Select(e => e.Id).ToList();

                        // Marking Upload images as delete
                        var uploads = _context.Uploads.Where(e => locationids.Contains(e.LocationId)).ToList();
                        if (uploads.Count > 0)
                        {
                            uploads.ForEach(e => e.IsDeleted = true);
                            _context.Uploads.UpdateRange(uploads);
                            _applicationCache.RemoveCache(CacheOptions.Uploads);
                        }

                        // child devices unlinking
                        var devices = _context.Devices.Where(e => locationids.Contains(e.LocationId ?? 0)).ToList();
                        if (devices.Count > 0)
                        {
                            devices.ForEach(e => e.LocationId = null);
                            _context.Devices.UpdateRange(devices);
                            _applicationCache.RemoveCache(CacheOptions.Devices, true);
                            _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                        }

                    }
                }
                await _context.SaveChangesAsync();
                if (ids.Count == 1)
                {
                    TempData["success"] = $"Region successfully deleted!";
                }
                else
                {
                    TempData["success"] = $"{ids.Count()} Regions successfully deleted!";
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
                    await RegionBase.MarkRegionsAsUndeleted(sqlcn, undeleteIds);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Region successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Regions successfully restored!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }
    }
}
