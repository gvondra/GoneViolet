namespace GoneViolet.Model
{
    public class ListChannelResponseItem
    {
        public string kind {  get; set; }
        public string id { get; set; }
        public ContentDetails contentDetails { get; set; }

        public class ContentDetails
        {
            public RelatedPlaylists relatedPlaylists { get; set; }
        }

        public class RelatedPlaylists
        {
            public string uploads { get; set; }
        }
    }
}