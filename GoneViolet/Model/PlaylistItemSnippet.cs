namespace GoneViolet.Model
{
    public class PlaylistItemSnippet
    {
        public DateTime? publishedAt { get; set; }
        public string channelId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public ResourceId resourceId { get; set; }
    }
}
