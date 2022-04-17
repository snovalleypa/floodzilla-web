using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using Newtonsoft.Json;

namespace FzCommon.Processors
{
    public class FloodzillaAzureJob
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
