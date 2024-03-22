using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IBlob
    {
        Task<Stream> OpenWrite(AppSettings settings, string name, string contentType = null);
        Task Upload(AppSettings settings, string name, Stream stream);
        Task<Stream> Download(AppSettings settings, string name);
        Task CreateSnapshot(AppSettings settings, string name);
    }
}
