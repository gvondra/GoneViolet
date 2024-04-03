using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class Blob : IBlob
    {
        public Task<Stream> OpenWrite(
            AppSettings settings,
            string name,
            string contentType = null)
        {
            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(settings.VideoDataContainerUrl),
                AzureCredential.DefaultAzureCredential);
            BlobClient blobClient = containerClient.GetBlobClient(name);
            BlobOpenWriteOptions options = new BlobOpenWriteOptions();
            if (!string.IsNullOrEmpty(contentType))
            {
                options.HttpHeaders = new BlobHttpHeaders()
                {
                    ContentType = contentType
                };
            }
            return blobClient.OpenWriteAsync(true, options);
        }

        public async Task Upload(AppSettings settings, string name, Stream stream)
        {
            using (Stream blobStream = await OpenWrite(settings, name))
            {
                await stream.CopyToAsync(blobStream);
            }
        }

        public async Task<Stream> Download(AppSettings settings, string name)
        {
            Stream result = null;
            try
            {
                BlobContainerClient containerClient = new BlobContainerClient(
                    new Uri(settings.VideoDataContainerUrl),
                    AzureCredential.DefaultAzureCredential);
                BlobClient blobClient = containerClient.GetBlobClient(name);
                result = await blobClient.OpenReadAsync();
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status != 404)
                    throw;
            }
            return result;
        }

        public Task CreateSnapshot(AppSettings settings, string name)
        {
            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(settings.VideoDataContainerUrl),
                AzureCredential.DefaultAzureCredential);
            BlobClient blobClient = containerClient.GetBlobClient(name);
            return blobClient.CreateSnapshotAsync();
        }

        public async Task<bool> Exists(AppSettings settings, string name)
        {
            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(settings.VideoDataContainerUrl),
                AzureCredential.DefaultAzureCredential);
            BlobClient blobClient = containerClient.GetBlobClient(name);
            Response<bool> response = await blobClient.ExistsAsync();
            return response.Value;
        }

        public async Task<long> GetContentLength(AppSettings settings, string name)
        {
            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(settings.VideoDataContainerUrl),
                AzureCredential.DefaultAzureCredential);
            BlobClient blobClient = containerClient.GetBlobClient(name);
            Response<BlobProperties> response = await blobClient.GetPropertiesAsync();
            return response.Value.ContentLength;
        }
    }
}
