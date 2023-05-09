using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace PushNotificationTracker
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
        public static async Task ProcessPushNotfications()
        {
            PushNotificationProcessor job = new();
            await job.Execute();
        }
    }

    // These are the Azure-specific entrypoints to run the functions in the project.
    public class TrackerFunctions
        {
#if DEBUG
        [Function("ProcessPushNotfications")]
        public static async Task<HttpResponseData> ProcessPushNotfications([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            await ProjectFunctions.ProcessPushNotfications();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("Triggered");
            return response;
        }
#else
        [Function("ProcessPushNotfications")]
        // Current schedule is to run once every minute.
        public static async Task ProcessPushNotfications([TimerTrigger("0 * * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.ProcessPushNotfications();
        }
#endif
    }
}
