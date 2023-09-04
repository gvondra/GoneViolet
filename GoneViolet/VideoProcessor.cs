using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoProcessor : IVideoProcessor
    {

        private const string _youTubeUrlTemplage = @"https://www.youtube.com/watch?v={0}";
        private readonly AppSettings _appSettings;
        private readonly IVideoDownloader _downloader;
        private readonly IYouTubeParser _youTubeParser;
        private readonly ILogger<VideoProcessor> _logger;

        public VideoProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeParser youTubeParser,
            ILogger<VideoProcessor> logger)
        {
            _appSettings = appSettings;
            _downloader = downloader;
            _youTubeParser = youTubeParser;
            _logger = logger;
        }

        public async Task SetGoogleVideoUrl(Video video)
        {
            if (!string.IsNullOrEmpty(video.VideoId) && string.IsNullOrEmpty(video.GoogleVideoUrl))
            {
                string pageUrl = string.Format(CultureInfo.InvariantCulture, _youTubeUrlTemplage, video.VideoId);
                _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
                string content = await _downloader.DownloadWebContent(pageUrl);
                video.GoogleVideoUrl = _youTubeParser.ParseVideo(content);
            }
        }
    }
}
