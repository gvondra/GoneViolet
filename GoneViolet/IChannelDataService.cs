using GoneViolet.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IChannelDataService
    {
        Task<Channel> GetChannel();
        Task SaveChannel(Channel channel);
        Task SavePlaylists(List<Playlist> playlists);
        Task CreateSnapshot();
    }
}
