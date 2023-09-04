﻿using GoneViolet.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoneViolet
{
    public interface IYouTubeDataService
    {
        Task<SearchChannelResponseItem> SearchChannel(string value, Func<string, SearchChannelResponseItem, bool> predicate);
        Task<ListChannelResponse> ListChannel(string id);
        Task<List<PlaylistItem>> ListPlaylist(string id);
    }
}
