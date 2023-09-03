using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class ChannelReader
    {
        private readonly IYouTubeDataService _youTubeDataService;
        private readonly ILogger<ChannelReader> _logger;

        public ChannelReader(IYouTubeDataService youTubeDataService, ILogger<ChannelReader> logger)
        {
            _youTubeDataService = youTubeDataService;
            _logger = logger;
        }

        public async Task SearchChannel(string channel)
        {
            ListChannelResponseItem funhausChannel = await _youTubeDataService.ListChannel(channel);
            if (funhausChannel != null)
                _logger.LogDebug($"Found Channel {funhausChannel.snippet.channelTitle}: {funhausChannel.snippet.description}");
        }
    }
}
