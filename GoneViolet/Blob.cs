using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class Blob : IBlob
    {
        public Task<Stream> OpenWrite(AppSettings settings, string name)
        {
            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(settings.VideoDataContainerUrl),
                new DefaultAzureCredential());
            BlobClient blobClient = containerClient.GetBlobClient(name);
            return blobClient.OpenWriteAsync(true);
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
                    new DefaultAzureCredential());
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
    }
}
