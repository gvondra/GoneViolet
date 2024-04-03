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
        private static readonly AsyncPolicy _circuitBreaker = Policy.Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
            0.75,
            TimeSpan.FromMinutes(10),
            10,
            TimeSpan.MaxValue);
        private static readonly AsyncPolicy _retry = Policy.WrapAsync(
            Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) }),
            Policy.Handle<IOException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) }));
        private readonly AppSettings _appSettings;
        private readonly IVideoDownloader _downloader;
        private readonly IYouTubeHtmlParser _youTubeParser;
        private readonly ILogger<VideoProcessor> _logger;
        private readonly IBlob _blob;

        public VideoProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeHtmlParser youTubeParser,
            ILogger<VideoProcessor> logger,
            IBlob blob)
        {
            _appSettings = appSettings;
            _downloader = downloader;
            _youTubeParser = youTubeParser;
            _logger = logger;
            _blob = blob;
        }

        private async Task<string> GetGoogleVideoUrl(string content, string videoId)
        {
            string url = null;
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(videoId))
            {
                url = await _youTubeParser.ParseVideo(content);
                if (string.IsNullOrEmpty(url))
                    LogContent(videoId, content);
            }
            return url;
        }

        private void LogContent(string videoId, string content)
        {
            if (!string.IsNullOrEmpty(_appSettings.WorkingDirectory) && Directory.Exists(_appSettings.WorkingDirectory))
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

        public async Task SaveGoogleVideo(Video video, bool skipNonEmptyExistingBlobs)
        {
            try
            {
                UpdateBlobName(video);
                if (!skipNonEmptyExistingBlobs || !await NonEmptyBlobExists(video))
                {
                    await SaveGoogleVideo(video);
                }
            }
            catch (Exception ex)
            {
                video.IsStored = false;
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task SaveGoogleVideo(Video video)
        {
            string content = null;
            try
            {
                await _circuitBreaker.ExecuteAsync(async () =>
                {
                    string googleVideoUrl = null;
                    if (!string.IsNullOrEmpty(video.VideoId))
                    {
                        string pageUrl = string.Format(CultureInfo.InvariantCulture, _appSettings.YouTubeUrlTemplate, video.VideoId);
                        _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
                        content = await _downloader.DownloadWebContent(pageUrl);
                        googleVideoUrl = await GetGoogleVideoUrl(content, video.VideoId);
                        video.Tags = _youTubeParser.GetTags(content);
                    }
                    if (!string.IsNullOrEmpty(googleVideoUrl))
                    {
                        await _retry.ExecuteAsync(async () =>
                        {
                            _logger.LogInformation($"Downloading video {video.Title} to blob {video.BlobName}");
                            using Stream blobStream = await _blob.OpenWrite(_appSettings, video.BlobName, contentType: "video/mp4");
                            await _downloader.Download(googleVideoUrl, blobStream);
                        });
                        video.IsStored = true;
                        video.Skip = null;
                    }
                    else
                    {
                        _logger.LogWarning($"Video url not found for \"{video.Title}\"");
                    }
                });
            }
            catch (Exception ex)
            {
                video.IsStored = false;
                _logger.LogError(ex, ex.Message);
                if (!string.IsNullOrEmpty(content))
                    LogContent(video.VideoId, content);
            }
        }

        private void UpdateBlobName(Video video)
        {
            if (string.IsNullOrEmpty(video.BlobName))
            {
                string blobNameTemplate = !string.IsNullOrEmpty(_appSettings.BlobNameTemplate) ? _appSettings.BlobNameTemplate : @"videos/{0}.mp4";
                video.BlobName = string.Format(CultureInfo.InvariantCulture, blobNameTemplate, video.VideoId);
            }
        }

        private async Task<bool> NonEmptyBlobExists(Video video)
        {
            return await _blob.Exists(_appSettings, video.BlobName)
                && await _blob.GetContentLength(_appSettings, video.BlobName) > 0;
        }
    }
}
