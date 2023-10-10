using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoProcessor : IVideoProcessor
    {
        private readonly AppSettings _appSettings;
        private readonly IVideoDownloader _downloader;
        private readonly IYouTubeParser _youTubeParser;
        private readonly ILogger<VideoProcessor> _logger;
        private readonly IBlob _blob;

        public VideoProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeParser youTubeParser,
            ILogger<VideoProcessor> logger,
            IBlob blob)
        {
            _appSettings = appSettings;
            _downloader = downloader;
            _youTubeParser = youTubeParser;
            _logger = logger;
            _blob = blob;
        }

        private async Task<string> GetGoogleVideoUrl(Video video)
        {
            string url = null;
            if (!string.IsNullOrEmpty(video.VideoId))
            {
                string pageUrl = string.Format(CultureInfo.InvariantCulture, _appSettings.YouTubeUrlTemplate, video.VideoId);
                _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
                string content = await _downloader.DownloadWebContent(pageUrl);
                url = _youTubeParser.ParseVideo(content);
                if (string.IsNullOrEmpty(url))
                {
                    using (FileStream fileStream = new FileStream(Path.Combine(_appSettings.WorkingDirectory, video.VideoId + ".html"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
                        {
                            writer.Write(content);
                        }
                    }
                }
            }
            return url;
        }

        public async Task SaveGoogleVideo(Video video)
        {
            try
            {
                string googleVideoUrl = await GetGoogleVideoUrl(video);
                if (!string.IsNullOrEmpty(googleVideoUrl))
                {
                    video.BlobName = $"videos/{video.VideoId}.mp4";
                    _logger.LogInformation($"Downloading video {video.Title} to blob {video.BlobName}");
                    using Stream blobStream = await _blob.OpenWrite(_appSettings, video.BlobName, contentType: "video/mp4");
                    await _downloader.Download(googleVideoUrl, blobStream);
                    video.IsStored = true;
                }
                else
                {
                    _logger.LogWarning($"Video url not found for \"{video.Title}\"");
                }
            }
            catch (Exception ex)
            {
                video.IsStored = false;
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
