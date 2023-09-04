using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoProcessor : IVideoProcessor
    {
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

        public async Task Process(string pageUrl)
        {
            _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
            string content = await _downloader.DownloadWebContent(pageUrl);
            //File.WriteAllText(Path.Combine(_appSettings.WorkingDirectory, "content.html"), content);
            string videoUrl = _youTubeParser.ParseVideo(content);
            if (!string.IsNullOrEmpty(videoUrl))
            {
                _logger.LogInformation($"Downloading {videoUrl}");
                using (FileStream stream = new FileStream(Path.Combine(_appSettings.WorkingDirectory, "video.mp4"), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await _downloader.Download(videoUrl, stream);
                }
            }
        }
    }
}
