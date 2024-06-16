using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoProcessor : AudioVideoProessor, IVideoProcessor
    {
        public VideoProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeHtmlParser youTubeParser,
            ILogger<VideoProcessor> logger,
            IBlob blob)
            : base(appSettings, downloader, youTubeParser, logger, blob)
        { }

        public Task SaveGoogleVideo(Video video)
            => SaveGoogleAudioVideoWithErrorHandler(video);

        protected override string BlobName(Video video) => video.BlobName;
        protected override string BlobName(Video video, string name) => video.BlobName = name;
        protected override string BlobNameTemplate(Video video) => !string.IsNullOrEmpty(AppSettings.BlobNameTemplate) ? AppSettings.BlobNameTemplate : @"videos/{0}.mp4";
        protected override string ContentType() => "video/mp4";

        protected override async Task<string> GetGoogleAudioVideoUrl(string content, string videoId)
        {
            string url = null;
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(videoId))
            {
                url = await YouTubeHtmlParser.ParseVideo(content);
                if (string.IsNullOrEmpty(url))
                    await LogContent(videoId, content);
            }
            return url;
        }

        protected override bool IsStored(Video video) => video.IsStored;
        protected override void IsStored(Video video, bool isStored) => video.IsStored = isStored;
    }
}
