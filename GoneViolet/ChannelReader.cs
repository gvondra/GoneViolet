using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System;
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
            SearchChannelResponseItem searchResult = await _youTubeDataService.SearchChannel(
                channel,
                (v, i) => string.Equals(v, i.snippet.channelTitle, StringComparison.OrdinalIgnoreCase));
            if (searchResult != null)
            {
                object funhausChannel = await _youTubeDataService.ListChannel(searchResult.snippet.channelId);
                _logger.LogDebug($"Found Channel {searchResult.snippet.channelTitle}: {searchResult.snippet.description}");
            }
                
        }
    }
}
