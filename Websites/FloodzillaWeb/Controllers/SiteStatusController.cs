using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

using FzCommon;

namespace FloodzillaWeb.Controllers
{
    public class SiteStatusController : Controller
    {
        public SiteStatusController()
        {
        }

        public async Task<IActionResult> Index()
        {
            // Our site monitor will check for "OK"...
            string siteStatus = "OK";

            // Check our SQL database connection.  The table we use doesn't really matter...
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Regions", sqlcn))
                    {
                        await sqlcn.OpenAsync();
                        using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            if (await dr.ReadAsync())
                            {
                                // all ok
                            }
                            else
                            {
                                throw new ApplicationException("Cannot read data");
                            }
                        }
                        sqlcn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                siteStatus = "Database error: " + ex.Message;
            }

            return View("Index", "Site Status: " + siteStatus);
        }
    }
}