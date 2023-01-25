using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    public class RecentReading
    {
        public int Id;
        public int DeviceId;
        public int DeviceTypeId;
        public string ListenerInfo;
        public string DeleteReason;
        public DateTime Timestamp;
        public DateTime DeviceTimestamp;
        public double GroundHeight;
        public double DistanceReading;
        public double WaterHeight;
        public double PageMinHeight;
        public double PageMaxHeight;
        public double CalcWaterDischarge;
        public double WaterDischarge;
        public double BatteryPercent;
        public double BatteryVoltage;
        public string RawSensorData;
        public double Rssi;
        public ApiFloodLevel Status;
        public bool IsDeleted;
        public double MsecToNextReading;
    }

    [Authorize(Roles = "Admin,Organization Admin,Gage Steward")]
    public class ReportsController : FloodzillaController
    {

        public const string DeviceName_All = "all";
        public const string DeviceName_None = "none";

        public const string JobName_Latest = "latest";
        
        public ReportsController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
                : base(context, memoryCache, userPermissions)
        {
        }
        
        // GET: /<controller>/
        // NOTE: date is expected to be in region time
        public async Task<IActionResult> Index(int? locationId, string readings, string date = null, int page = 0)
        {
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
            }
            DateTime targetDate = region.ToRegionTimeFromUtc(DateTime.UtcNow);
            
            if (date != null)
            {
                if (!DateTime.TryParse(date, out targetDate))
                {
                    targetDate = region.ToRegionTimeFromUtc(DateTime.UtcNow);
                }
            }
            ViewBag.Locations = GetLocations(locationId ?? 0);
            ViewBag.Date = targetDate;
            ViewBag.Highlight = readings;
            return View();
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReadings(int deviceId, string deleteReadingIds)
        {
            string user = GetUserEmail();
            string fullDeleteReason = String.Format("Deleted by {0}:", user);
            IEnumerable<int> deleteIds = deleteReadingIds.Split(',').Select(int.Parse);
            await SensorReading.MarkReadingsAsDeleted(deleteIds, fullDeleteReason);
            return RedirectToAction("DeviceReadings", new { deviceId = deviceId });
        }
        
        [Authorize(Roles = "Admin,Organization Admin,Gage Steward")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocationReadings(int deleteLocationId, string deleteReadingIds, string deleteReason)
        {
            string user = GetUserEmail();
            string fullReason = String.Format("Deleted by {0}: {1}", user, deleteReason ?? "[none]");
            IEnumerable<int> deleteIds = deleteReadingIds.Split(',').Select(int.Parse);
            await SensorReading.MarkReadingsAsDeleted(deleteIds, fullReason);
            await SlackClient.SendReadingsDeletedNotification(deleteLocationId, deleteReadingIds, user, deleteReason);
            return RedirectToAction("Index", new { locationId = deleteLocationId });
        }

        [Authorize(Roles = "Admin,Organization Admin,Gage Steward")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndeleteLocationReadings(int undeleteLocationId, string undeleteReadingIds, string undeleteReason)
        {
            string user = GetUserEmail();
            string fullReason = String.Format("Undeleted by {0}: {1}", user, undeleteReason ?? "[none]");
            IEnumerable<int> undeleteIds = undeleteReadingIds.Split(',').Select(int.Parse);
            await SensorReading.MarkReadingsAsUndeleted(undeleteIds, fullReason);
            await SlackClient.SendReadingsUndeletedNotification(undeleteLocationId, undeleteReadingIds, user, undeleteReason);
            return RedirectToAction("Index", new { locationId = undeleteLocationId });
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpGet]
        public IActionResult GetLocationOfflineStatus(int locationId)
        {
            var locations = _applicationCache.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == locationId);
            if (location == null)
            {
                return BadRequest("An error occurred while processing this request.");
            }
            dynamic ret = new
            {
                IsOffline = location.IsOffline,
            };
            return SuccessResult(ret);
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpGet]
        public async Task<IActionResult> SetLocationOfflineStatus(int locationId, bool isOffline)
        {
            var locations = _applicationCache.GetLocations();
            var location = locations.FirstOrDefault(l => l.Id == locationId);
            if (location == null)
            {
                return BadRequest("An error occurred while processing this request.");
            }
            location.IsOffline = isOffline;

            _context.Locations.Update(location);
            await _context.SaveChangesAsync();
            _applicationCache.RemoveCache(CacheOptions.Locations);

            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    GageEvent evt = new GageEvent()
                    {
                        LocationId = location.Id,
                        EventType = isOffline ? GageEventTypes.MarkedOffline : GageEventTypes.MarkedOnline,
                        EventTime = DateTime.UtcNow,
                    };
                    await evt.Save(sqlcn);
                }
            }
            catch
            {
                //$ TODO: How to handle this kind of failure?
            }

            LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), location, isOffline ? "Marked offline from Reports view" : "Marked online from Reports view");

            dynamic ret = new
            {
                IsOffline = location.IsOffline,
            };
            return SuccessResult(ret);
        }

        private async Task<Tuple<SensorReading, RegionBase>> GetSampleTargetReading(string readings)
        {
            string[] readingList = readings.Split(",");
            if (readingList.Length == 0)
            {
                return null;
            }
            // just look at the first one; if you pass readings from multiple locations and multiple
            // days, you'll get what you deserve
            int readingId;
            if (!Int32.TryParse(readingList[0], out readingId))
            {
                return null;
            }

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();

                //$ TODO: Region
                RegionBase region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                SensorReading sr = await SensorReading.GetReading(sqlcn, readingId);
                if (sr == null)
                {
                    return null;
                }
                return new Tuple<SensorReading, RegionBase>(sr, region);
            }
        }

        public async Task<IActionResult> ViewReadings([FromQuery]string readings)
        {
            Tuple<SensorReading, RegionBase> target = await GetSampleTargetReading(readings);
            if (target == null)
            {
                return RedirectToAction("Index");
            }
            SensorReading sr = target.Item1;
            RegionBase region = target.Item2;
            int locationId = sr.LocationId ?? 0;
            DateTime endDate = region.ToRegionTimeFromUtc(sr.Timestamp).Date;
            return RedirectToAction("Index", new { locationId = locationId, date = endDate.ToString("yyyy-MM-dd"), readings = readings });
        }

        public async Task<IActionResult> ViewSenixLogForReadings([FromQuery]string readings)
        {
            Tuple<SensorReading, RegionBase> target = await GetSampleTargetReading(readings);
            if (target == null)
            {
                return RedirectToAction("SenixLogs");
            }
            SensorReading sr = target.Item1;
            RegionBase region = target.Item2;
            int deviceId = sr.DeviceId ?? 0;
            DateTime endDate = region.ToRegionTimeFromUtc(sr.Timestamp).Date;
            return RedirectToAction("SenixLogs", new { device = deviceId.ToString(), date = endDate.ToString("yyyy-MM-dd"), readings = readings });
        }

        //$ TODO: region
        public async Task<IActionResult> SenixLogs(string device, string date, string readings)
        {
            if (String.IsNullOrEmpty(device))
            {
                device = DeviceName_All;
            }
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
            }
            DateTime targetDate = region.ToRegionTimeFromUtc(DateTime.UtcNow);
            
            if (date != null)
            {
                if (!DateTime.TryParse(date, out targetDate))
                {
                    targetDate = region.ToRegionTimeFromUtc(DateTime.UtcNow);
                }
            }
            ViewBag.Devices = GetDevicesForSenixLogs(device);
            ViewBag.Date = targetDate;
            ViewBag.Highlight = readings;
            return View();
        }

        //$ TODO: region
        //$ TODO: date?
        public async Task<IActionResult> JobStatus(string jobName)
        {
            RegionBase region = null;
            List<string> jobNames = null;

            if (jobName == null)
            {
                jobName = JobName_Latest;
            }
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                jobNames = await JobRunLog.GetJobRunLogJobNamesAsync(sqlcn);
                sqlcn.Close();
            }
            ViewBag.Region = region;
            List<SelectListItem> jobs = new List<SelectListItem>();
            jobs.Add(new SelectListItem() { Text = "Latest Runs", Value = JobName_Latest, Selected = (jobName == JobName_Latest) });
            foreach (string job in jobNames)
            {
                jobs.Add(new SelectListItem() { Text = job, Value = job, Selected = (jobName == job) });
            }
            ViewBag.Jobs = jobs;
            return View();
        }
        
        public IActionResult Stats(int? locationId)
        {
            ViewBag.Locations = GetLocations(locationId ?? 0, (l => l.Devices != null && l.Devices.DeviceType.DeviceTypeId == DeviceTypeIds.Senix));
            return View();
        }

        public async Task<IActionResult> LatestStats()
        {
            ViewBag.AllLocations = _applicationCache.GetLocations();
            ViewBag.StatsSummary = await GageStatistics.GetStatisticsSummary();
            return View();
        }

        public IActionResult DeviceReadings(int? deviceId)
        {
            ViewBag.Devices = GetDevices(deviceId ?? 0);
            return View();
        }

        public List<SelectListItem> GetDevices(int deviceId)
        {
            var devices = _applicationCache.GetDevices();
            var locations = _applicationCache.GetLocations();
            var selectListItems = new List<SelectListItem>();
            foreach (var item in devices)
            {
                string text = item.DeviceId.ToString() + " " + item.Name ?? "";
                if ((item.LocationId ?? 0) != 0)
                {
                    var location = locations.Where(e => e.Id == item.LocationId).FirstOrDefault();
                    if (location != null)
                    {
                        text += " (@ " + location.LocationName + ")";
                    }
                }
                SelectListItem sli = new SelectListItem() { Text = text, Value = item.DeviceId.ToString() };
                if (item.DeviceId == deviceId)
                {
                    sli.Selected = true;
                }
                selectListItems.Add(sli);
            }
            selectListItems.Insert(0, new SelectListItem() { Text="-- Select Device --", Value="0" });
            return selectListItems;
        }

        public List<SelectListItem> GetLocations(int locationId, Predicate<Locations> filter = null)
        {
            var locations = _applicationCache.GetLocations().OrderByDescending(e => e.Rank.HasValue).ThenBy(e => e.Rank).ToList();
            var selectListItems = new List<SelectListItem>();

            foreach (var location in locations)
            {
                if (filter != null)
                {
                    if (!filter(location))
                    {
                        continue;
                    }
                }
                string text;
                if (location.Devices != null)
                {
                    text = location.Devices.DeviceType.DeviceTypeName;
                }
                else
                {
                    text = "[no device]";
                }
                text += ": " + location.LocationName;
                SelectListItem sli = new SelectListItem() { Text = text, Value = location.Id.ToString() };
                if (location.Id == locationId)
                {
                    sli.Selected = true;
                }
                selectListItems.Add(sli);
            }
            selectListItems.Insert(0, new SelectListItem() { Text="-- Select Location --", Value="0" });
            return selectListItems;
        }

        public List<SelectListItem> GetDevicesForSenixLogs(string deviceValue)
        {
            var devices = _applicationCache.GetDevices();
            var locations = _applicationCache.GetLocations();
            var selectListItems = new List<SelectListItem>();

            selectListItems.Add(new SelectListItem() { Text = "All Logs", Value = DeviceName_All, Selected = (deviceValue == DeviceName_All) });
            
            foreach (var item in devices)
            {
                string text = item.Name ?? "" + "[" + item.DeviceId.ToString() + "]";
                if ((item.LocationId ?? 0) != 0)
                {
                    var location = locations.Where(e => e.Id == item.LocationId).FirstOrDefault();
                    if (location != null)
                    {
                        text += " (@ " + location.LocationName + ")";
                    }
                }
                SelectListItem sli = new SelectListItem() { Text = text, Value = item.DeviceId.ToString() };
                if (item.DeviceId.ToString() == deviceValue)
                {
                    sli.Selected = true;
                }
                selectListItems.Add(sli);
            }
            selectListItems.Add(new SelectListItem() { Text = "No Device Found", Value = DeviceName_None, Selected = (deviceValue == DeviceName_None) });
            return selectListItems;
        }

        public async Task<IActionResult> GetReadingsForDevice(double tzOffset, int deviceId)
        {
            List<SensorReading> readings = await SensorReading.GetReadingsForDevice(deviceId, 500, null, null);
            List<RecentReading> ret = new List<RecentReading>();
            foreach (SensorReading sr in readings)
            {
                double? batteryPercent = FzCommonUtility.CalculateBatteryVoltPercentage(sr.BatteryVolt);
                ret.Add(new RecentReading()
                {
                    Id = sr.Id,
                    DeviceId = sr.DeviceId ?? 0,
                    ListenerInfo = sr.ListenerInfo,
                    DeleteReason = sr.DeleteReason,
                    Timestamp = sr.Timestamp == DateTime.MinValue ? DateTime.MinValue : sr.Timestamp.AddMinutes(tzOffset),
                    DeviceTimestamp = sr.DeviceTimestamp == DateTime.MinValue ? DateTime.MinValue : sr.DeviceTimestamp.AddMinutes(tzOffset),
                    GroundHeight = (sr.GroundHeight ?? 0) / 12.0,
                    DistanceReading = (sr.DistanceReading ?? 0) / 12.0,
                    WaterHeight = (sr.WaterHeight ?? 0) / 12.0,
                    WaterDischarge = (sr.WaterDischarge ?? 0),
                    CalcWaterDischarge = (sr.CalcWaterDischarge ?? 0),
                    BatteryPercent = batteryPercent ?? 0,
                    BatteryVoltage = (double)(sr.BatteryVolt ?? 0),
                    RawSensorData = (string)sr.RawSensorData,
                    Rssi = (double)(sr.RSSI ?? 0),
                    Status = ApiLocationStatus.ComputeReadingFloodLevel(sr),
                });
            }
            return Ok(new { data = ret });
        }

        public async Task<IActionResult> GetReadingCountForDevice(int deviceId)
        {
            int count = await SensorReading.GetReadingCountForDevice(deviceId, null, null);
            return Ok(new { data = count });
        }

        //$ TODO: region
        public async Task<IActionResult> GetReadingsForLocation(int locationId, string endDateString, int pageSize, int pageNumber)
        {
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
            }
            
            DateTime? endDate = null;
            if (!String.IsNullOrEmpty(endDateString))
            {
                // assume date is in region time; convert to midnight UTC
                endDate = DateTime.Parse(endDateString + " 23:59:59");
                endDate = region.ToUtcFromRegionTime(endDate.Value);
            }
            int skipRows = (pageSize * pageNumber);
            List<SensorReading> readings = await SensorReading.GetAllReadingsForLocation(locationId, pageSize, null, endDate, skipRows);
            List<RecentReading> ret = new List<RecentReading>();
            double pageMinHeight = Double.MaxValue;
            double pageMaxHeight = Double.MinValue;
            foreach (SensorReading sr in readings)
            {
                if (sr.WaterHeight < pageMinHeight) pageMinHeight = sr.WaterHeight.Value;
                if (sr.WaterHeight > pageMaxHeight) pageMaxHeight = sr.WaterHeight.Value;
            }

            DeviceBase device = null;
            int lastDeviceId = -1;
            var devices = _applicationCache.GetDevices();

            // Pad these a bit so that small variations don't show up as huge variations
            pageMinHeight = (pageMinHeight / 12.0) - .2;
            pageMaxHeight = (pageMaxHeight / 12.0) + .2;
            DateTime lastTime = DateTime.UtcNow;
            foreach (SensorReading sr in readings)
            {
                double? batteryPercent = FzCommonUtility.CalculateBatteryVoltPercentage(sr.BatteryVolt);

                if (sr.DeviceId.HasValue && sr.DeviceId.Value != lastDeviceId && sr.DeviceId.Value != 0)
                {
                    lastDeviceId = sr.DeviceId.Value;
                    device = (DeviceBase)devices.FirstOrDefault(d => d.DeviceId == lastDeviceId);
                }
                
                ret.Add(new RecentReading()
                {
                    Id = sr.Id,
                    DeviceId = sr.DeviceId ?? 0,
                    DeviceTypeId = (device != null) ? device.DeviceTypeId : 0,
                    ListenerInfo = sr.ListenerInfo,
                    DeleteReason = sr.DeleteReason,
                    Timestamp = (sr.Timestamp == DateTime.MinValue) ? DateTime.MinValue : region.ToRegionTimeFromUtc(sr.Timestamp),
                    DeviceTimestamp = (sr.DeviceTimestamp == DateTime.MinValue) ? DateTime.MinValue : region.ToRegionTimeFromUtc(sr.DeviceTimestamp),
                    GroundHeight = (sr.GroundHeight ?? 0) / 12.0,
                    DistanceReading = (sr.DistanceReading ?? 0) / 12.0,
                    WaterHeight = (sr.WaterHeight ?? 0) / 12.0,
                    PageMinHeight = pageMinHeight,
                    PageMaxHeight = pageMaxHeight,
                    WaterDischarge = (sr.WaterDischarge ?? 0),
                    CalcWaterDischarge = (sr.CalcWaterDischarge ?? 0),
                    BatteryPercent = batteryPercent ?? 0,
                    BatteryVoltage = (double)(sr.BatteryVolt ?? 0),
                    RawSensorData = (string)sr.RawSensorData,
                    Rssi = (double)(sr.RSSI ?? 0),
                    Status = ApiLocationStatus.ComputeReadingFloodLevel(sr),
                    IsDeleted = sr.IsDeleted,
                    MsecToNextReading = (lastTime - sr.Timestamp).TotalMilliseconds,
                });
                lastTime = sr.Timestamp;
            }

            int totalCount = await SensorReading.GetReadingCountForLocation(locationId, null, endDate);
            return SuccessResult(new { data = ret, count = totalCount, pageNumber = pageNumber });
        }

        public async Task<IActionResult> GetReadingCountForLocation(int locationId)
        {
            int count = await SensorReading.GetReadingCountForLocation(locationId, null, null);
            return Ok(new { data = count });
        }

        public async Task<IActionResult> GetJobRunLogs(string jobName, int regionId)
        {
            List<RecentJobRun> ret;
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, regionId);
                if (String.IsNullOrEmpty(jobName) || (jobName == JobName_Latest))
                {
                    ret = await JobRunLog.GetLatestJobRunLogsAsync(sqlcn);
                }
                else
                {
                    ret = await JobRunLog.GetJobRunLogsForNameAsync(sqlcn, jobName);
                }
                await sqlcn.CloseAsync();
            }
            
            foreach (RecentJobRun rjr in ret)
            {
                rjr.StartTime = region.ToRegionTimeFromUtc(rjr.StartTime);
                rjr.EndTime = region.ToRegionTimeFromUtc(rjr.EndTime);
            }
            return Ok(new { data = ret });
        }

        public async Task<IActionResult> GetStatsForLocation(double tzOffset, int locationId)
        {
            List<GageStatistics> stats = await GageStatistics.GetStatisticsForLocation(locationId);
            List<GageStatistics> ret = new List<GageStatistics>();
            DateTime lastDate = stats[0].Date.AddMinutes(tzOffset);
            foreach (GageStatistics stat in stats)
            {
                stat.Date = stat.Date.AddMinutes(tzOffset);
                while ((stat.Date.Date - lastDate.Date.Date).TotalDays > 1)
                {
                    DateTime missedDay = lastDate.Date.AddDays(1);
                    ret.Add(new GageStatistics()
                            {
                                LocationId = locationId,
                                Date = missedDay,
                                AverageBatteryMillivolts = 0,
                                PercentReadingsReceived = 0,
                                AverageRssi = 0,
                                SensorUpdateInterval = 0,
                            });
                    lastDate = missedDay;
                }
                ret.Add(stat);
                lastDate = stat.Date;
            }
            ret.Reverse();
            return Ok(new { data = ret });
        }

        public class SenixLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Device { get; set; }
            public string ExternalDeviceId { get; set; }
            public string Receiver { get; set; }
            public string ReceiverId { get; set; }
            public string Result { get; set; }
            public string ClientIP { get; set; }
            public int? Id { get; set; }
            public int? ReadingId { get; set; }
            public SenixLogEntry(RegionBase region, SenixListenerLog log)
            {
                Timestamp = region.ToRegionTimeFromUtc(log.Timestamp);
                ExternalDeviceId = log.ExternalDeviceId;
                ReceiverId = log.ReceiverId;
                if (String.IsNullOrEmpty(ExternalDeviceId))
                {
                    ExternalDeviceId = "n/a";
                }
                Result = log.Result.Substring(0, Math.Min(60, log.Result.Length));
                ClientIP = log.ClientIP;
                Id = log.Id;
                ReadingId = log.ReadingId;
            }
        }
        public async Task<IActionResult> GetSenixLogs(string device, string date)
        {
            List<SenixLogEntry> ret = new List<SenixLogEntry>();
            int deviceId;
            List<SenixListenerLog> logs;
            Dictionary<int, string> deviceNames = new Dictionary<int, string>();
            Dictionary<string, string> receiverNames = new Dictionary<string, string>();

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
            
                //$ TODO: Region
                RegionBase region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                List<DeviceBase> devices = await DeviceBase.GetDevicesAsync(sqlcn);
                List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsAsync(sqlcn);
                List<ReceiverBase> receivers = await ReceiverBase.GetReceiversAsync(sqlcn);

                DateTime targetDate = DateTime.Now;
                if (!DateTime.TryParse(date, out targetDate))
                {
                    targetDate = DateTime.Now;
                }
                targetDate = new DateTime(targetDate.Ticks, DateTimeKind.Unspecified);
                DateTime fromDate = region.ToUtcFromRegionTime(targetDate.Date);
                DateTime toDate = region.ToUtcFromRegionTime(targetDate.AddDays(1).Date);
            
                switch (device)
                {
                    case DeviceName_All:
                        logs = await SenixListenerLog.GetAllLogs(sqlcn, fromDate, toDate);
                        break;
                    case DeviceName_None:
                        logs = await SenixListenerLog.GetLogs(sqlcn, null, fromDate, toDate);
                        break;
                    default:
                        if (!Int32.TryParse(device, out deviceId))
                        {
                            // just to have a fallback
                            deviceId = devices[0].DeviceId;
                        }
                        logs = await SenixListenerLog.GetLogs(sqlcn, deviceId, fromDate, toDate);
                        break;
                }
                foreach (SenixListenerLog log in logs)
                {
                    SenixLogEntry sle = new SenixLogEntry(region, log);

                    if (!log.DeviceId.HasValue)
                    {
                        sle.Device = "- none -";
                    }
                    else
                    {
                        if (deviceNames.ContainsKey(log.DeviceId.Value))
                        {
                            sle.Device = deviceNames[log.DeviceId.Value];
                        }
                        else
                        {
                            DeviceBase logDevice = devices.FirstOrDefault(d => d.DeviceId == log.DeviceId.Value);
                            if (logDevice == null)
                            {
                                sle.Device = "- unknown -";
                            }
                            else
                            {
                                sle.Device = logDevice.Name;
                                SensorLocationBase location = locations.FirstOrDefault(l => l.Id == logDevice.LocationId);
                                if (location == null)
                                {
                                    sle.Device += " (no location)";
                                }
                                else
                                {
                                    sle.Device += " (" + location.LocationName + ")";
                                }
                            }
                            deviceNames[log.DeviceId.Value] = sle.Device;
                        }

                        if (receiverNames.ContainsKey(log.ReceiverId))
                        {
                            sle.Receiver = receiverNames[log.ReceiverId];
                        }
                        else
                        {
                            ReceiverBase logReceiver = receivers.FirstOrDefault(r => r.ExternalReceiverId == log.ReceiverId);
                            if (logReceiver == null)
                            {
                                sle.Receiver = "unknown (" + log.ReceiverId + ")";
                            }
                            else
                            {
                                sle.Receiver = logReceiver.Name;
                            }
                            receiverNames[log.ReceiverId] = sle.Receiver;
                        }
                    }
                    
                    ret.Add(sle);
                }
                sqlcn.Close();
            }

            return SuccessResult(new { data = ret });
        }

        public async Task<IActionResult> GetFullJobRunException(int runId)
        {
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

                    RecentJobRun rjr = await JobRunLog.GetJobRun(sqlcn, runId);
                    if (rjr == null)
                    {
                        return BadRequest("An error occurred while processing this request.");
                    }

                    return SuccessResult(rjr.FullException ?? "");
                }
            }
            catch
            {
                //$ TODO: where do these errors go
            }

            return BadRequest("An error occurred while processing this request.");
        }

        public async Task<IActionResult> GetRawSenixLogData(int logId)
        {
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();

                    SenixListenerLog entry = await SenixListenerLog.GetLogEntry(sqlcn, logId);
                    if (entry == null)
                    {
                        return BadRequest("An error occurred while processing this request.");
                    }

                    dynamic senixData = JsonConvert.DeserializeObject((string)entry.RawSensorData);
                    string formatted = JsonConvert.SerializeObject(senixData, Formatting.Indented);
                    return SuccessResult(formatted);
                }
            }
            catch
            {
                //$ TODO: where do these errors go
            }

            return BadRequest("An error occurred while processing this request.");
        }
    }
}

