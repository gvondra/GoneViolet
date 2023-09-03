using System.Collections.Generic;

namespace GoneViolet.Model
{
    public class SearchChannelResponse
    {
        public string kind { get; set; }
        public string nextPageToken { get; set; }
        public PageInfo pageInfo { get; set; }
        public List<SearchChannelResponseItem> items { get; set; }
    }
}