using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelYt = GoneViolet.Model.YouTube;

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
                // search for a youtube channel by name
                ModelYt.SearchChannelResponseItem searchResult = await _youTubeDataService.SearchChannel(
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
                // find the id of the "uploads" playlist
                _logger.LogDebug($"Listing Channel with id {channelId}");
                ModelYt.ListChannelResponse targetChannel = await _youTubeDataService.ListChannel(channelId);
                _logger.LogInformation($"Channel uploads play list {targetChannel.items[0].contentDetails.relatedPlaylists.uploads}");
                id = targetChannel.items[0].contentDetails.relatedPlaylists.uploads;
            }
            return id;
        }

        public async Task GetPlaylistItems(Channel channel)
        {
            List<ModelYt.PlaylistItem> items = await _youTubeDataService.ListPlaylist(channel.PlaylistId);
            foreach (ModelYt.PlaylistItem item in items)
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
            if (channel.Videos != null && channel.Videos.Count > 0)
            {
                channel.Videos.Sort(new VideoPublishDateComparer(false));
            }
            channel.YouTubDataTimestamp = DateTime.UtcNow;
        }

        public async Task<List<Playlist>> GetPlaylistsByChannelId(string channelId)
        {
            List<Playlist> playlists = new List<Playlist>();
            List<ModelYt.Playlist> ytPlaylists = await _youTubeDataService.GetPlaylistsByChannelId(channelId);
            foreach (ModelYt.Playlist ytPlaylist in ytPlaylists ?? new List<ModelYt.Playlist>())
            {
                Playlist playlist = new Playlist
                {
                    Id = ytPlaylist.id,
                    PublishedTimestamp = ytPlaylist.snippet.publishedAt,
                    Title = ytPlaylist.snippet.title,
                    Description = ytPlaylist.snippet.description
                };
                playlists.Add(playlist);

                List<ModelYt.PlaylistItem> ytItems = await _youTubeDataService.ListPlaylist(ytPlaylist.id);
                foreach (ModelYt.PlaylistItem ytItem in ytItems ?? new List<ModelYt.PlaylistItem>())
                {
                    playlist.Items.Add(new PlaylistItem
                    {
                        PublishedTimestamp = ytItem.snippet.publishedAt,
                        VideoId = ytItem.snippet.resourceId.videoId,
                        Title = ytItem.snippet.title
                    });
                }
            }
            playlists.Sort(new PlaylistPublishDateComparer(false));
            return playlists;
        }
    }
}
