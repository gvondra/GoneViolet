namespace GoneViolet
{
    public class AppSettings
    {
        public string LogFile { get; set; }
        public string GoogleApiKey { get; set; }
        public string YouTubeDataApiBaseAddress { get; set; }
        public string YouTubeUrlTemplate { get; set; }
        public string SaveTubeBaseAddress { get; set; }
        public string WorkingDirectory { get; set; }
        public string ChannelDataFile { get; set; }
        public string PlaylistsDataFile { get; set; }
        public string VideoDataContainerUrl { get; set; }
        public string BlobNameTemplate { get; set; }
        public string AudioBlobNameTemplate { get; set; }
        public string HtmlContentBlobNameTemplate { get; set; }
        public short? MaxThreadCount { get; set; }
    }
}
