using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;

namespace FloodzillaWeb.Controllers
{
    public class FloodzillaController : Controller
    {
        protected FloodzillaContext _context;
        protected ApplicationCache _applicationCache;
        protected UserPermissions _userPermissions;
        protected readonly IWebHostEnvironment _env;
        protected readonly RoleManager<IdentityRole> _roleManager;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly IUserValidator<ApplicationUser> _userValidator;
        public FloodzillaController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
        {
            _context = context;
            _userPermissions = userPermissions;

            _applicationCache = new ApplicationCache(_context, memoryCache);
        }

        public FloodzillaController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions, UserManager<ApplicationUser> userManager)
                : this(context, memoryCache, userPermissions)
        {
             _userManager = userManager;
        }
        public FloodzillaController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions, IWebHostEnvironment env)
                : this(context, memoryCache, userPermissions)
        {
            _env = env;
        }

        public FloodzillaController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions,
                                    UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
                                    IUserValidator<ApplicationUser> userValidator)
        {
            _context = context;
            _userPermissions = userPermissions;
            _userManager = userManager;
            _roleManager = roleManager;
            _userValidator = userValidator;

            _applicationCache = new ApplicationCache(roleManager, context, memoryCache);
        }

        [NonAction]
        protected string GetAspNetUserId()
        {
            return SecurityHelper.GetAspNetUserId(User);
        }

        [NonAction]
        protected Users GetFloodzillaUser()
        {
            return SecurityHelper.GetFloodzillaUser(User, _applicationCache);
        }

        [NonAction]
        protected int GetFloodzillaUserId()
        {
            return SecurityHelper.GetFloodzillaUserId(User, _applicationCache);
        }

        [NonAction]
        protected string GetUserEmail()
        {
            return SecurityHelper.GetFloodzillaUserEmail(User, _applicationCache);
        }

        protected static IActionResult SuccessResult(object o)
        {
            // NOTE: Don't use JsonResult() here -- it will ignore default serializer settings.
            // Also note: ContentResult treats its parameter as a literal, so it doesn't do extra
            // quoting the way OkObjectResult does.
            return new ContentResult() { Content = JsonConvert.SerializeObject(o), ContentType = "application/json" };
        }
    }
}
