using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

using FzCommon;

namespace FloodzillaWeb.Controllers
{

    public class DynamicContentController : Controller
    {
        [Route("dynamic/clientkeys.js")]
        public IActionResult ClientKeys()
        {
            Response.ContentType = "text/javascript";
            return View();
        }

        [Route("dynamic/regionsettings.js")]
        public IActionResult RegionSettings()
        {
            //$ TODO: Make site configuration specify which region this instance is serving.
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                RegionBase region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
                ViewBag.Region = region;
            }
            Response.ContentType = "text/javascript";
            return View();
        }
    }

}

