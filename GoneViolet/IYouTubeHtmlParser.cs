using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IYouTubeHtmlParser
    {
        Task<string> ParseVideo(string content);
        Task<string> ParseAudio(string content);
        List<string> GetTags(string content);
    }
}
