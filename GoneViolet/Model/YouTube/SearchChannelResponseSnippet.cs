using System;

namespace GoneViolet.Model.YouTube
{
    public class SearchChannelResponseSnippet
    {
        public DateTime? publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string channelTitle { get; set; }
    }
}
