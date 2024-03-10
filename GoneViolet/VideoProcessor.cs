using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using Polly;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoProcessor : IVideoProcessor
    {
        private static readonly AsyncPolicy _retry = Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) });
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

        private string GetGoogleVideoUrl(string content, string videoId)
        {
            string url = null;
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(videoId))
            {
                url = _youTubeParser.ParseVideo(content);
                if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(_appSettings.WorkingDirectory) && Directory.Exists(_appSettings.WorkingDirectory))
                {
                    // if we don't find the url, write the html to a file, so we can manually analyze it
                    using (FileStream fileStream = new FileStream(Path.Combine(_appSettings.WorkingDirectory, videoId + ".html"), FileMode.Create, FileAccess.Write, FileShare.Read))
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
                string googleVideoUrl = null;
                if (!string.IsNullOrEmpty(video.VideoId))
                {
                    string pageUrl = string.Format(CultureInfo.InvariantCulture, _appSettings.YouTubeUrlTemplate, video.VideoId);
                    _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
                    string content = await _downloader.DownloadWebContent(pageUrl);
                    googleVideoUrl = GetGoogleVideoUrl(content, video.VideoId);
                    video.Tags = _youTubeParser.GetTags(content);
                }
                if (!string.IsNullOrEmpty(googleVideoUrl))
                {
                    string blobNameTemplate = !string.IsNullOrEmpty(_appSettings.BlobNameTemplate) ? _appSettings.BlobNameTemplate : @"videos/{0}.mp4";
                    video.BlobName = string.Format(CultureInfo.InvariantCulture, blobNameTemplate, video.VideoId);
                    await _retry.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation($"Downloading video {video.Title} to blob {video.BlobName}");
                        using Stream blobStream = await _blob.OpenWrite(_appSettings, video.BlobName, contentType: "video/mp4");
                        await _downloader.Download(googleVideoUrl, blobStream);
                    });
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
