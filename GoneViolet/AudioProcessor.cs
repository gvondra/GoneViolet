using GoneViolet.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class AudioProcessor : AudioVideoProessor, IAudioProcessor
    {
        public AudioProcessor(
            AppSettings appSettings,
            IVideoDownloader downloader,
            IYouTubeHtmlParser youTubeParser,
            ILogger<AudioProcessor> logger,
            IBlob blob)
            : base(appSettings, downloader, youTubeParser, logger, blob)
        { }

        public Task SaveGoogleAudio(Video video)
            => SaveGoogleAudioVideoWithErrorHandler(video);

        protected override string BlobName(Video video) => video.AudioBlobName;
        protected override string BlobName(Video video, string name) => video.AudioBlobName = name;
        protected override string BlobNameTemplate(Video video) => !string.IsNullOrEmpty(AppSettings.AudioBlobNameTemplate) ? AppSettings.AudioBlobNameTemplate : @"audio/{0}.mp3";
        protected override string ContentType() => "audio/mgeg";

        protected override async Task<string> GetGoogleAudioVideoUrl(string content, string videoId)
        {
            string url = null;
            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(videoId))
            {
                url = await YouTubeHtmlParser.ParseAudio(content);
                if (string.IsNullOrEmpty(url))
                    await LogContent(videoId, content);
            }
            return url;
        }

        protected override bool IsStored(Video video) => video.IsAudioStored;
        protected override void IsStored(Video video, bool isStored) => video.IsAudioStored = isStored;
    }
}
