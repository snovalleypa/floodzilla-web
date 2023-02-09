using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using Newtonsoft.Json;

namespace FzCommon.Processors
{
    public class AzureJobHelpers
    {
        public static async Task<BlobContainerClient> EnsureBlobContainer(string containerName)
        {
            BlobServiceClient client = new BlobServiceClient(FzConfig.Config[FzConfig.Keys.AzureStorageConnectionString]);
            BlobContainerClient container = client.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);
            return container;
        }

        public static async Task SaveBlobText(BlobContainerClient bcc, string blobName, string statusText)
        {
            BlobClient blobClient = bcc.GetBlobClient(blobName);
            BinaryData data = new BinaryData(Encoding.UTF8.GetBytes(statusText));
            await blobClient.UploadAsync(data, true);
        }

        public static string GetJobDetailsBlobName(string jobName)
        {
            return String.Format("JobStatus:{0}", jobName);
        }

        public static async Task SaveJobDetailedStatus(string jobName, string details)
        {
            string detailsBlob = String.Format("Job {0} status at {1} UTC:\r\n{2}", jobName, DateTime.UtcNow, details);
            string detailsBlobName = GetJobDetailsBlobName(jobName);
            BlobContainerClient container = await EnsureBlobContainer(FzCommon.StorageConfiguration.JobStatusBlobContainer);
            await SaveBlobText(container, detailsBlobName, detailsBlob);
        }

        public static async Task<string> GetLastJobDetailedStatus(string jobName)
        {
            string detailsBlobName = GetJobDetailsBlobName(jobName);
            BlobContainerClient container = await EnsureBlobContainer(FzCommon.StorageConfiguration.JobStatusBlobContainer);
            BlobClient blobClient = container.GetBlobClient(detailsBlobName);
            BlobDownloadResult response = await blobClient.DownloadContentAsync();
            return Encoding.UTF8.GetString(response.Content.ToArray());
        }

        public static async Task SaveStatusBlob<T>(BlobContainerClient bcc, string blobName, T statusObj)
        {
            string json = JsonConvert.SerializeObject(statusObj);
            await SaveBlobText(bcc, blobName, json);
        }

        public static async Task<T> LoadStatusBlob<T>(BlobContainerClient bcc, string blobName)
        {
            BlobClient blobClient = bcc.GetBlobClient(blobName);
            BlobDownloadResult response = await blobClient.DownloadContentAsync();
            string json = Encoding.UTF8.GetString(response.Content.ToArray());
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
