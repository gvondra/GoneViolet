using System.Collections.Generic;

namespace GoneViolet.Model
{
    public class Playlist
    {
        public string Id { get; set; }
        public DateTime? PublishedTimestamp { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
    }
}
