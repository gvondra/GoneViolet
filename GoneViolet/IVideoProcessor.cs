using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IVideoProcessor
    {
        Task SaveGoogleVideo(Video video, bool skipNonEmptyExistingBlobs);
    }
}
