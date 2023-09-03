using GoneViolet.Model;
using System;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IYouTubeDataService
    {
        Task<SearchChannelResponseItem> SearchChannel(string value, Func<string, SearchChannelResponseItem, bool> predicate);
        Task<dynamic> ListChannel(string id);
    }
}
