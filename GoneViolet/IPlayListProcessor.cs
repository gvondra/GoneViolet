using GoneViolet.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IPlayListProcessor
    {
        Task<string> SearchChannelPlaylistId(string channel, string channelId);
        Task GetPlaylistItems(Channel channel);
        Task<List<Playlist>> GetPlaylistsByChannelId(string channelId);
    }
}
