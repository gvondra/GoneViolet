using System.Collections.Generic;

namespace GoneViolet.Model.YouTube
{
    public class ListChannelResponse
    {
        public string kind { get; set; }
        public string nextPageToken { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<ListChannelResponseItem> items { get; set; }
    }
}