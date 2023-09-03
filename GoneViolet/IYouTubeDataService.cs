using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IYouTubeDataService
    {
        Task<ListChannelResponseItem> ListChannel(string value);
    }
}
