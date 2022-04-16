using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using FzCommon;

[assembly: FunctionsStartup(typeof(FzSlackBot.SlashFz))]
namespace FzSlackBot
{
    public class SlackResponse
    {
        public string response_type;
        public string text;
    }
    
    public class SlashFz : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder hostBuilder)
        {
            FzConfig.Initialize();
        }

        [FunctionName("SlashFz")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string command = req.Form["text"];

            //$ TODO: actual command dispatch stuff
            string result;
            if (command.ToLower().StartsWith("help"))
            {
                string text =   "FZ Bot Commands:\n"
                              + "- /fz help -- show list of available commands\n"
                              + "- /fz location monitor status -- show status of location monitor\n"
                              + "- /fz what's offline -- show list of currently offline sensors\n"
                              + "\nMore to come...";
                SlackResponse response = new SlackResponse()
                {
                    response_type = "in_channel",
                    text = text,
                };

                OkObjectResult okResult = new OkObjectResult(response);
                okResult.ContentTypes.Clear();
                okResult.ContentTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
                return okResult;
            }
            else if (command.ToLower().StartsWith("location monitor status"))
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(FzConfig.Config[FzConfig.Keys.AzureStorageConnectionString]);
                CloudBlobClient blob = account.CreateCloudBlobClient();
                CloudBlobContainer container = blob.GetContainerReference(FzCommon.StorageConfiguration.MonitorBlobContainer);
                CloudBlockBlob summaryBlob = container.GetBlockBlobReference("LocationMonitor-summary");
                string text = await summaryBlob.DownloadTextAsync();
                SlackResponse response = new SlackResponse()
                {
                    response_type = "in_channel",
                    text = text,
                };

                OkObjectResult okResult = new OkObjectResult(response);
                okResult.ContentTypes.Clear();
                okResult.ContentTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
                return okResult;
            }
            else if (command.ToLower().StartsWith("what's offline"))
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(FzConfig.Config[FzConfig.Keys.AzureStorageConnectionString]);
                CloudBlobClient blob = account.CreateCloudBlobClient();
                CloudBlobContainer container = blob.GetContainerReference(FzCommon.StorageConfiguration.MonitorBlobContainer);
                CloudBlockBlob summaryBlob = container.GetBlockBlobReference("LocationMonitor-offline");
                string text = await summaryBlob.DownloadTextAsync();
                SlackResponse response = new SlackResponse()
                {
                    response_type = "in_channel",
                    text = text,
                };

                OkObjectResult okResult = new OkObjectResult(response);
                okResult.ContentTypes.Clear();
                okResult.ContentTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
                return okResult;
            }
            else
            {
                result = "Command not recognized. Use '/fz help' to get a list of commands.";
            }

            return new OkObjectResult(result);
        }
    }
}
