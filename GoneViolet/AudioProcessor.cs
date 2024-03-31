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
    public class AudioProcessor : IAudioProcessor
    {
        private static readonly AsyncPolicy _retry = Policy.WrapAsync(
            Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) }),
            Policy.Handle<IOException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) })
            );
        private readonly AppSettings _appSettings;
        private readonly IVideoDownloader _downloader;
        private readonly IYouTubeHtmlParser _youTubeParser;
        private readonly ILogger<AudioProcessor> _logger;
        private readonly IBlob _blob;

        public AudioProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeHtmlParser youTubeParser,
            ILogger<AudioProcessor> logger,
            IBlob blob)
        {
            _appSettings = appSettings;
            _downloader = downloader;
            _youTubeParser = youTubeParser;
            _logger = logger;
            _blob = blob;
        }

        public async Task SaveGoogleAudio(Video video)
        {
            string content = null;
            try
            {
                string googleVideoUrl = null;
                if (!string.IsNullOrEmpty(video.VideoId))
                {
                    string pageUrl = string.Format(CultureInfo.InvariantCulture, _appSettings.YouTubeUrlTemplate, video.VideoId);
                    _logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
                    content = await _downloader.DownloadWebContent(pageUrl);
                    googleVideoUrl = await GetGoogleAudioUrl(content, video.VideoId);
                    if (video.Tags == null || video.Tags.Count == 0)
                        video.Tags = _youTubeParser.GetTags(content);
                }
                if (!string.IsNullOrEmpty(googleVideoUrl))
                {
                    string blobNameTemplate = !string.IsNullOrEmpty(_appSettings.AudioBlobNameTemplate) ? _appSettings.AudioBlobNameTemplate : @"audio/{0}.mp3";
                    video.AudioBlobName = string.Format(CultureInfo.InvariantCulture, blobNameTemplate, video.VideoId);
                    await _retry.ExecuteAsync(async () =>
                    {
                        _logger.LogInformation($"Downloading audio {video.Title} to blob {video.AudioBlobName}");
                        using Stream blobStream = await _blob.OpenWrite(_appSettings, video.AudioBlobName, contentType: "audio/mgeg");
                        await _downloader.Download(googleVideoUrl, blobStream);
                    });
                    video.IsAudioStored = true;
                    video.Skip = null;
                }
                else
                {
                    _logger.LogWarning($"Audio url not found for \"{video.Title}\"");
                }
            }
            catch (Exception ex)
            {
                video.IsAudioStored = false;
                _logger.LogError(ex, ex.Message);
                if (!string.IsNullOrEmpty(content))
                    LogContent(video.VideoId, content);
            }
        }

        private async Task<string> GetGoogleAudioUrl(string content, string videoId)
        {
            string url = null;
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(videoId))
            {
                url = await _youTubeParser.ParseAudio(content);
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
    }
}
