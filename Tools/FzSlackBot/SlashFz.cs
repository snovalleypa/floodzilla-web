using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

using FzCommon.Processors;

namespace FzSlackBot
{
    public class SlackResponse
    {
        public string response_type;
        public string text;
    }

    public class SlashFz
    {
        [Function("SlashFz")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            string command = req.Form["text"];

            //$ TODO: actual command dispatch stuff
            string result;
            if (command.StartsWith("help", System.StringComparison.CurrentCultureIgnoreCase))
            {
                string text = "FZ Bot Commands:\n"
                              + "- /fz help -- show list of available commands\n"
                              + "- /fz location monitor status -- show status of location monitor\n"
                              + "- /fz what's offline -- show list of currently offline sensors\n"
                              + "\nMore to come...";
                SlackResponse response = new()
                {
                    response_type = "in_channel",
                    text = text,
                };
                return new ContentResult() { Content = JsonConvert.SerializeObject(response), ContentType = "application/json" };
            }
            else if (command.StartsWith("location monitor status", System.StringComparison.CurrentCultureIgnoreCase))
            {
                string text = await GetBlobText("LocationMonitor-summary");
                SlackResponse response = new()
                {
                    response_type = "in_channel",
                    text = text,
                };

                return new ContentResult() { Content = JsonConvert.SerializeObject(response), ContentType = "application/json" };
            }
            else if (command.StartsWith("what's offline", System.StringComparison.CurrentCultureIgnoreCase))
            {
                string text = await GetBlobText("LocationMonitor-offline");
                SlackResponse response = new()
                {
                    response_type = "in_channel",
                    text = text,
                };

                return new ContentResult() { Content = JsonConvert.SerializeObject(response), ContentType = "application/json" };
            }
            else
            {
                result = "Command not recognized. Use '/fz help' to get a list of commands.";
            }

            return new OkObjectResult(result);
        }

        private static async Task<string> GetBlobText(string blobName)
        {
            BlobContainerClient container = await AzureJobHelpers.EnsureBlobContainer(FzCommon.StorageConfiguration.MonitorBlobContainer);
            BlobClient blobClient = container.GetBlobClient(blobName);
            BlobDownloadResult response = await blobClient.DownloadContentAsync();
            return Encoding.UTF8.GetString(response.Content.ToArray());
        }
    }
}
