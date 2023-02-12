using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Dynamic;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;
using FzCommon.Processors;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles = "Admin,Organization Admin,Gage Steward")]
    public class JobsController : FloodzillaController
    {
        public const string JobName_Latest = "latest";
        
        public JobsController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions)
                : base(context, memoryCache, userPermissions)
        {
        }

        //$ TODO: region
        public async Task<IActionResult> JobControl()
        {
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
            }
            ViewBag.Region = region;
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

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableJob(int disableJobId, string disableReason)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                JobEntry job = await JobEntry.GetJob(sqlcn, disableJobId);
                if (job == null)
                {
                    return NotFound();
                }
                await job.DisableJob(sqlcn, GetUserEmail(), disableReason);

                LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), job, "Disabled: " + disableReason);
                sqlcn.Close();
            }
            return RedirectToAction("JobControl");
        }
        
        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableJob(int enableJobId)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                JobEntry job = await JobEntry.GetJob(sqlcn, enableJobId);
                if (job == null)
                {
                    return NotFound();
                }
                await job.EnableJob(sqlcn);
                LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), job, "Enabled");
                sqlcn.Close();
            }
            return RedirectToAction("JobControl");
        }
        
        public async Task<IActionResult> GetJobsWithStatus(int regionId)
        {
            List<JobEntryWithStatus> ret;
            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                region = RegionBase.GetRegion(sqlcn, regionId);
                ret = await JobEntry.GetAllJobsWithStatus(sqlcn);
                await sqlcn.CloseAsync();
            }
            
            foreach (JobEntryWithStatus entry in ret)
            {
                entry.JobEntry.LastStartTime = region.ToRegionTimeFromUtc(entry.JobEntry.LastStartTime);
                entry.JobEntry.LastEndTime = region.ToRegionTimeFromUtc(entry.JobEntry.LastEndTime);
                entry.JobEntry.LastSuccessfulEndTime = region.ToRegionTimeFromUtc(entry.JobEntry.LastSuccessfulEndTime);
                entry.JobEntry.LastErrorTime = region.ToRegionTimeFromUtc(entry.JobEntry.LastErrorTime);
                entry.JobEntry.DisabledTime = region.ToRegionTimeFromUtc(entry.JobEntry.DisabledTime);
                if (entry.RecentJobRun != null)
                {
                    entry.RecentJobRun.StartTime = region.ToRegionTimeFromUtc(entry.RecentJobRun.StartTime);
                    entry.RecentJobRun.EndTime = region.ToRegionTimeFromUtc(entry.RecentJobRun.EndTime);
                }   
            }
            dynamic resultObject = new ExpandoObject();
            resultObject.data = ret;
            return new ContentResult() { Content = JsonConvert.SerializeObject(resultObject), ContentType = "application/json" };
        }

        public async Task<IActionResult> GetLatestJobDetails(string jobName)
        {
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    string status = await AzureJobHelpers.GetLastJobDetailedStatus(jobName);
                    if (status == null)
                    {
                        return BadRequest("An error occurred while processing this request.");
                    }
                    return SuccessResult(status);
                }
            }
            catch
            {
                //$ TODO: where do these errors go
            }

            return BadRequest("An error occurred while processing this request.");
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
    }
}

