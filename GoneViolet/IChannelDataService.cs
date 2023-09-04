using GoneViolet.Model;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IChannelDataService
    {
        Task<Channel> GetChannel();
        Task SaveChannel(Channel channel);
    }
}
