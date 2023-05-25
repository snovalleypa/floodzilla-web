using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using FzCommon;

namespace FloodzillaWeb.Controllers
{

    public class AppleEndpointController : Controller
    {
        [Route(".well-known/apple-app-site-association")]
        public IActionResult AppleAppSiteAssociation()
        {
            return File("/.well-known/apple-app-site-association.json", "application/json");
        }
    }

}

