using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Models;
using FloodzillaWeb.ViewModels.LogBook;
using FzCommon;
namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin,Organization Admin")]
    public class LogBookController : FloodzillaController
    {
        protected IMemoryCache _memoryCache;
        public LogBookController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions) : base(context, memoryCache, userPermissions)
        {
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.AvailableTags = await LogBookTagRepository.Repo.GetAvailableTags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LogBookPostViewModel model)
        {
            LogBookEntry lbe = new LogBookEntry()
            {
                UserId = GetFloodzillaUserId(),
                Timestamp = DateTime.UtcNow,
                Text = model.Text,
                IsDeleted = false,
                Tags = model.Tags,
            };
            if (!String.IsNullOrEmpty(model.FixedTag))
            {
                if (lbe.Tags == null)
                {
                    lbe.Tags = new List<string>();
                }
                lbe.Tags.Add(model.FixedTag);
            }
            lbe.Save();

            await SlackClient.SendCreateLogBookEntryNotification(lbe.UserId, GetUserEmail(), lbe.Tags, lbe.Text);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {
            try
            {
                IEnumerable<int> deleteIds = deleteList.Split(',').Select(int.Parse);
                var ids = deleteList.Split(',').Select(int.Parse).ToList();
                if (ids.Count == 1)
                {
                    TempData["success"] = $"Post successfully deleted!";
                }
                else
                {
                    TempData["success"] = $"{ids.Count} posts successfully deleted!";
                }
                await LogBook.MarkEntriesAsDeleted(deleteIds);
                return RedirectToAction("Index");
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
                await LogBook.MarkEntriesAsUndeleted(undeleteIds);
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Post successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} posts successfully restored!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> GetLogBookEntries(bool showDeleted)
        {
            LogBookModel.EnsureTagCache(this._memoryCache);

            List<LogBookModel.LogBookEntry> entries = new List<LogBookModel.LogBookEntry>();
            List<LogBookEntry> rawEntries;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                rawEntries = await LogBook.GetEntriesAsync(sqlcn, null);
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                rawEntries = rawEntries.Where(e => !e.IsDeleted).ToList();
            }
            foreach (LogBookEntry rawEntry in rawEntries)
            {
                LogBookModel.LogBookEntry entry = new LogBookModel.LogBookEntry();
                await entry.InitializeLogBookEntry(_applicationCache, rawEntry);
                entries.Add(entry);
            }

            //$ TODO: Get Timezone from region
            return Ok(JsonConvert.SerializeObject(new { Data = entries, Timezone = FzCommonUtility.IanaTimeZone }));
        }
    }
}
