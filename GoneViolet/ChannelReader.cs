using GoneViolet.Model.YouTube;
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

        public async Task<string> SearchChannelPlaylistId(string channel, string channelId)
        {
            string id = null;
            if (string.IsNullOrEmpty(channelId))
            {
                SearchChannelResponseItem searchResult = await _youTubeDataService.SearchChannel(
                    channel,
                    (v, i) => string.Equals(v, i.snippet.channelTitle, StringComparison.OrdinalIgnoreCase));
                if (searchResult != null)
                {
                    _logger.LogInformation($"Search Found Channel {searchResult.snippet.channelTitle}: {searchResult.snippet.description}");                    
                    channelId = searchResult.snippet.channelId;
                }
            }
            if (!string.IsNullOrEmpty(channelId))
            {
                _logger.LogDebug($"Listing Channel with id {channelId}");
                ListChannelResponse funhausChannel = await _youTubeDataService.ListChannel(channelId);
                _logger.LogInformation($"Channel uploads play list {funhausChannel.items[0].contentDetails.relatedPlaylists.uploads}");
                id = funhausChannel.items[0].contentDetails.relatedPlaylists.uploads;
            }
            return id;
        }

        public async Task GetPlaylistItesm(string id)
        {
            dynamic items = await _youTubeDataService.ListPlaylist(id);
        }
    }
}
