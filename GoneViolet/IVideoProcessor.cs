using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IVideoProcessor
    {
        bool ManualDownloadUrl { get; set; }

        Task SaveGoogleVideo(Video video);
        Task<bool> UpdateIsStored(Video video);
    }
}
