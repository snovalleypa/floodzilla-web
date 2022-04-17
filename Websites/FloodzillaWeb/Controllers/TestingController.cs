#define INITIALIZE_DATABASE
#define CREATE_A_NEW_ROLE

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;
using FzCommon.Processors;

namespace FloodzillaWeb.Controllers
{

    public class TestReading
    {
        public int LocationId { get; set; }
        public string ExternalDeviceId { get; set; }
        public int MinutesAgo { get; set; }
        public double WaterHeight { get; set; }
    }

    public class TestGage
    {
        public string ExternalDeviceId { get; set; }
        public int LocationId { get; set; }
        public string DeviceName { get; set; }
        public string LocationName { get; set; }
        public double? GroundHeight { get; set; }
        public double? RoadSaddleHeight { get; set; }
        public double? Yellow { get; set; }
        public double? Red { get; set; }
    }

    public class TestingController : FloodzillaController
    {
        public TestingController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions,
                                 UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
                                 IUserValidator<ApplicationUser> userValidator)
            : base(context, memoryCache, userPermissions, userManager, roleManager, userValidator)
        {
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Organization Admin")]
        public async Task<IActionResult> Index()
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                ViewBag.LatestForecast = await NoaaForecastSet.GetLatestForecastSet(sqlcn);
                ViewBag.PreviousForecast = await NoaaForecastSet.GetPreviousForecastSet(sqlcn);
                await sqlcn.CloseAsync();
            }
            return View();
        }

        private async Task<string> EnsureRole(string roleName)
        {
            StringBuilder sbResult = new StringBuilder();
            sbResult.AppendFormat("Checking role {0}...", roleName);
            IdentityRole role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                sbResult.Append("exists");
            }
            else
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    sbResult.Append("created!");
                }
                else
                {
                    sbResult.Append("error: ");
                    foreach (var error in result.Errors)
                    {
                        sbResult.AppendFormat("{0}...", error.Description);
                    }
                }
            }
            return sbResult.ToString();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        public async Task<IActionResult> RoleTests()
        {
            ViewBag.ExtraText = "";

#if CREATE_A_NEW_ROLE
            ViewBag.ExtraText = await EnsureRole(SecurityRoles.GageSteward);
#endif
            ViewBag.Roles = _roleManager.Roles;
            return View();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        public async Task<IActionResult> CreateReading()
        {
            ViewBag.TestGages = await GetTestGages();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organization Admin")]
        public async Task<IActionResult> CreateReading(TestReading testReading)
        {
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    List<SensorLocationBase> locations;
                    await sqlcn.OpenAsync();

                    string listenerInfo = Environment.MachineName + " - Admin TestReading";

                    locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                    SensorLocationBase location = locations.FirstOrDefault(l => l.Id == testReading.LocationId);
                    location.ConvertValuesForDisplay();

                    double sensorHeightFeetASL = location.RelativeSensorHeight.Value;

                    // senix sensors report the distance as negative
                    double distanceFeet = -(sensorHeightFeetASL - testReading.WaterHeight);
                    DateTime timestamp = DateTime.UtcNow;
                    timestamp = timestamp.AddMinutes(-Math.Abs(testReading.MinutesAgo));

                    // The easiest path into all of the common processing for sensor readings
                    // is to build a fake data payload in the format used by the Senix sensors.
                    DeviceBase device;
                    Dictionary<string, object> fakeSenixData = new Dictionary<string, object>();
                    fakeSenixData["eui"] = testReading.ExternalDeviceId;
                    fakeSenixData["LOffset"] = 0;
                    fakeSenixData["Distance"] = distanceFeet;
                    fakeSenixData["time"] = timestamp.ToString();
                    fakeSenixData["TransmitterBat"] = 4.0;
                    fakeSenixData["RSSI"] = -50;
                    fakeSenixData["SNR"] = 15;
                    fakeSenixData["cmd"] = 64;
                    fakeSenixData["LScale"] = 0.0005;
                    fakeSenixData["ecode"] = 0;
                    fakeSenixData["alarm"] = "";
                    fakeSenixData["discard"] = "false";

                    SensorReading sensorReading = new SensorReading();
                    sensorReading.ListenerInfo = listenerInfo;
                    string externalDeviceId = null;
                    SenixReadingResult result = SenixSensorHelper.ProcessReading(sqlcn, sensorReading, fakeSenixData, out externalDeviceId, out device);
                    if (!result.ShouldSave || result.ShouldSaveAsDeleted)
                    {
                        throw new ApplicationException(result.Result);
                    }
                    await sensorReading.Save(sqlcn);

                    TempData["success"] = String.Format("Reading created: distance {0} at {1} UTC", -distanceFeet, timestamp);

                    sqlcn.Close();
                }
            }
            catch (Exception e)
            {
                TempData["error"] = e.ToString();
            }
            ViewBag.TestGages = await GetTestGages();
            return View();
        }

        private async Task<List<TestGage>> GetTestGages()
        {
            List<DeviceBase> devices;
            List<SensorLocationBase> locations;
            List<TestGage> gages = new List<TestGage>();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                devices = await DeviceBase.GetDevicesAsync(sqlcn);
                locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                sqlcn.Close();
            }

            foreach (DeviceBase device in devices)
            {
                if (device.DeviceTypeId != DeviceTypeIds.Virtual)
                {
                    continue;
                }

                if (device.LocationId == 0)
                {
                    continue;
                }

                SensorLocationBase location = locations.FirstOrDefault(l => l.Id == device.LocationId);
                location.ConvertValuesForDisplay();
                TestGage tg = new TestGage()
                {
                    ExternalDeviceId = device.ExternalDeviceId,
                    LocationId = location.Id,
                    DeviceName = device.Name,
                    LocationName = location.LocationName,
                    GroundHeight = location.GroundHeight,
                    RoadSaddleHeight = location.RoadSaddleHeight,
                    Yellow = location.Green,
                    Red = location.Brown,
                };

                gages.Add(tg);
            }

            return gages;
        }

        internal class Forecasts
        {
            internal NoaaForecastSet Previous;
            internal NoaaForecastSet New;
        }
        // NOTE: The specific forecast IDs in here were found by inspection.
        private async Task<Forecasts> GetForecasts(SqlConnection sqlcn, string forecastType)
        {
            Forecasts ret = new Forecasts();
            switch (forecastType)
            {
                case "latest":
                    ret.Previous = await NoaaForecastSet.GetPreviousForecastSet(sqlcn);
                    ret.New = await NoaaForecastSet.GetLatestForecastSet(sqlcn);
                    break;

                case "flooding":
                    ret.Previous = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, 1014);
                    ret.New = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, 1020);
                    break;
                case "clear":
                    ret.Previous = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, 1080);
                    ret.New = await NoaaForecastSet.GetForecastSetForForecastId(sqlcn, 1086);
                    break;
            }
            return ret;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Organization Admin")]

        [Route("Testing/GetForecastJson/{forecastType}")]
        public async Task<IActionResult> GetForecastJson(string forecastType)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                Forecasts f = await GetForecasts(sqlcn, forecastType);
                ForecastEmailModel model = await NoaaForecastProcessor.BuildEmailModel(sqlcn, f.New, f.Previous);
                model.Context = ForecastEmailModel.ForecastContext.Alert;
                model.User = UserBase.GetUser(sqlcn, GetFloodzillaUserId());
                model.AspNetUser = AspNetUserBase.GetAspNetUser(sqlcn, model.User.AspNetUserId);
                ViewBag.ModelBody = model.Serialize();
                await sqlcn.CloseAsync();
            }
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Organization Admin")]
        [Route("Testing/SendForecastEmail/{forecastType}")]
        public async Task<IActionResult> SendForecastEmail(string forecastType)
        {
            try
            {              
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    Forecasts f = await GetForecasts(sqlcn, forecastType);
                    ForecastEmailModel model = await NoaaForecastProcessor.BuildEmailModel(sqlcn, f.New, f.Previous);
                    model.Context = ForecastEmailModel.ForecastContext.Alert;
                    model.User = UserBase.GetUser(sqlcn, GetFloodzillaUserId());
                    model.AspNetUser = AspNetUserBase.GetAspNetUser(sqlcn, model.User.AspNetUserId);

                    await model.SendEmail(FzConfig.Config[FzConfig.Keys.EmailFromAddress], model.AspNetUser.Email);
                    ViewBag.EmailResult = "Sent email to " + model.AspNetUser.Email;

                    await sqlcn.CloseAsync();
                }
            }
            catch (Exception e)
            {
                ViewBag.EmailResult = "Error sending email: " + e.ToString();
            }
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Organization Admin")]
        [Route("Testing/ProcessForecast/{forecastType}")]
        public async Task<IActionResult> ProcessForecast(string forecastType)
        {
            try
            {              
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    Forecasts f = await GetForecasts(sqlcn, forecastType);
                    StringBuilder sbDetails = new StringBuilder();
                    StringBuilder sbResult = new StringBuilder();
                    await NoaaForecastProcessor.ProcessNewForecasts(sqlcn,
                                                                    f.New,
                                                                    f.Previous,
                                                                    sbDetails,
                                                                    sbResult);
                    ViewBag.Details = sbDetails.ToString();
                    ViewBag.Result = sbResult.ToString();
                    await sqlcn.CloseAsync();
                }
            }
            catch (Exception e)
            {
                ViewBag.EmailResult = "Error sending email: " + e.ToString();
            }
            return View();
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> InitializeDatabase()
        {
            List<string> results = new List<string>();
#if INITIALIZE_DATABASE

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                StringBuilder sbResult = new StringBuilder();
                results.Add("Checking security roles...");
                results.Add(await EnsureRole(SecurityRoles.Admin));
                results.Add(await EnsureRole(SecurityRoles.OrgAdmin));
                results.Add(await EnsureRole(SecurityRoles.OrgMember));
                results.Add(await EnsureRole(SecurityRoles.Guest));
                results.Add(await EnsureRole(SecurityRoles.GageSteward));

                results.Add(await EnsureAdmin());
                results.Add(await EnsureOrganization(sqlcn));
                results.Add(await EnsureRegion(sqlcn));

                sqlcn.Close();
            }
