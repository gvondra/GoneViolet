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
                    await LogContent(videoId, content);
            }
            return url;
        }

        private async Task LogContent(string videoId, string content)
        {
            if (!string.IsNullOrEmpty(content))
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
                if (!string.IsNullOrEmpty(_appSettings.HtmlContentBlobNameTemplate))
                {
                    using MemoryStream stream = new MemoryStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        await writer.WriteAsync(content);
                        await writer.FlushAsync();
                        writer.Close();
                    }
                    stream.Position = 0;
                    await _blob.Upload(_appSettings, string.Format(CultureInfo.InvariantCulture, _appSettings.HtmlContentBlobNameTemplate, videoId), stream, "text/plain");
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
                else
                {
                    if (video.Tags == null || video.Tags.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(20));
                        video.Tags = _youTubeParser.GetTags(
                            await GetContent(video));
                    }
                    _logger.LogInformation($"Skipping existing blob {video.BlobName}");
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
                        content = await GetContent(video);
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
                    await LogContent(video.VideoId, content);
                await DeleteBlob(video);
            }
        }

        private Task<string> GetContent(Video video)
        {
            string pageUrl = string.Format(CultureInfo.InvariantCulture, _appSettings.YouTubeUrlTemplate, video.VideoId);
            _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
            return _downloader.DownloadWebContent(pageUrl);
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
            return !string.IsNullOrEmpty(video.BlobName)
                && await _blob.Exists(_appSettings, video.BlobName)
                && await _blob.GetContentLength(_appSettings, video.BlobName) > 0;
        }

        private async Task DeleteBlob(Video video)
        {
            try
            {
                if (!string.IsNullOrEmpty(video.BlobName) && await _blob.Exists(_appSettings, video.BlobName))
                {
                    await _blob.Delete(_appSettings, video.BlobName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
