using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

using FzCommon;

namespace FloodzillaWeb.Controllers.Api
{
    //$ need fully-realized admin API models for everything
    public class AdminLocationInfo
    {
        public string Id;
        public string LocationName;
        public double? Latitude;
        public double? Longitude;
        public bool IsOffline;
        public double? Rank;
        public double? YMin;
        public double? YMax;
        public double? GroundHeight;
        public double? RoadSaddleHeight;
        public string RoadDisplayName;
        public string DeviceTypeName;
        public string TimeZoneName;

        public List<string> LocationImages = null;
        
        public AdminLocationInfo(SensorLocationBase location, string deviceTypeName)
        {
            this.Id = location.PublicLocationId;
            this.LocationName = location.LocationName;
            this.Latitude = location.Latitude;
            this.Longitude = location.Longitude;
            this.IsOffline = location.IsOffline;
            this.Rank = location.Rank;
            this.YMin = location.YMin;
            this.YMax = location.YMax;
            this.GroundHeight = location.GroundHeight;
            this.RoadSaddleHeight = location.RoadSaddleHeight;
            this.RoadDisplayName = location.RoadDisplayName;
            this.DeviceTypeName = deviceTypeName;

            //$ TODO: This should eventually come from the region...
            this.TimeZoneName = FzCommonUtility.IanaTimeZone;
        }
    }

    [Route("api/admin")]
    public class AdminApiController : Controller
    {

        //$ TODO: Other params?
        public AdminApiController()
        {
        }

        [HttpGet]
        [Route("locations")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> GetLocations(int regionId, bool showDeleted = false)
        {
            List<SensorLocationBase> locations;
            List<DeviceBase> devices;
            List<DeviceType> deviceTypes;
            List<FloodzillaWeb.Models.FzModels.Uploads> uploads;
            List<AdminLocationInfo> ret = new List<AdminLocationInfo>();

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, regionId);
                devices = await DeviceBase.GetDevicesAsync(sqlcn);
                deviceTypes = DeviceType.GetDeviceTypes(sqlcn);
                uploads = FloodzillaWeb.Models.FzModels.Uploads.GetUploads(sqlcn);

                if (!showDeleted)
                {
                    locations = locations.Where(l => !l.IsDeleted).ToList();
                }

                //$ TODO: How should API sort items by default?
                foreach (SensorLocationBase location in locations)
                {
                    location.ConvertValuesForDisplay();

                    DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                    string deviceTypeName = "[none]";
                    if (device != null)
                    {
                        DeviceType deviceType = deviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == device.DeviceTypeId);
                        deviceTypeName = deviceType.DeviceTypeName;
                    }

                    AdminLocationInfo ali = new AdminLocationInfo(location, deviceTypeName);
                    
                    ret.Add(ali);
                }

                sqlcn.Close();
            }
            
            // NOTE: Don't use JsonResult() here -- it doesn't pick up the default serializer settings...
            return Ok(JsonConvert.SerializeObject(ret));
        }

        //$ let this take public id instead of int id?  it kind of sucks having a non-stable id as a key...

        [HttpGet]
        [Route("locations/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> GetLocation(string id)
        {
            AdminLocationInfo ali = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();

                //$ TODO: Async versions of all this
                SensorLocationBase location;
                int intId = 0;
                if (Int32.TryParse(id, out intId))
                {
                     location = SensorLocationBase.GetLocation(sqlcn, intId);
                }
                else
                {
                    location = SensorLocationBase.GetLocationByPublicId(sqlcn, id);
                }

                if (location == null)
                {
                    return BadRequest("Invalid id");
                }
                List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
                List<DeviceType> deviceTypes = DeviceType.GetDeviceTypes(sqlcn);

                location.ConvertValuesForDisplay();
                DeviceBase device = devices.FirstOrDefault(d => d.LocationId == location.Id);
                string deviceTypeName = "[none]";
                if (device != null)
                {
                    DeviceType deviceType = deviceTypes.FirstOrDefault(dt => dt.DeviceTypeId == device.DeviceTypeId);
                    deviceTypeName = deviceType.DeviceTypeName;
                }

                ali = new AdminLocationInfo(location, deviceTypeName);
                sqlcn.Close();

                // NOTE: Don't use JsonResult() here -- it doesn't pick up the default serializer settings...
                return Ok(JsonConvert.SerializeObject(ali));
            }
        }
    }
}