#else
            results.Add("Rebuild with INITIALIZE_DATABASE defined in order to initialize the database.");
#endif
            ViewBag.Results = results;
            return View();
        }

        private const string FIRST_ADMIN_USERNAME = "admin@floodzilla.floodzilla";
        private const string FIRST_ADMIN_PASSWORD = "admin@floodzilla.floodzilla";
        private const string FIRST_ADMIN_FIRSTNAME = "Floodzilla";
        private const string FIRST_ADMIN_LASTNAME = "Admin";

        private async Task<string> EnsureAdmin()
        {
            StringBuilder sbResult = new StringBuilder();

            sbResult.AppendFormat("Checking user {0}...", FIRST_ADMIN_USERNAME);
            ApplicationUser user = await _userManager.FindByNameAsync(FIRST_ADMIN_USERNAME);
            if (user != null)
            {
                sbResult.Append("exists");
                return sbResult.ToString();
            }

            sbResult.Append("creating...");
            user = new ApplicationUser() { Email = FIRST_ADMIN_USERNAME, UserName = FIRST_ADMIN_USERNAME, EmailConfirmed = true };
            IdentityResult createUserResult = await _userManager.CreateAsync(user, FIRST_ADMIN_PASSWORD);
            if (!createUserResult.Succeeded)
            {
                sbResult.Append("error: ");
                foreach (var error in createUserResult.Errors)
                {
                    sbResult.AppendFormat("{0}...", error.Code);
                }
                return sbResult.ToString();
            }

            Users uinfo = new Users();
            uinfo.FirstName = FIRST_ADMIN_FIRSTNAME;
            uinfo.LastName = FIRST_ADMIN_LASTNAME;
            uinfo.AspNetUserId = user.Id;

            _context.Users.Add(uinfo);
            int res = await _context.SaveChangesAsync();
            if (res <= 0)
            {
                sbResult.Append("error: can't create userinfo");
                return sbResult.ToString();
            }

            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, SecurityRoles.Admin);
            if (!roleResult.Succeeded)
            {
                sbResult.Append("error setting role: ");
                foreach (var error in roleResult.Errors)
                {
                    sbResult.AppendFormat("{0}...", error.Code);
                }
                return sbResult.ToString();
            }

            sbResult.Append("Success!  DO NOT FORGET TO CHANGE PASSWORD");
            return sbResult.ToString();
        }

        private const string FIRST_ORGANIZATION_NAME = "Floodzilla Organization";

        private async Task<string> EnsureOrganization(SqlConnection sqlcn)
        {
            StringBuilder sbResult = new StringBuilder();
            sbResult.Append("Checking organization...");

            List<OrganizationBase> orgs = await OrganizationBase.GetOrganizations(sqlcn);
            if (orgs != null && orgs.Count > 0)
            {
                sbResult.Append("exists");
                return sbResult.ToString();
            }
            sbResult.Append("creating...");
            Organizations org = new Organizations()
            {
                Name = FIRST_ORGANIZATION_NAME,
                IsActive = true,
            };
            _context.Organizations.Add(org);
            int res = await _context.SaveChangesAsync();
            if (res <= 0)
            {
                sbResult.Append("error: couldn't create organization");
                return sbResult.ToString();
            }

            sbResult.Append("Success!");
            return sbResult.ToString();
        }

        private const string FIRST_REGION_NAME = "Floodzilla Region";

        private async Task<string> EnsureRegion(SqlConnection sqlcn)
        {
            StringBuilder sbResult = new StringBuilder();
            sbResult.Append("Checking region...");

            List<RegionBase> regions = await RegionBase.GetAllRegions(sqlcn);
            if (regions != null && regions.Count > 0)
            {
                sbResult.Append("exists");
                return sbResult.ToString();
            }
            sbResult.Append("creating...");
            Regions firstRegion = new Regions()
            {
                RegionName = FIRST_REGION_NAME,
                IsActive = true,
                Organizations = _context.Organizations.FirstOrDefault(),
            };
            _context.Regions.Add(firstRegion);
            int res = await _context.SaveChangesAsync();
            if (res <= 0)
            {
                sbResult.Append("error: couldn't create region");
                return sbResult.ToString();
            }

            sbResult.Append("Success!");
            return sbResult.ToString();
        }
    }
}
