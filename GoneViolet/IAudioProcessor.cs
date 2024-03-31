using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IAudioProcessor
    {
        Task SaveGoogleAudio(Video video);
    }
}
