using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin,Organization Admin,Organization Member,Gage Steward")]
    public class AdminController : Controller
    {
        public AdminController()
        {
        }

        public IActionResult Index(int regionId = 1)
        {
            ViewBag.Title = "Admin Dashboard";
            return View();
        }

    }
}
