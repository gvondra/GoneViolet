using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GoneViolet
{
    public abstract class AudioVideoProessor
    {
        private static readonly object _userInputLock = new { };
        private static readonly AsyncPolicy _circuitBreaker = Policy.Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
            0.75,
            TimeSpan.FromMinutes(10),
            10,
            TimeSpan.MaxValue);
        private static readonly AsyncPolicy _retry = Policy.WrapAsync(
            Policy.Handle<HttpRequestException>(HttpRequestExceptionHandler)
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) }),
            Policy.Handle<IOException>()
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.FromSeconds(5) }));
        private readonly IVideoDownloader _downloader;
        private readonly IBlob _blob;

        protected AudioVideoProessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeHtmlParser youTubeParser,
            ILogger logger,
            IBlob blob)
        {
            AppSettings = appSettings;
            _downloader = downloader;
            YouTubeHtmlParser = youTubeParser;
            Logger = logger;
            _blob = blob;
        }

        public virtual bool ManualDownloadUrl { get; set; }

        public virtual bool SaveTube { get; set; }

        protected AppSettings AppSettings { get; private init; }

        protected ILogger Logger { get; private init; }

        protected IYouTubeHtmlParser YouTubeHtmlParser { get; private init; }

        public virtual async Task<bool> UpdateIsStored(Video video)
        {
            if (IsStored(video) && await ShouldDownload(video))
            {
                IsStored(video, false);
                return true;
            }
            return false;
        }

        protected abstract Task<string> GetGoogleAudioVideoUrl(string content, string videoId);

        protected abstract bool IsStored(Video video);

        protected abstract void IsStored(Video video, bool isStored);

        protected abstract string BlobNameTemplate(Video video);

        protected abstract string BlobName(Video video);

        protected abstract string BlobName(Video video, string name);

        protected abstract string ContentType();

        protected async Task<bool> ShouldDownload(Video video)
            => !string.IsNullOrEmpty(video.VideoId) && (!IsStored(video) || !await _blob.Exists(AppSettings, BlobName(video)) || await _blob.GetContentLength(AppSettings, BlobName(video)) == 0);

        protected virtual async Task SaveGoogleAudioVideoWithErrorHandler(Video video)
        {
            string content = null;
            try
            {
                UpdateBlobName(video);
                await _circuitBreaker.ExecuteAsync(async () =>
                {
                    string googleVideoUrl = null;
                    if (await ShouldDownload(video))
                    {
                        content = await GetContent(video);
                        if (ManualDownloadUrl)
                        {
                            googleVideoUrl = GetManualDownloadUrl(video);
                        }
                        else if (SaveTube)
                        {
                            googleVideoUrl = GetSaveTubeDownloadUrl(video);
                        }
                        else
                        {
                            googleVideoUrl = await GetGoogleAudioVideoUrl(content, video.VideoId);
                        }
                        video.Tags = YouTubeHtmlParser.GetTags(content);
                        if (!string.IsNullOrEmpty(googleVideoUrl))
                        {
                            await _retry.ExecuteAsync(async () =>
                            {
                                Logger.LogInformation($"Downloading {video.Title} to blob {BlobName(video)}");
                                using Stream blobStream = await _blob.OpenWrite(AppSettings, BlobName(video), contentType: ContentType());
                                await _downloader.Download(googleVideoUrl, blobStream);
                            });
                            IsStored(video, true);
                            video.Skip = null;
                        }
                        else
                        {
                            Logger.LogWarning($"Video url not found for \"{video.Title}\"");
                        }
                    }
                });
            }
            catch (BrokenCircuitException ex)
            {
                Logger.LogError(ex, ex.Message);
            }
            catch (Exception ex)
            {
                IsStored(video, false);
                Logger.LogError(ex, ex.Message);
                await LogScript(video.VideoId, ex);
                if (!string.IsNullOrEmpty(content))
                    await LogContent(video.VideoId, content);
                await DeleteBlob(video);
            }
        }

        protected void UpdateBlobName(Video video)
        {
            if (string.IsNullOrEmpty(BlobName(video)))
            {
                BlobName(video, string.Format(CultureInfo.InvariantCulture, BlobNameTemplate(video), video.VideoId));
            }
        }

        protected Task<string> GetContent(Video video)
        {
            string pageUrl = GetPageUrl(video);
            Logger.LogInformation($"Downloading and parsing web page data {pageUrl}");
            return _downloader.DownloadWebContent(pageUrl);
        }

        protected string GetPageUrl(Video video)
            => string.Format(CultureInfo.InvariantCulture, AppSettings.YouTubeUrlTemplate, video.VideoId);

        protected async Task LogContent(string videoId, string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                if (!string.IsNullOrEmpty(AppSettings.WorkingDirectory) && Directory.Exists(AppSettings.WorkingDirectory))
                {
                    // if we don't find the url, write the html to a file, so we can manually analyze it
                    using (FileStream fileStream = new FileStream(Path.Combine(AppSettings.WorkingDirectory, videoId + ".html"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
                        {
                            await writer.WriteAsync(content);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(AppSettings.HtmlContentBlobNameTemplate))
                {
                    using MemoryStream stream = new MemoryStream();
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        await writer.WriteAsync(content);
                        await writer.FlushAsync();
                        writer.Close();
                    }
                    stream.Position = 0;
                    await _blob.Upload(AppSettings, string.Format(CultureInfo.InvariantCulture, AppSettings.HtmlContentBlobNameTemplate, videoId), stream, "text/plain");
                }
            }
        }

        protected async Task LogScript(string videoId, Exception ex)
        {
            string script = ex.Data.Contains("script") ? ex.Data["script"]?.ToString() : null;

            if (!string.IsNullOrEmpty(script))
            {
                await LogContent($"script-{videoId}", script);
            }
        }

        protected Task DeleteBlob(Video video)
            => DeleteBlob(BlobName(video));

        protected async Task DeleteBlob(string blobName)
        {
            try
            {
                if (!string.IsNullOrEmpty(blobName) && await _blob.Exists(AppSettings, blobName))
                {
                    await _blob.Delete(AppSettings, blobName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        private static bool HttpRequestExceptionHandler(HttpRequestException exception)
            => exception.StatusCode != HttpStatusCode.Forbidden;

        private string GetManualDownloadUrl(Video video)
        {
            lock (_userInputLock)
            {
                string pageUrl = GetPageUrl(video);
                Console.WriteLine("\nEnter download url for");
                Console.WriteLine(pageUrl);
                return Console.ReadLine();
            }
        }

        private string GetSaveTubeDownloadUrl(Video video)
        {
            string pageUrl = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}",
                AppSettings.SaveTubeBaseAddress,
                HttpUtility.UrlEncode(GetPageUrl(video)));
            lock (_userInputLock)
            {
                Console.WriteLine($"Launched: {pageUrl}");
                Console.WriteLine(video.Title);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pageUrl,
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(startInfo);
                return Console.ReadLine();
            }
        }
    }
}
