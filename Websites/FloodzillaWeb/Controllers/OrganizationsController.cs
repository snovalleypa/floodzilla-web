using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles ="Admin")]
    public class OrganizationsController : Controller
    {
        private readonly FloodzillaContext _context;
        private ApplicationCache _applicationCache;
        public OrganizationsController(FloodzillaContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _applicationCache = new ApplicationCache(_context, memoryCache);
        }

        public async Task<IActionResult> GetOrganizations(bool showDeleted)
        {
            List<OrganizationBase> organizations;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                organizations = await OrganizationBase.GetOrganizations(sqlcn);
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                organizations = organizations.Where(e => !e.IsDeleted).ToList();
            }
            return Ok(JsonConvert.SerializeObject(new { Data = organizations }));
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Organizations org)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Organizations.Add(org);
                    int result = _context.SaveChanges();
                    if (result > 0)
                    {
                        TempData["success"] = "Organization successfully saved!";
                        _applicationCache.RemoveCache(CacheOptions.Organizations);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong!";
                    }
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong!";
                }
            }
            return View();
        }

        public IActionResult Edit(int id)
        {
            var record = _context.Organizations.SingleOrDefault(e => e.OrganizationsId == id);
            if (record == null)
            {
                return NotFound();
            }
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Organizations org)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Detaching any old org attached with _context.
                    _context.Entry<Organizations>(org).State = EntityState.Detached;

                    _context.Organizations.Update(org);
                    int result = _context.SaveChanges();
                    if (result > 0)
                    {
                        TempData["success"] = "Organization successfully updated!";
                        _applicationCache.RemoveCache(CacheOptions.Organizations);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong!";
                    }
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong!";
                }
            }
            return View(org);
        }

        /// <summary>
        /// It will soft delete the selected region as well as, soft delete all its dependent sub records
        /// in other tables. such as locations etc.
        /// </summary>
        /// <param name="deleteOrgsIds"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {
            /* Map to delete records: 
             * Organization-> Users-> DataSubscriptions
             * Organization->Region-> (child)Flood events ->
             * Region-> Location->(child)uploads-- device(optional.. need to discuss device deletion)
             * Right now devices are unlinking from the location but delete operation is not performing. 
             * Users will have to manually delete the devices.
             * Devices can be child and also can be independent. 
             */

            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();
                var organizations = _context.Organizations.Where(e => ids.Contains(e.OrganizationsId)).ToList();

                if (organizations.Count > 0)
                {
                    organizations.ForEach(e => e.IsDeleted = true);
                    _context.Organizations.UpdateRange(organizations);

                    // Marking All users related to organization delete
                    var users = _context.Users.Include(e => e.AspNetUser).Where(e => ids.Contains(e.OrganizationsId ?? 0)).ToList();

                    if (users.Count > 0)
                    {
                        users.ForEach(e => e.AspNetUser.LockoutEnd = DateTimeOffset.MaxValue);
                        users.ForEach(e => e.IsDeleted = true);
                        _context.Users.UpdateRange(users);
                        _applicationCache.RemoveCache(CacheOptions.Users, true);

                        var dataSubscriptions = _context.DataSubscriptions.Where(e => users.Select(u => u.Id).Contains(e.UserId)).ToList();

                        if (dataSubscriptions.Count > 0)
                        {
                            dataSubscriptions.ForEach(e => e.IsDeleted = true);
                            _context.DataSubscriptions.UpdateRange(dataSubscriptions);
                            _applicationCache.RemoveCache(CacheOptions.DataSubscriptions, true);
                        }
                    }

                    // Marking child regions delete
                    var regions = _context.Regions.Where(e => organizations.Select(o => o.OrganizationsId).Contains(e.OrganizationsId)).ToList();

                    if (regions.Count > 0)
                    {
                        regions.ForEach(e => e.IsDeleted = true);
                        _context.Regions.UpdateRange(regions);
                        _applicationCache.RemoveCache(CacheOptions.Regions, true);

                        var regionIds = regions.Select(r => r.RegionId).ToList();

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
                    _applicationCache.RemoveCache(CacheOptions.Organizations, true);
                    if (ids.Count() == 1)
                    {
                        TempData["success"] = "Organization successfully deleted!";
                    }
                    else
                    {
                        TempData["success"] = $"{ids.Count()} Organizations successfully deleted!";
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
                    await OrganizationBase.MarkOrganizationsAsUndeleted(sqlcn, undeleteIds);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Organization successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Organizations successfully restored!";
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
