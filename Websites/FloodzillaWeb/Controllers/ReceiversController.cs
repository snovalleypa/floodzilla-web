using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin,Organization Admin")]
    public class ReceiversController : Controller
    {

        private FloodzillaContext _context;
        private ApplicationCache _applicationCache;
        private UserPermissions _userPermissions;
        public ReceiversController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
        {
            _context = context;
            _applicationCache = new ApplicationCache(_context, memoryCache);
            _userPermissions = userPermissions;
        }

        [NonAction]
        private int GetFloodzillaUserId()
        {
            return SecurityHelper.GetFloodzillaUserId(User, _applicationCache);
        }

        [NonAction]
        private string GetUserEmail()
        {
            return SecurityHelper.GetFloodzillaUserEmail(User, _applicationCache);
        }

        public IActionResult Index()
        {
            List<ReceiverBase> receivers = null;
            ViewBag.AttachedSensors = new Dictionary<int, string[]>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                receivers = ReceiverBase.GetReceivers(sqlcn);
                foreach (ReceiverBase receiver in receivers)
                {
                    ViewBag.AttachedSensors[receiver.ReceiverId] = GetAttachedLocations(sqlcn, receiver);
                }
                sqlcn.Close();
            }

            return View(receivers);
        }

        public IActionResult Edit(int id)
        {
            ReceiverBase receiver = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                receiver = ReceiverBase.GetReceiver(sqlcn, id);
                ViewBag.AttachedLocations = GetAttachedLocations(sqlcn, receiver);
                sqlcn.Close();
            }

            if (receiver == null)
            {
                //$ TODO: error message?
                return RedirectToAction("Index");
            }
            return View(receiver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ReceiverBase receiver)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                receiver.Save(sqlcn).Wait();
                sqlcn.Close();

                LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), receiver, Request.Form["ChangeReason"]);
            }
            //$ TODO: what else here besides saving
            return RedirectToAction("Index");
        }

        //$ TODO: region
        protected string[] GetAttachedLocations(SqlConnection sqlcn, ReceiverBase receiver)
        {
            RegionBase region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
            var devices = DeviceBase.GetDevices(sqlcn).Where(d => d.LatestReceiverId == receiver.ExternalReceiverId);
            var locations = _applicationCache.GetLocations().Where(l => devices.Select(d =>d.LocationId).Contains(l.Id));
            List<string> ret = new List<string>();
            foreach (var location in locations)
            {
                var device = devices.Where(d => d.LocationId == location.Id).FirstOrDefault();
                string readingTime = device.LastReadingReceived.HasValue ? region.ToRegionTimeFromUtc(device.LastReadingReceived.Value).ToString("g") : "n/a";
                ret.Add(String.Format("{0} ({1})", location.LocationName, readingTime));
            }
            return ret.ToArray();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteReceiverIds)
        {
            try
            {
                IEnumerable<int> ids = deleteReceiverIds.Split(',').Select(int.Parse);
                await ReceiverBase.MarkReceiversAsDeleted(ids);
                if (ids.Count() == 1)
                {
                    TempData["success"] = $"Receiver successfully deleted!";
                }
                else
                {
                    TempData["success"] = $"{ids.Count()} Receivers successfully deleted!";
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
