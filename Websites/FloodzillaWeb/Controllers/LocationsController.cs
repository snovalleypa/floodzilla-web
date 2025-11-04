using Azure.Storage.Blobs;
using ExifLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Data;
using System.Drawing;

using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.Cache;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    /// <summary>
    /// Classes for uploading images async to azure.
    /// </summary>
    class Files
    {
        public Files()
        {
            Paths = new List<Controllers.Paths>();
        }
        public int LocationId { get; set; }
        public string DirectoryPath { get; set; }
        public List<Paths> Paths { get; set; }
    }

    class Paths
    {
        public string FileName { get; set; }
        public string TempPath { get; set; }
    }
    
    [Authorize(Roles = "Admin,Organization Admin,Organization Member")]
    public class LocationsController : FloodzillaController
    {
        public LocationsController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions, IWebHostEnvironment env)
                : base(context, memoryCache, userPermissions, env)
        {
        }

        [NonAction]
        private List<SelectListItem> GetDeviceTypes()
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var devicetypes = _applicationCache.GetDeviceTypes();

            var selectListItems = new List<SelectListItem>();
            foreach (var item in devicetypes)
            {
                if (item.DeviceTypeId == 1)
                {
                    item.DeviceTypeName = "SVPA";
                }

                selectListItems.Add(new SelectListItem { Text = item.DeviceTypeName, Value = item.DeviceTypeId.ToString() });
            }

            selectListItems.Insert(0, new SelectListItem { Text = "-- Select Device Type --", Value = "" });
            return selectListItems;
        }

        [NonAction]
        private List<SelectListItem> GetRegions()
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var regions = _applicationCache.GetRegions();
            
            if (!User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                regions = regions.Where(e => e.OrganizationsId==user.OrganizationsId).ToList();
            }

            List<SelectListItem> selectListItems = new List<SelectListItem>();
            foreach (var item in regions)
            {
                selectListItems.Add(new SelectListItem() { Text = item.RegionName, Value = item.RegionId.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "-- Select Region --", Value = "" });
            return selectListItems;
        }

        [NonAction]
        private List<SelectListItem> GetTimeZones()
        {
            List<SelectListItem> selectListItems = new List<SelectListItem>();
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (var item in timeZones)
            {
                selectListItems.Add(new SelectListItem() { Text = item.DisplayName, Value = item.Id });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "-- Select Time Zone --", Value = "" });
            return selectListItems;
        }

        private Devices GetCurrentDevice(int locationId)
        {
            var devices = _applicationCache.GetDevices();
            return devices.Where(e => e.LocationId == locationId).FirstOrDefault();
        }

        // Need authorization guidance on this function.
        public List<SelectListItem> GetDevices(int locationId=0)
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var devices = _applicationCache.GetDevices();
           
            // Getting only those devices which is not assign to any location.
            var unAllocatedDevices = devices.Where(e => e.LocationId == null).ToList();

            List<SelectListItem> selectListItems = new List<SelectListItem>();

            foreach (var item in unAllocatedDevices)
            {
                selectListItems.Add(new SelectListItem() { Text = item.Name ?? item.DeviceId.ToString(), Value = item.DeviceId.ToString() });
            }

            // If request is coming from Edit form, then we need to show the selected devices with location.
            if (locationId != 0)
            {
                var allocatedDevices = devices.Where(e => e.LocationId == locationId).ToList();
                foreach (var item in allocatedDevices)
                {
                    selectListItems.Add(new SelectListItem() { Text = item.Name ?? item.DeviceId.ToString(), Value = item.DeviceId.ToString(), Selected = true });
                }
            }
            selectListItems.Insert(0, new SelectListItem() { Text="-- Select Device --", Value="0" });
            return selectListItems;
        }

        public class DeviceReadingStatus
        {
            public double BatteryPercent;
            public double LastReading;
            public DateTime LastReadingTime;
        }

        public async Task<DeviceReadingStatus> GetDeviceReadingStatus(int deviceId, double tzOffset)
        {
            List<SensorReading> readings = await SensorReading.GetReadingsForDevice(deviceId, 1, null, null);
            if (readings == null || readings.Count == 0)
            {
                return null;
            }
            SensorReading sr = readings[0];
            return new DeviceReadingStatus()
            {
                BatteryPercent = FzCommonUtility.CalculateBatteryVoltPercentage(sr.BatteryPercent, sr.BatteryVolt) ?? 0,
                LastReading = (sr.WaterHeight ?? 0) / 12.0,
                LastReadingTime = sr.Timestamp.AddMinutes(tzOffset),
            };
        }

        // Uploading Image
        [NonAction]
        private void UploadImage(List<IFormFile> images, int locationId)
        {
            try
            {
                var temp = Path.Combine(_env.WebRootPath, $"tempImages/{locationId}");
                Directory.CreateDirectory(temp);

                var uploads = new List<Uploads>();
                var listOfFiles = new Files();
                listOfFiles.DirectoryPath = temp;

                listOfFiles.LocationId = locationId;

                foreach (var image in images)
                {
                    if (CheckImageFile(image))
                    {
                        var extension = Path.GetExtension(image.FileName);
                        var imageKey = Guid.NewGuid().ToString() + extension;

                        using (var stream = new FileStream(Path.Combine(temp, imageKey), FileMode.Create))
                        {
                            FlipOrientation(image, stream);
                        }
                        // Getting Image detail
                        var upload = GetImageDetail(temp, imageKey);
                        upload.LocationId = locationId;
                        upload.Image = locationId+"/"+ imageKey;

                        if (User.IsInRole("Admin"))
                        {
                            upload.IsVarified = true;
                        }

                        upload.IsActive = true;

                        uploads.Add(upload);
                        listOfFiles.Paths.Add(new Paths() { FileName= imageKey, TempPath=temp });
                    }
                }
                if (uploads.Count > 0)
                {
                    _context.Uploads.AddRange(uploads);
                    _context.SaveChanges();
                    _applicationCache.RemoveCache(CacheOptions.Uploads);
                    UploadToAzure(listOfFiles);
                }
            }
            catch (Exception)
            {
            }
        }

        //$ TODO: Consolidate with UploadsController.cs
        [NonAction]
        private void CreateResizedImage(Image sourceimage, FileStream dest, int maxWidth, int maxHeight, string prefix)
        {
            int imgWidth, imgHeight;
            if (sourceimage.Width < sourceimage.Height)
            {
                //portrait image  
                imgHeight = maxHeight;
                var imgRatio = (float)imgHeight / (float)sourceimage.Height;
                imgWidth = Convert.ToInt32(sourceimage.Width * imgRatio);
            }
            else
            {
                //landscape image  
                imgWidth = maxWidth;
                var imgRatio = (float)imgWidth / (float)sourceimage.Width;
                imgHeight = Convert.ToInt32(sourceimage.Height * imgRatio);
            }
            Bitmap thumbBitmap = new Bitmap(sourceimage);
            Image thumbnail = thumbBitmap.GetThumbnailImage(
            imgWidth, imgHeight, () => false, IntPtr.Zero);

            var path = dest.Name.Substring(0, dest.Name.LastIndexOf('\\')) + "\\" + prefix;

            var imageName = dest.Name.Substring(dest.Name.LastIndexOf('\\') + 1);

            Directory.CreateDirectory(path);
            using (var thumbStream = new FileStream(Path.Combine(path, imageName), FileMode.Create))
            {
                thumbnail.Save(thumbStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        //$ TODO: Consolidate with UploadsController.cs
        private void UploadResizedImage(BlobContainerClient bcc, Files files, Paths path, string prefix)
        {
            BlobClient blobClient = bcc.GetBlobClient(prefix + "/" + files.LocationId + "/" + path.FileName);
            using (var stream = System.IO.File.OpenRead(path.TempPath + "\\" + prefix + "\\" + path.FileName))
            {
                blobClient.Upload(stream);
                stream.Close();
            }
        }

        //$ TODO: Consolidate with UploadsController.cs
        private BlobContainerClient CreateBlobContainerClient()
        {
            BlobServiceClient bsc = new BlobServiceClient(FzConfig.Config[FzConfig.Keys.AzureStorageConnectionString]);
            BlobContainerClient bcc = bsc.GetBlobContainerClient(FzConfig.Config[FzConfig.Keys.UploadsBlobContainer]);
            bcc.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            return bcc;
        }

        [NonAction]
        private async void UploadToAzure(Files files)
        {
            BlobContainerClient bcc = this.CreateBlobContainerClient();

            foreach (var path in files.Paths)
            {
                try
                {
                    BlobClient blobClient = bcc.GetBlobClient(files.LocationId + "/" + path.FileName);
                    using (var stream = System.IO.File.OpenRead(path.TempPath + "\\" + path.FileName))
                    {
                        await blobClient.UploadAsync(stream);
                        stream.Close();
                    }
                    UploadResizedImage(bcc, files, path, "thumbnails");
                    UploadResizedImage(bcc, files, path, "medium");
                    UploadResizedImage(bcc, files, path, "large");
                }
                catch (Exception)
                {
                    
                }
            }
            Directory.Delete(files.DirectoryPath, true);
        }

        [NonAction]
        private void FlipOrientation(IFormFile source, FileStream dest)
        {
            const int orientation_property_id = 0x112;
            RotateFlipType rotateAngle = RotateFlipType.Rotate180FlipNone;
            using (var fstream = source.OpenReadStream())
            {

                using (var image = Image.FromStream(fstream))
                {
                    var pi = image.PropertyItems.Where(x => x.Id == orientation_property_id).FirstOrDefault();
                    if (pi != null && pi.Value.Count() > 0)
                    {
                        rotateAngle = OrientationToFlipType(pi.Value[0]);
                        image.RotateFlip(rotateAngle);
                    }

                    image.Save((Stream)dest, System.Drawing.Imaging.ImageFormat.Jpeg);
                    CreateResizedImage(image, dest, 100, 100, "thumbnails");
                    CreateResizedImage(image, dest, 400, 300, "medium");
                    CreateResizedImage(image, dest, 1024, 768, "large");
                }
            }
        }

        [NonAction]
        private static RotateFlipType OrientationToFlipType(int orientation)
        {
            switch (orientation)
            {
                case 1:
                    return RotateFlipType.RotateNoneFlipNone;
                case 2:
                    return RotateFlipType.RotateNoneFlipX;
                case 3:
                    return RotateFlipType.Rotate180FlipNone;
                case 4:
                    return RotateFlipType.Rotate180FlipX;
                case 5:
                    return RotateFlipType.Rotate90FlipX;
                case 6:
                    return RotateFlipType.Rotate90FlipNone;
                case 7:
                    return RotateFlipType.Rotate270FlipX;
                case 8:
                    return RotateFlipType.Rotate270FlipNone;
                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }

        [NonAction]
        private bool CheckImageFile(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName);
            if ((extension == ".jpg") || (extension == ".jpeg") || (extension == ".png"))
                return true;
            return false;
        }

        [NonAction]
        private Uploads GetImageDetail(string path, string fileName)
        {
            var upload = new Uploads();

            double[] GpsLongArray;
            double[] GpsLatArray;
            string GpsLatRef = null;
            string GpsLongRef = null;
            double Altitude = 0;

            DateTime PicDate = new DateTime();
            try
            {
                using (ExifReader reader = new ExifReader(Path.Combine(path, fileName)))
                {
                    if (reader.GetTagValue<Double[]>(ExifTags.GPSLongitude, out GpsLongArray)
                        && reader.GetTagValue<Double[]>(ExifTags.GPSLatitude, out GpsLatArray))
                    {
                        reader.GetTagValue<string>(ExifTags.GPSLatitudeRef, out GpsLatRef);
                        reader.GetTagValue<string>(ExifTags.GPSLongitudeRef, out GpsLongRef);

                        // if longitude ref is east than long will be positive, else negative 
                        upload.Longitude = GpsLongRef.ToLower() == "e" ? (GpsLongArray[0] + GpsLongArray[1] / 60 + GpsLongArray[2] / 3600) : (-(GpsLongArray[0] + GpsLongArray[1] / 60 + GpsLongArray[2] / 3600));

                        // if latitude ref is north than lat will be positive, else negative 
                        upload.Latitude = GpsLatRef.ToLower() == "n" ? GpsLatArray[0] + GpsLatArray[1] / 60 + GpsLatArray[2] / 3600 : (-(GpsLatArray[0] + GpsLatArray[1] / 60 + GpsLatArray[2] / 3600));
                    }
                    if (reader.GetTagValue<DateTime>(ExifTags.DateTime, out PicDate))
                    {
                        upload.DateOfPicture = PicDate;
                    }
                    if (reader.GetTagValue<double>(ExifTags.GPSAltitude, out Altitude))
                    {
                        upload.Altitude = Altitude;
                    }


                    var props = Enum.GetValues(typeof(ExifTags)).Cast<ushort>().Select(tagID =>
                    {
                        object val;
                        if (reader.GetTagValue(tagID, out val))
                        {
                            // Special case - some doubles are encoded as TIFF rationals. These
                            // items can be retrieved as 2 element arrays of {numerator, denominator}
                            if (val is double)
                            {
                                int[] rational;
                                if (reader.GetTagValue(tagID, out rational))
                                    val = string.Format("{0} ({1}/{2})", val, rational[0], rational[1]);
                            }

                            return string.Format("{0}: {1}", Enum.GetName(typeof(ExifTags), tagID), RenderTag(val));
                        }

                        return null;

                    }).Where(x => x != null).ToArray();
                    upload.ResponseString = string.Join("<br />", props);
                    
                    reader.Dispose();
                }
            }
            catch (Exception)
            {

            }
            return upload;
        }

        [NonAction]
        private string RenderTag(object tagValue)
        {
            // Arrays don't render well without assistance.
            var array = tagValue as Array;
            if (array != null)
            {
                // Hex rendering for really big byte arrays (ugly otherwise)
                if (array.Length > 20 && array.GetType().GetElementType() == typeof(byte))
                    return "0x" + string.Join("", array.Cast<byte>().Select(x => x.ToString("X2")).ToArray());

                return string.Join(", ", array.Cast<object>().Select(x => x.ToString()).ToArray());
            }

            return tagValue.ToString();
        }

        [NonAction]
        private void SetDropdownList()
        {
            ViewBag.TimeZones = GetTimeZones();
            ViewBag.Regions = GetRegions();
            ViewBag.Devices = GetDevices();
            ViewBag.ElevationTypes = GetElevationTypes();
            ViewBag.DeviceTypes = GetDeviceTypes();
        }

        // will update geodata column in locations tables, with geography data, base on provided lat,lng.
        // Note that, entity framework core does not support yet Geography data type columns. This function will update data manually.
        [NonAction]
        private void UpdateGeoGraphyData(int locationId, double latitude, double longitude)
        {
            if (latitude != 0 && longitude != 0)
            {
                string query = $"update locations set GeoData=(geography::Point({latitude},{longitude},4326)) where id={locationId}";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    try
                    {
                        _context.Database.OpenConnection();

                        command.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        _context.Database.CloseConnection();
                    }
                }
            }

        }

        // Getting ElevationTypes
        private List<SelectListItem> GetElevationTypes(bool isEditForm=false)
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var elevationTypes = _applicationCache.GetElevationTypes();

            List<SelectListItem> selectListItems = new List<SelectListItem>();
            if(!isEditForm)
            {
                foreach (var item in elevationTypes)
                {
                    selectListItems.Add(new SelectListItem() { Text = item.ElevationTypeName, Value = item.ElevationTypeName });
                }
            }
            else
            {
                foreach (var item in elevationTypes)
                {
                    selectListItems.Add(new SelectListItem() { Text = item.ElevationTypeName, Value = item.ElevationTypeId.ToString() });
                }
            }
            
            selectListItems.Insert(0, new SelectListItem() { Text = "-- Select Elevation Type --", Value = "" });
            return selectListItems;
        }

        // GET: Locations
        public IActionResult Index()
        {
            ViewBag.AzureImageUploadBaseUrl = FzCommon.StorageConfiguration.AzureImageUploadBaseUrl;
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var locations = _applicationCache.GetLocations();
            if(!User.IsInRole("Admin"))
            {
                var claims = User.Claims;
                var user = _context.Users.AsNoTracking().SingleOrDefault(e => e.AspNetUserId == GetAspNetUserId());

                var regions = _applicationCache.GetRegions().Where(e => e.OrganizationsId == user.OrganizationsId).Select(e=>e.RegionId).ToList();
                
                locations = locations.Where(e => regions.Contains(e.RegionId)).ToList();
            }

            List<Locations> returnedLocations = new List<Locations>();
            locations.ForEach(l => returnedLocations.Add(new Locations(l)));
            returnedLocations.ForEach(l => l.ConvertValuesForDisplay());
            return View(returnedLocations);
        }

        // GET: Locations/Create
        [Authorize(Roles = "Admin,Organization Admin")]
        public IActionResult Create()
        {
            SetDropdownList();
            ViewBag.DefaultThreshold = FzCommon.Constants.DefaultMaxValidChangeThreshold;
            return View();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Locations locations, int DeviceId, List<IFormFile> images)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (CheckRankExist(locations.RegionId, locations.Id, locations.Rank))
                    {
                        TempData["error"] = "Rank is already exist.";
                        return RedirectToAction("Create");
                    }

                    locations.ConvertValuesForStorage();

                    _context.Add(locations);
                    await _context.SaveChangesAsync();

                    UpdateGeoGraphyData(locations.Id, locations.Latitude ?? 0, locations.Longitude ?? 0);

                    // Checking if user select some device.
                    if (DeviceId > 0)
                    {
                        var newDevice = _context.Devices.SingleOrDefault(e => e.DeviceId == DeviceId);
                        newDevice.LocationId = locations.Id;
                        _context.Devices.Update(newDevice);
                        await _context.SaveChangesAsync();
                        _applicationCache.RemoveCache(CacheOptions.Devices);
                    }

                    TempData["success"] = "Location successfully added.";
                    _applicationCache.RemoveCache(CacheOptions.Locations);

                    // Checking if user upload images.
                    if (images.Count > 0)
                    {
                        UploadImage(images, locations.Id);
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong. Please contact with service provider.";
                }
            }
            SetDropdownList();
            return View(locations);
        }

        // GET: Locations/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var locations = await _context.Locations.SingleOrDefaultAsync(m => m.Id == id);
            if (locations == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(locations.Id, GetAspNetUserId(), PermissionOptions.Location))
            {
                return Redirect("~/NotAuthorized");
            }

            SetDropdownList();
            ViewBag.Devices = GetDevices(id);
            ViewBag.ElevationTypes = GetElevationTypes(true);
            ViewBag.CurrentDevice = GetCurrentDevice(id);
            ViewBag.DefaultThreshold = FzCommon.Constants.DefaultMaxValidChangeThreshold;

            locations.ConvertValuesForEditing();
            
            if (locations.SeaLevel != null)
            {
                locations.DeviceSeaLevelElevation = FzCommonUtility.GetRoundValue((locations.SeaLevel ?? 0) + locations.GroundHeight);
            }

            return View(locations);
        }

        // POST: Locations/Edit/5
        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Locations locations, int DeviceId, List<IFormFile> images, string? isLocationChanged)
        {
            if (id != locations.Id)
            {
                return NotFound();
            }

            // Checking for location restriction for user.
            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(locations.Id, GetAspNetUserId(), PermissionOptions.Location))
            {
                return Redirect("~/NotAuthorized");
            }

            // Checking for region restriction for user.
            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(locations.RegionId, GetAspNetUserId(), PermissionOptions.Region))
            {
                return Redirect("~/NotAuthorized");
            }

            if (CheckRankExist(locations.RegionId, locations.Id, locations.Rank))
            {
                TempData["error"] = "Rank is already exist.";
                return RedirectToAction("Edit");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    locations.ConvertValuesForStorage();

                    // Detaching any old location attached with _context.
                    _context.Entry<Locations>(locations).State = EntityState.Detached;

                    _context.Locations.Update(locations);
                    await _context.SaveChangesAsync();

                    UpdateGeoGraphyData(locations.Id, locations.Latitude ?? 0, locations.Longitude ?? 0);

                    // Checking if user select some device.
                    if (DeviceId == 0)
                    {
                        var previousDevice = _context.Devices.AsNoTracking().SingleOrDefault(e => e.LocationId == locations.Id);
                        if (previousDevice != null && previousDevice.LocationId!=null)
                        {
                            previousDevice.LocationId = null;
                            _context.Devices.Update(previousDevice);
                            _applicationCache.RemoveCache(CacheOptions.Devices);
                        }
                    }
                    if (DeviceId > 0)
                    {
                        var previousDevice = _context.Devices.AsNoTracking().SingleOrDefault(e => e.LocationId == locations.Id);
                        var newDevice = _context.Devices.AsNoTracking().SingleOrDefault(e => e.DeviceId == DeviceId);

                        if (previousDevice != null && previousDevice.LocationId != null && previousDevice.DeviceId != newDevice.DeviceId)
                        {
                            previousDevice.LocationId = null;
                            _context.Devices.Update(previousDevice);

                            newDevice.LocationId = locations.Id;
                            _context.Devices.Update(newDevice);
                        }
                        else
                        {
                            newDevice.LocationId = locations.Id;
                            _context.Devices.Update(newDevice);
                        }

                        _applicationCache.RemoveCache(CacheOptions.Devices);
                    }

                    TempData["success"] = "Location successfully updated.";

                    bool wasOffline;
                    if (Boolean.TryParse(Request.Form["wasOffline"], out wasOffline))
                    {
                        GageEvent evt = null;
                        if (wasOffline && !locations.IsOffline)
                        {
                            evt = new GageEvent()
                            {
                                LocationId = locations.Id,
                                EventType = GageEventTypes.MarkedOnline,
                                EventTime = DateTime.UtcNow,
                            };
                        }
                        else if (!wasOffline && locations.IsOffline)
                        {
                            evt = new GageEvent()
                            {
                                LocationId = locations.Id,
                                EventType = GageEventTypes.MarkedOffline,
                                EventTime = DateTime.UtcNow,
                            };
                        }
                        if (evt != null)
                        {
                            //$ TODO: Details?
                            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                            {
                                await sqlcn.OpenAsync();
                                await evt.Save(sqlcn);
                                sqlcn.Close();
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    _applicationCache.RemoveCache(CacheOptions.Locations);

                    if (images.Count > 0)
                    {
                        UploadImage(images, id);
                    }

                    LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), locations, Request.Form["ChangeReason"]);

                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationsExists(locations.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong. Please contact with service provider. Exception Type:(Db Update Concurrency)";
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Something went wrong. Please contact with service provider.<br /> Error: "+ex.Message;
                }
            }
            SetDropdownList();
            ViewBag.Devices = GetDevices(id);
            ViewBag.ElevationTypes = GetElevationTypes(true);
            return View(locations);
        }
        
        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        public IActionResult AddDevice(Devices devices, int AdctestsCount)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (AdctestsCount >= 5 && AdctestsCount <= 999)
                    {
                        _context.Devices.Add(devices);
                        _context.SaveChanges();

                        _context.DevicesConfiguration.Add(new DevicesConfiguration { DeviceId=devices.DeviceId, AdctestsCount=AdctestsCount });
                        _context.SaveChanges();

                        _applicationCache.RemoveCache(CacheOptions.Devices);
                        _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(new { message = "Value should be between 5-999" });
                    }

                }
                catch (Exception ex)
                {
                    var sqlException = ex.InnerException as SqlException;
                    if (sqlException.Number == 2601 || sqlException.Number == 2627)
                    {
                        return Ok(new { message = "Cannot insert duplicate Device ID." });
                    }                        
                }
            }
            return Ok(new { message = "Something went wrong." });
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {

            /* Map to delete records: 
             * Location->(child)uploads-- device(optional.. need to discuss device deletion)
             * Right now devices are unlinking from the location but delete operation is not performing. 
             * Users will have to manually delete the devices.
             * Devices can be child and also can be independent. 
             */

            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();

                // Marking child locations delete
                var locations = _context.Locations.Where(e => ids.Contains(e.Id)).ToList();

                List<Locations> listToDeleteLocations = new List<Locations>();

                if (!User.IsInRole("Admin"))
                {
                    foreach (var item in locations)
                    {
                        if (_userPermissions.CheckPermission(item.Id, GetAspNetUserId(), PermissionOptions.Location))
                        {
                            listToDeleteLocations.Add(item);
                        }
                    }
                }
                else
                {
                    listToDeleteLocations.AddRange(locations);
                }

                if (listToDeleteLocations.Count > 0)
                {
                    listToDeleteLocations.ForEach(e => e.IsDeleted = true);
                    _context.Locations.UpdateRange(listToDeleteLocations);

                    var locationids = listToDeleteLocations.Select(e => e.Id).ToList();
                    
                    // Marking Upload images as delete
                    var uploads = _context.Uploads.Where(e => locationids.Contains(e.LocationId)).ToList();
                    if (uploads.Count > 0)
                    {
                        uploads.ForEach(e => e.IsDeleted = true);
                        _context.Uploads.UpdateRange(uploads);
                        _applicationCache.RemoveCache(CacheOptions.Uploads);
                    }

                    // child devices unlinking
                    var devices = _context.Devices.Where(e => locationids.Contains(e.LocationId ?? 0)).ToList();
                    if (devices.Count > 0)
                    {
                        devices.ForEach(e => e.LocationId = null);
                        _context.Devices.UpdateRange(devices);
                        _applicationCache.RemoveCache(CacheOptions.Devices, true);
                        _applicationCache.RemoveCache(CacheOptions.DeviceConfiguration);
                    }
                }
                await _context.SaveChangesAsync();

                _applicationCache.RemoveCache(CacheOptions.Locations, true);
                _applicationCache.RemoveCache(CacheOptions.UserLocations);
                _applicationCache.RemoveCache(CacheOptions.UserNotifications);
                _applicationCache.RemoveCache(CacheOptions.EventsDetail);

                if (ids.Count() == 1)
                {
                    TempData["success"] = $"Location successfully deleted!";
                }
                else
                {
                    TempData["success"] = $"{ids.Count()} Locations successfully deleted!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact with service provider.";
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
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    await SensorLocationBase.MarkLocationsAsUndeleted(sqlcn, undeleteIds);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"Location successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Locations successfully restored!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult GetImages(int locationId)
        {
            var data = _applicationCache.GetUploads().Where(e => e.LocationId == locationId).Select(e=>e.Image).ToList();
            return Ok(data);
        }

        public async Task<IActionResult> GetLocations(bool includeDevices, bool showDeleted)
        {
            List<SensorLocationBase> locations;
            List<DeviceBase> devices = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                if (includeDevices)
                {
                    devices = await DeviceBase.GetDevicesAsync(sqlcn);
                }
                sqlcn.Close();
            }
            if (!showDeleted)
            {
                locations = locations.Where(e => !e.IsDeleted).ToList();
            }
            foreach (SensorLocationBase location in locations)
            {
                location.ConvertValuesForDisplay();
            }
            return Ok(JsonConvert.SerializeObject(new { Data = locations, Devices = devices }));
        }

        // Location Checking.
        private bool LocationsExists(int id)
        {
            return _applicationCache.GetLocations().Any(e => e.Id == id);
        }

        public bool LocationExistsByName(int id, string name)
        {
            return _applicationCache.GetLocations().Any(e => e.Id != id && e.LocationName == name);
        }

        public bool CheckDeviceIdExist(int DeviceId)
        {
            return _applicationCache.GetDevices().Any(e => e.DeviceId == DeviceId);
        }

        public bool CheckPublicLocationIdExists(string locId)
        {
            return _applicationCache.GetLocations().Any(e => locId.Equals(e.PublicLocationId, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool CheckRankExist(int regionId, int locationId, double? rank)
        {
            if (rank == null)
            {
                return false;
            }
            return _context.Locations.AsNoTracking().Any(e => e.RegionId == regionId && e.Id != locationId && e.Rank == rank);
        }
        
    }
}
