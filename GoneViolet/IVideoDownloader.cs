using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IVideoDownloader
    {
        Task<string> DownloadWebContent(string url);
        Task Download(string url, Stream output);
    }
}
