using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;

namespace FloodzillaWeb
{
    public class SecurityHelper
    {
        public static string GetAspNetUserId(ClaimsPrincipal requestUser)
        {
            Claim c = requestUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return (c == null ? null : c.Value);
        }

        public static async Task<ApplicationUser> GetApplicationUser(ClaimsPrincipal requestUser, UserManager<ApplicationUser> userManager)
        {
            string id = GetAspNetUserId(requestUser);
            if (!String.IsNullOrEmpty(id))
            {
                return await userManager.FindByIdAsync(id);
            }
            return null;
        }

        public static Users GetFloodzillaUser(ClaimsPrincipal requestUser, ApplicationCache applicationCache)
        {
            return applicationCache.GetUsers().SingleOrDefault(u => u.AspNetUserId == GetAspNetUserId(requestUser));
        }

        public static int GetFloodzillaUserId(ClaimsPrincipal requestUser, ApplicationCache applicationCache)
        {
            Users user = GetFloodzillaUser(requestUser, applicationCache);
            return (user == null) ? 0 : user.Id;
        }

        public static string GetFloodzillaUserEmail(ClaimsPrincipal requestUser, ApplicationCache applicationCache)
        {
            Users user = GetFloodzillaUser(requestUser, applicationCache);
            return user.AspNetUser.Email;
        }
    }
}
