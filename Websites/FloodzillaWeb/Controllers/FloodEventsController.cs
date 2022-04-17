using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    [Authorize(Roles = "Admin,Organization Admin,Organization Member")]
    public class FloodEventsController : FloodzillaController
    {
        public FloodEventsController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
                : base(context, memoryCache, userPermissions)
        {
        }

        private List<SelectListItem> GetLocations(int regionId)
        {
            var locations = _applicationCache.GetLocations().Where(e => e.IsActive == true && e.RegionId == regionId).ToList();
            var selectListItems = new List<SelectListItem>();
            foreach (var item in locations)
            {
                selectListItems.Add(new SelectListItem { Text = item.LocationName, Value = item.Id.ToString() });
            }
            return selectListItems;
        }

        // GET: FloodEvents
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetFloodEvents(bool showDeleted)
        {
            List<FloodEventBase> floodEvents;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                floodEvents = await FloodEventBase.GetFloodEvents(sqlcn);
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                floodEvents = floodEvents.Where(e => !e.IsDeleted).ToList();
            }

            //$ TODO: Support multiple regions here? Or just look up the correct region?            
            return Ok(JsonConvert.SerializeObject(new { Data = floodEvents, TimeZone = FzCommon.FzCommonUtility.IanaTimeZone }));
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        // GET: FloodEvents/Create
        public IActionResult Create()
        {
            //$ TODO: region
            var locations = GetLocations(FzCommon.Constants.SvpaRegionId);
            MultiSelectList multiLocations = new MultiSelectList(locations, "Value", "Text");
            ViewBag.Locations = multiLocations;
            ViewBag.TimeZone = FzCommon.FzCommonUtility.IanaTimeZone;
           return View();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        // POST: FloodEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FloodEvents model, List<string> LocationIds)
        {
            if(ModelState.IsValid)
            {
                try
                {
                    if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(model.RegionId, GetAspNetUserId(), PermissionOptions.Region))
                    {
                        return Redirect("~/NotAuthorized");
                    }

                    foreach (var id in LocationIds)
                    {
                        if(!_userPermissions.CheckPermission(Convert.ToInt32(id),GetAspNetUserId(), PermissionOptions.Location))
                        {
                            LocationIds.Remove(id);
                        }
                    }

                    _context.FloodEvents.Add(model);
                    _context.SaveChanges();

                    var eventDetais = new List<EventsDetail>();

                    foreach (var item in LocationIds)
                    {
                        eventDetais.Add(new EventsDetail { EventId = model.Id, LocationId = Convert.ToInt32(item) });
                    }
                    _context.EventsDetail.AddRange(eventDetais);
                    _context.SaveChanges();

                    TempData["success"] = "Event successfully added.";
                    _applicationCache.RemoveCache(CacheOptions.FloodEvents);
                    _applicationCache.RemoveCache(CacheOptions.EventsDetail);
                    return RedirectToAction("Index");
                }
                catch(Exception)
                {
                    TempData["error"] = "Something went wrong.";
                }
            }

            //$ TODO: region
            var locations = GetLocations(FzCommon.Constants.SvpaRegionId);
            MultiSelectList multiLocations = new MultiSelectList(locations, "Value", "Text");
            ViewBag.Locations = multiLocations;
            ViewBag.TimeZone = FzCommon.FzCommonUtility.IanaTimeZone;
            return View(model);
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        // GET: FloodEvents/Edit/5
        public IActionResult Edit(int id)
        {
            var floodEvent = _context.FloodEvents.Include(e=>e.EventsDetail).SingleOrDefault(e => e.Id == id);
            if (floodEvent == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(floodEvent.Id, GetAspNetUserId(), PermissionOptions.FloodEvent))
            {
                return Redirect("~/NotAuthorized");
            }

            //$ TODO: region
            var locations = GetLocations(FzCommon.Constants.SvpaRegionId);
            MultiSelectList multiLocations = new MultiSelectList(locations, "Value", "Text", floodEvent.EventsDetail.Select(e => e.LocationId));
            ViewBag.Locations = multiLocations;
            ViewBag.TimeZone = FzCommon.FzCommonUtility.IanaTimeZone;
            return View(floodEvent);
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        // POST: FloodEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FloodEvents model)
        {
            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(model.Id, GetAspNetUserId(), PermissionOptions.FloodEvent))
            {
                return Redirect("~/NotAuthorized");
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(model.RegionId, GetAspNetUserId(), PermissionOptions.Region))
            {
                return Redirect("~/NotAuthorized");
            }

            if (ModelState.IsValid)
            {
                try
               {
                    string locationList = Request.Form["LocationList"];
                    string[] LocationIds = locationList.Split(",");
                    model.LocationIds = locationList;

                    // Detaching any old flood event attached with _context.
                    _context.Entry<FloodEvents>(model).State = EntityState.Detached;

                    _context.FloodEvents.Update(model);
                    _context.SaveChanges();

                    _context.EventsDetail.RemoveRange(_context.EventsDetail.Where(e => e.EventId == model.Id).ToList());
                    _context.SaveChanges();

                    var eventDetais = new List<EventsDetail>();

                    foreach (var item in LocationIds)
                    {
                        eventDetais.Add(new EventsDetail { EventId = model.Id, LocationId = Convert.ToInt32(item) });
                    }
                    _context.EventsDetail.AddRange(eventDetais);
                    _context.SaveChanges();

                    TempData["success"] = "Event successfully updated.";
                    _applicationCache.RemoveCache(CacheOptions.FloodEvents);
                    _applicationCache.RemoveCache(CacheOptions.EventsDetail);
                    return RedirectToAction("Index");
                }
                catch
                {
                    TempData["error"] = "Something went wrong.";
                }
            }

            //$ TODO: region
            var locations = GetLocations(FzCommon.Constants.SvpaRegionId);
            MultiSelectList multiLocations;
            if (model.EventsDetail == null)
            {
                multiLocations = new MultiSelectList(locations, "Value", "Text");
            }
            else
            {
                multiLocations = new MultiSelectList(locations, "Value", "Text", model.EventsDetail.Select(e => e.LocationId));
            }
            ViewBag.Locations = multiLocations;
            ViewBag.TimeZone = FzCommon.FzCommonUtility.IanaTimeZone;
            return View(model);
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {
            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();
                var floodEvents = _context.FloodEvents.Where(e => ids.Contains(e.Id)).ToList();

                List<FloodEvents> listToDeleteEvents = new List<FloodEvents>();
                if (!User.IsInRole("Admin"))
                {
                    foreach (var item in floodEvents)
                    {
                        if (_userPermissions.CheckPermission(item.Id, GetAspNetUserId(), PermissionOptions.FloodEvent))
                        {
                            listToDeleteEvents.Add(item);
                        }
                    }
                }
                else
                {
                    listToDeleteEvents.AddRange(floodEvents);
                }

                if (listToDeleteEvents.Count > 0)
                {
                    listToDeleteEvents.ForEach(e => e.IsDeleted = true);
                    _context.FloodEvents.UpdateRange(listToDeleteEvents);
                    await _context.SaveChangesAsync();
                    _applicationCache.RemoveCache(CacheOptions.FloodEvents, true);
                    if (ids.Count == 1)
                    {
                        TempData["success"] = $"Event successfully deleted!";
                    }
                    else
                    {
                        TempData["success"] = $"{ids.Count} Events successfully deleted!";
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
                    await FloodEventBase.MarkFloodEventsAsUndeleted(sqlcn, undeleteIds);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Flood Event successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Flood Events successfully restored!";
                }
                _applicationCache.RemoveCache(CacheOptions.FloodEvents, true);
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        public async Task<List<SensorLocationBase>> GetEventLocations(int eventId)
        {
            //$ TODO: If we're going to keep the list of locations as part of flood events,
            //$ we should add EventsDetail to FzCommon.

            List<SensorLocationBase> locations;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                sqlcn.Close();
            }
            List<SensorLocationBase> ret = new List<SensorLocationBase>();
            var floodEvents = _applicationCache.GetFloodEvents();
            FloodEvents floodEvent = floodEvents.SingleOrDefault(e => e.Id == eventId);
            foreach (EventsDetail ed in floodEvent.EventsDetail)
            {
                ret.Add(locations.Single(l => l.Id == ed.LocationId));
            }
            return ret;
        }
    }
}