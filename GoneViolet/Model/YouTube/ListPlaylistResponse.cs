using System.Collections.Generic;

namespace GoneViolet.Model.YouTube
{
    public class ListPlaylistResponse
    {
        public string kind { get; set; }
        public string nextPageToken { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<PlaylistItem> items { get; set; }
    }
}
