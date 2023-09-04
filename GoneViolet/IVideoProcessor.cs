using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IVideoProcessor
    {
        Task SetGoogleVideoUrl(Video video);
    }
}
