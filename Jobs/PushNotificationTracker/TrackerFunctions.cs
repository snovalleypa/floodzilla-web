using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace PushNotificationTracker
{
    // This is a wrapper for all of the supported functions in this project.
    public class ProjectFunctions
    {
        public static async Task ProcessPushNotifications()
        {
            PushNotificationProcessor job = new();
            await job.Execute();
        }
    }

    // These are the Azure-specific entrypoints to run the functions in the project.
    public class TrackerFunctions
        {
#if DEBUG
        [Function("ProcessPushNotifications")]
        public static async Task<HttpResponseData> ProcessPushNotifications([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            await ProjectFunctions.ProcessPushNotifications();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync("Triggered");
            return response;
        }
#else
        [Function("ProcessPushNotifications")]
        // Current schedule is to run once every minute.
        public static async Task ProcessPushNotifications([TimerTrigger("0 * * * * *", RunOnStartup = false)]TimerInfo myTimer)
        {
            await ProjectFunctions.ProcessPushNotifications();
        }
#endif
    }
}
