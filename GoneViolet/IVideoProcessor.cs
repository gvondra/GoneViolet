using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IVideoProcessor
    {
        Task Process(string pageUrl);
    }
}
