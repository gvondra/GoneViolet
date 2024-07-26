using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IAudioProcessor
    {
        bool ManualDownloadUrl { get; set; }
        bool SaveTube { get; set; }

        Task SaveGoogleAudio(Video video);
        Task<bool> UpdateIsStored(Video video);
    }
}
