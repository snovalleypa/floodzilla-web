using Azure.Storage.Blobs;
using ExifLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Drawing;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.ViewModels.Uploads;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin, Organization Admin")]
    public class UploadsController : Controller
    {
        private readonly FloodzillaContext _context;
        private IWebHostEnvironment _env;
        private ApplicationCache _applicationCache;
        public UploadsController(FloodzillaContext context, IWebHostEnvironment env, IMemoryCache memoryCache)
        {
            _context = context;
            _env = env;
            _applicationCache = new ApplicationCache(_context, memoryCache);
        }

        private List<SelectListItem> GetLocations(int locationId=0,params int[] locationIds)
        {
            // The following code is to retrive data from cache, if cache not exits, than it will create the cache.
            var locations = _applicationCache.GetLocations().OrderBy(s => s.Latitude).OrderBy(s => s.Longitude).ToList();

            var selectListItems = new List<SelectListItem>();

            if (!User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                var regions = _applicationCache.GetRegions().Where(e => e.OrganizationsId == user.OrganizationsId).ToList();

                locations = locations.Where(e => regions.Select(r => r.RegionId).Contains(e.RegionId)).ToList();
            }

            if (locationIds.Count() > 0)
            {
                var suggestedLocations = locations.Where(e => locationIds.Contains(e.Id)).ToList();
                foreach (var item in suggestedLocations)
                {
                    selectListItems.Add(new SelectListItem { Text = item.LocationName, Value = item.Id.ToString() });
                }
                if (selectListItems.Count > 0)
                    selectListItems.First().Selected = true;
                locations.RemoveAll(e => locationIds.Contains(e.Id));
            }

            foreach (var item in locations)
            {
                if (locationId != 0)
                {
                    if (item.Id == locationId)
                    {
                        selectListItems.Add(new SelectListItem { Text = item.LocationName, Value = item.Id.ToString(), Selected = true });
                    }
                    else
                        selectListItems.Add(new SelectListItem { Text = item.LocationName, Value = item.Id.ToString() });
                }
                else
                    selectListItems.Add(new SelectListItem { Text = item.LocationName, Value = item.Id.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem { Text = "-- Select Location --", Value = "" });
            return selectListItems;
        }

        private List<SelectListItem> GetFloodEvents()
        {
            // Getting floodevents from cache.
            var floodEvents = _applicationCache.GetFloodEvents();
            if (!User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                var regions = _applicationCache.GetRegions().Where(e => e.OrganizationsId == user.OrganizationsId).ToList();

                floodEvents = floodEvents.Where(e => regions.Select(r => r.RegionId).Contains(e.RegionId)).ToList();
            }

            var selectListItems = new List<SelectListItem>();
            foreach (var item in floodEvents)
            {
                selectListItems.Add(new SelectListItem { Text = item.EventName, Value = item.Id.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem { Text = "-- Select Event --", Value = "" });
            return selectListItems;
        }

        private void SetDropdownLists()
        {
            ViewBag.Locations = GetLocations();
            ViewBag.Events = GetFloodEvents();
        }

        private static string RenderTag(object tagValue)
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

        // Function to get nearest location base on provided lat and lng
        private List<NearestLocationViewModel> GetNearestLocation(double latitude, double longitude)
        {
            List<NearestLocationViewModel> nearestLocation = new List<NearestLocationViewModel>();
            string query = "usp_NearestLocationByLatLng";

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter[] sqlParamters = new SqlParameter[2];
                sqlParamters[0] = new SqlParameter() { ParameterName = "@lat", Value = latitude };
                sqlParamters[1] = new SqlParameter() { ParameterName = "@lng", Value = longitude };
                command.Parameters.AddRange(sqlParamters);
                _context.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        nearestLocation.Add(new NearestLocationViewModel()
                        {
                            Id = (int)result["Id"],
                            LocationName = (string)result["LocationName"],
                            Latitude = (double)result["Latitude"],
                            Longitude = (double)result["Longitude"],
                            DistanceKm = (double)result["DistanceKm"]
                        });
                    }
                }
            }
            return nearestLocation;
        }

        private BlobContainerClient CreateBlobContainerClient()
        {
            BlobServiceClient bsc = new BlobServiceClient(FzConfig.Config[FzConfig.Keys.AzureStorageConnectionString]);
            BlobContainerClient bcc = bsc.GetBlobContainerClient(FzConfig.Config[FzConfig.Keys.UploadsBlobContainer]);
            bcc.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            return bcc;
        }

        // Uploading Image
        [NonAction]
        private string UploadImage(IFormFile image, int locationId)
        {
            var extension = Path.GetExtension(image.FileName);
            var imageKey = Guid.NewGuid().ToString() + extension;

            var temp = Path.Combine(_env.WebRootPath, $"tempImages/{locationId}");

            Directory.CreateDirectory(temp);

            using (var stream = new FileStream(Path.Combine(temp, imageKey), FileMode.Create))
            {
                FlipOrientation(image, stream);
                stream.Close();
            }

            BlobContainerClient bcc = this.CreateBlobContainerClient();

            // Uploading Image to azure
            string blobName = locationId + "/" + imageKey;
            BlobClient blobClient = bcc.GetBlobClient(blobName);
            using (var stream = System.IO.File.OpenRead(temp + "\\" + imageKey))
            {
                blobClient.Upload(stream);
                stream.Close();
            }

            UploadResizedImage(bcc, temp, locationId, imageKey, "thumbnails");
            UploadResizedImage(bcc, temp, locationId, imageKey, "medium");
            UploadResizedImage(bcc, temp, locationId, imageKey, "large");

            Directory.Delete(temp,true);
            return blobName;
        }

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

        private void UploadResizedImage(BlobContainerClient bcc, string tempPath, int locationId, string imageKey, string prefix)
        {
            BlobClient blobClient = bcc.GetBlobClient(prefix + "/" + locationId + "/" + imageKey);
            using (var stream = System.IO.File.OpenRead(tempPath + "\\" + prefix + "\\" + imageKey))
            {
                blobClient.Upload(stream);
                stream.Close();
            }
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

        // Will delete image from azure blob storage.
        [NonAction]
        private void DeleteImageFromAzure(string image)
        {
            try
            {
                if (image != null)
                {
                    BlobContainerClient bcc = this.CreateBlobContainerClient();
                    bcc.DeleteBlobIfExists(image);
                }
            }
            catch (Exception)
            {
                //$ TODO: What to do here?
            }
        }

        [NonAction]
        private bool CheckImageFile(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName);
            if ((extension== ".jpg") || (extension == ".jpeg") || (extension == ".png"))
                return true;
            return false;
        }

        // GET: /<controller>/
        public IActionResult Index(int? locationId)
        {
            ViewBag.AzureImageUploadBaseUrl = FzCommon.StorageConfiguration.AzureImageUploadBaseUrl;
            ViewBag.Locations = GetLocations(locationId ?? 0);
            var uploads = _applicationCache.GetUploads().Where(e => e.LocationId == locationId).OrderByDescending(e => e.Rank.HasValue).ThenBy(e => e.Rank).ToList();
            ViewBag.LocationId = locationId;
            return View(uploads);
        }
        
        public IActionResult GetUploads(int locationId)
        {
            return Ok(_applicationCache.GetUploads().Where(e => e.LocationId == locationId).Select(e => new {
                e.Id,
                e.Image,
                e.IsVarified,
                e.DateOfPicture,
                e.Latitude,
                e.Longitude,
                e.Altitude,
                e.Location.LocationName,
                e.Rank
            }).OrderByDescending(e => e.Rank.HasValue).ThenBy(e => e.Rank).ToList());
        }

        public IActionResult Create(int? locationId)
        {
            //$ TODO: config
            ViewBag.AzureImageUploadBaseUrl = FzCommon.StorageConfiguration.AzureImageUploadBaseUrl;
            ViewBag.Locations = GetLocations(locationId ?? 0);
            ViewBag.LocationId = locationId;
            return View();
        }

        [HttpPost]
        public IActionResult Create(Uploads model, IFormFile? UploadFile)
        {
            if (UploadFile == null)
            {
                TempData["error"] = "Image is required. Please select any image.";
                return View(model);
            }
            if(!CheckImageFile(UploadFile))
            {
                TempData["error"] = "Only jpeg, jpg or png file can be upload.";
                SetDropdownLists();
                return View(model);
            }
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.Rank.HasValue)
                    {
                        if (CheckRankExist(model.Id, model.LocationId, model.Rank ?? 0))
                        {
                            TempData["error"] = "Duplicate Rank.";
                            SetDropdownLists();
                            return View(model);
                        }
                    }

                    model.Image = UploadImage(UploadFile,model.LocationId);
                    model.IsVarified = true;

                    _context.Uploads.Add(model);
                    _context.SaveChanges();
                    _applicationCache.RemoveCache(CacheOptions.Uploads);
                    TempData["success"] = "Successfully uploaded.";
                    return RedirectToAction("Index", new { locationId = model.LocationId });
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong. Contact SVPA.";
                }
            }
            SetDropdownLists();
            return View(model);
        }

        public IActionResult Edit(int Id)
        {
            ViewBag.AzureImageUploadBaseUrl = FzCommon.StorageConfiguration.AzureImageUploadBaseUrl;
            SetDropdownLists();
            var record = _context.Uploads.SingleOrDefault(e => e.Id == Id);
            if (record == null)
            {
                return NotFound();
            }
            return View(record);
        }

        [HttpPost]
        public IActionResult Edit(Uploads model, string IsImageChanged, IFormFile? UploadFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.Rank.HasValue)
                    {
                        if (CheckRankExist(model.Id, model.LocationId, model.Rank ?? 0))
                        {
                            TempData["error"] = "Duplicate Rank.";
                            SetDropdownLists();
                            return View(model);
                        }
                    }

                    if (IsImageChanged == "true")
                    {
                        if (!CheckImageFile(UploadFile))
                        {
                            TempData["error"] = "Only jpeg, jpg or png file can be upload.";
                            return View(model);
                        }
                        DeleteImageFromAzure(model.Image);

                        model.Image = UploadImage(UploadFile, model.LocationId);
                    }

                    model.IsVarified = true;
                    _context.Uploads.Update(model);
                    _context.SaveChanges();
                    _applicationCache.RemoveCache(CacheOptions.Uploads);
                    TempData["success"] = "Successfully updated.";
                    return RedirectToAction("Index", new { locationId = model.LocationId });
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong. Contact with service provider.";
                }
            }
            SetDropdownLists();
            return View(model);
        }

        [HttpPost]
        public IActionResult Delete(string deleteUploadIds)
        {
            try
            {
                var ids = deleteUploadIds.Split(',').Select(int.Parse).ToList();
                var uploads = _context.Uploads.Where(e => ids.Contains(e.Id)).ToList();
                foreach (var item in uploads)
                {
                    DeleteImageFromAzure(item.Image);
                }
                _context.Uploads.RemoveRange(uploads);
                _context.SaveChanges();
                if (ids.Count == 1)
                {
                    TempData["success"] = "Upload successfully deleted.";
                }
                else
                {
                    TempData["success"] = $"{ids.Count} uploads successfully deleted.";
                }                    
                _applicationCache.RemoveCache(CacheOptions.Uploads);

                return RedirectToAction("Index", new { locationId = uploads[0].LocationId });
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }
        
        // Ajax call. retrive image detail, suggested location and event.
        [HttpPost]
        public IActionResult GetImageDetail(IFormFile file)
        {
            if (!CheckImageFile(file))
            {
                return Ok(new { msg = false });
            }
            var filePath = Path.Combine(_env.WebRootPath, "tempImages");

            var extension = Path.GetExtension(file.FileName);
            var imageKey = Guid.NewGuid().ToString() + extension;

            Uploads upload = new Uploads();

            double[] GpsLongArray;
            double[] GpsLatArray;
            string GpsLatRef = null;
            string GpsLongRef = null;
            double Altitude = 0;

            var locations = new List<SelectListItem>();

            DateTime PicDate = new DateTime();
            try
            {
                using (var stream = new FileStream(Path.Combine(filePath,imageKey), FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                using (ExifReader reader = new ExifReader(Path.Combine(filePath, imageKey)))
                {
                    if (reader.GetTagValue<Double[]>(ExifTags.GPSLongitude, out GpsLongArray)
                        && reader.GetTagValue<Double[]>(ExifTags.GPSLatitude, out GpsLatArray))
                    {
                        reader.GetTagValue<string>(ExifTags.GPSLatitudeRef, out GpsLatRef);
                        reader.GetTagValue<string>(ExifTags.GPSLongitudeRef, out GpsLongRef);

                        //Convert coordinates to WGS84 decimal
                        // if longitude ref is east than long will be positive, else negative 
                        upload.Longitude = (GpsLongArray[0] + GpsLongArray[1] / 60 + GpsLongArray[2] / 3600) * (GpsLongRef.ToLower() == "e" ? 1 : -1);

                        // if latitude ref is north than lat will be positive, else negative 
                        upload.Latitude = (GpsLatArray[0] + GpsLatArray[1] / 60 + GpsLatArray[2] / 3600) * (GpsLatRef.ToLower() == "n" ? 1 : -1);
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
                return Ok(new { msg = "error" });
            }
            finally
            {
                System.IO.File.Delete(filePath + "\\" + imageKey);
            }
            
            // Getting nearest location base on image lat and long.
            var nearestLocation = GetNearestLocation(upload.Latitude ?? 0, upload.Longitude ?? 0);
            locations = nearestLocation.Count > 0 ? GetLocations(0,nearestLocation.Select(e => e.Id).ToArray()) : GetLocations();

            return Ok(new { upload, locations });
        }

        [HttpPost]
        public IActionResult AddQuickImage(Uploads model, IFormFile UploadFile)
        {
            if (ModelState.IsValid)
            {
                if (UploadFile == null)
                {
                    return Ok(false);
                }
                if (!CheckImageFile(UploadFile))
                {
                    return Ok(false);
                }
                try
                {
                    model.Image = UploadImage(UploadFile, model.LocationId);
                    if (User.IsInRole("Admin"))
                    {
                        model.IsVarified = true;
                    }
                    _context.Uploads.Add(model);
                    _context.SaveChanges();
                    _applicationCache.RemoveCache(CacheOptions.Uploads);
                    return Ok(true);
                }
                catch (Exception)
                {
                }
            }
            return Ok(false);
        }

        public bool CheckRankExist(int id,int locationId, int rank)
        {
            return _context.Uploads.AsNoTracking().Any(e => e.Id != id && e.LocationId == locationId && e.Rank == rank);
        }

    }
}
