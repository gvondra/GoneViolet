using System.Collections.Generic;

namespace GoneViolet
{
    public interface IYouTubeParser
    {
        string ParseVideo(string content);
        List<string> GetTags(string content);
    }
}
