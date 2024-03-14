using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IYouTubeParser
    {
        Task<string> ParseVideo(string content);
        List<string> GetTags(string content);
    }
}
