using System.Collections.Generic;

namespace GoneViolet.Model
{
    public class Channel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string PlaylistId { get; set; }
        public DateTime? YouTubDataTimestamp { get; set; }
        public List<Video> Videos { get; set; } = new List<Video>();
    }
}
