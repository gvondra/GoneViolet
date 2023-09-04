namespace GoneViolet.Model.YouTube
{
    public class PlaylistItem
    {
        public string kind { get; set; }
        public string id { get; set; }
        public PlaylistItemSnippet snippet { get; set; }
    }
}