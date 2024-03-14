using System.Threading.Tasks;

namespace GoneViolet
{
    public interface ISignatureCipherDecoder
    {
        Task<string> Decode(string content, string s);
    }
}
