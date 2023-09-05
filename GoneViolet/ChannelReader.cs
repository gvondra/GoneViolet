using GoneViolet.Model;
using GoneViolet.Model.YouTube;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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

        public async Task GetPlaylistItems(Channel channel)
        {
            List<PlaylistItem> items = await _youTubeDataService.ListPlaylist(channel.PlaylistId);
            foreach (PlaylistItem item in items)
            {
                Video video = channel.Videos.SingleOrDefault(v => string.Equals(v.VideoId, item.snippet.resourceId.videoId, StringComparison.OrdinalIgnoreCase));
                if (video == null)
                {
                    video = new Video()
                    {
                        PublishedTimestammp = item.snippet.publishedAt,
                        VideoId = item.snippet.resourceId.videoId
                    };
                    channel.Videos.Add(video);
                }
                video.Title = item.snippet.title;
            }
            channel.YouTubDataTimestamp = DateTime.UtcNow;
        }
    }
}
