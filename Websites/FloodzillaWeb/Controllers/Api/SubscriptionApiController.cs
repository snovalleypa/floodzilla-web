using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.Cache;
using FzCommon;

namespace FloodzillaWeb.Controllers.Api
{
    public class UserSubscriptionSettings
    {
        public bool NotifyViaEmail          { get; set; } 
        public bool NotifyViaSms            { get; set; }
        public bool NotifyForecastAlerts    { get; set; }
        public bool NotifyDailyForecasts    { get; set; }
    }

    //$ TODO: Remove dependencies on entity framework/caches so this can move out into its own service

    [Route("api/subscription")]
    public class SubscriptionApiController : FloodzillaController
    {
        public SubscriptionApiController(FloodzillaContext context,
                                         IMemoryCache memoryCache,
                                         UserPermissions userPermissions,
                                         UserManager<ApplicationUser> userManager) :
            base(context, memoryCache, userPermissions, userManager)
        {
        }

        [HttpGet]
        [Route("usersettings")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetUserSubscriptionSettings()
        {
            Users fzUser = GetFloodzillaUser();
            if (fzUser == null)
            {
                return NotFound("User settings not found");
            }
            UserSubscriptionSettings settings = new UserSubscriptionSettings()
            {
                NotifyViaEmail = fzUser.NotifyViaEmail,
                NotifyViaSms = fzUser.NotifyViaSms,
                NotifyDailyForecasts = fzUser.NotifyDailyForecasts,
                NotifyForecastAlerts = fzUser.NotifyForecastAlerts,
            };
            return SuccessResult(settings);
        }

        [HttpPost]
        [Route("usersettings")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetUserSubscriptionSettings([FromBody]UserSubscriptionSettings settings)
        {
            ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
            if (user == null)
            {
                return NotFound("User settings not found");
            }
            Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();
            if (userinfo == null)
            {
                return NotFound("User settings not found");
            }
            userinfo.NotifyViaEmail = settings.NotifyViaEmail;
            userinfo.NotifyViaSms = settings.NotifyViaSms;
            userinfo.NotifyDailyForecasts = settings.NotifyDailyForecasts;
            userinfo.NotifyForecastAlerts = settings.NotifyForecastAlerts;
            await _context.SaveChangesAsync();
            _applicationCache.RemoveCache(CacheOptions.Users);

            return SuccessResult(new { success = true });
        }

        
        public class UnsubscribeEmailModel
        {
            public string userId        { get; set; } 
        }
        [HttpPost]
        [Route("unsubemail")]
        [AllowAnonymous]
        public async Task<IActionResult> UnsubscribeEmail([FromBody]UnsubscribeEmailModel model)
        {
            Users userinfo = (from u in _context.Users where u.AspNetUserId == model.userId select u).FirstOrDefault();
            if (userinfo == null)
            {
                return NotFound("User settings not found");
            }
            userinfo.NotifyViaEmail = false;
            await _context.SaveChangesAsync();
            _applicationCache.RemoveCache(CacheOptions.Users);

            return SuccessResult(new { success = true });
        }
        
        [HttpGet]
        [Route("usersubs/{regionId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetUserGageSubscriptions(int regionId)
        {
            int userId = GetFloodzillaUserId();

            List<string> idList = new List<string>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                List<GageSubscription> subs = await GageSubscription.GetSubscriptionsForUser(sqlcn, userId);
                if (subs.Count > 0)
                {
                    List<SensorLocationBase> locations = SensorLocationBase.GetLocationsForRegion(sqlcn, regionId);
                    foreach (GageSubscription sub in subs)
                    {
                        SensorLocationBase loc = locations.FirstOrDefault(l => l.Id == sub.LocationId);
                        if (loc != null)
                        {
                            idList.Add(loc.PublicLocationId);
                        }
                    }
                }
            }
            return SuccessResult(idList);
        }

        [HttpPut]
        [Route("usersubs/{regionId}/{gageId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SetUserGageSubscription(int regionId, string gageId, [FromBody]bool enabled)
        {
            int userId = GetFloodzillaUserId();
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                    {
                    await sqlcn.OpenAsync();
                    List<SensorLocationBase> locations = SensorLocationBase.GetLocationsForRegion(sqlcn, regionId);
                    SensorLocationBase location = locations.FirstOrDefault(l => l.PublicLocationId == gageId);
                    if (location == null)
                    {
                        return BadRequest("Invalid gage ID.");
                    }
                    if (enabled)
                    {
                        await GageSubscription.AddSubscription(sqlcn, userId, location.Id);
                    }
                    else
                    {
                        await GageSubscription.RemoveSubscription(sqlcn, userId, location.Id);
                    }
                }
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }

            return SuccessResult(new { success = true });
        }


    }
}
