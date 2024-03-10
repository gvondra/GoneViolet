using BrassLoon.RestClient;
using GoneViolet.Model.YouTube;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class YouTubeDataService : IYouTubeDataService
    {
        private readonly AppSettings _appSettings;
        private readonly IService _service;
        private readonly RestUtil _restUtil;

        public YouTubeDataService(
            AppSettings appSettings,
            IService service,
            RestUtil restUtil)
        {
            _appSettings = appSettings;
            _service = service;
            _restUtil = restUtil;
        }

        public async Task<SearchChannelResponseItem> SearchChannel(
            string value,
            Func<string, SearchChannelResponseItem, bool> predicate)
        {
            IRequest request = CreateSearchChannelRequest(value);
            SearchChannelResponseItem item = null;
            SearchChannelResponse listChannelResponse = await _restUtil.Send<SearchChannelResponse>(_service, request);
            item = listChannelResponse.items.SingleOrDefault(i => predicate(value, i));
            while (item == null && !string.IsNullOrEmpty(listChannelResponse.nextPageToken))
            {
                request = CreateSearchChannelRequest(value, listChannelResponse.nextPageToken);
                listChannelResponse = await _restUtil.Send<SearchChannelResponse>(_service, request);
                item = listChannelResponse.items.SingleOrDefault(i => predicate(value, i));
            }
            return item;
        }

        private IRequest CreateSearchChannelRequest(string value, string page = null)
        {
            IRequest request = _service.CreateRequest(new Uri(_appSettings.YouTubeDataApiBaseAddress), HttpMethod.Get)
                .AddPath("search")
                .AddQueryParameter("key", _appSettings.GoogleApiKey)
                .AddQueryParameter("part", "snippet")
                .AddQueryParameter("type", "channel")
                .AddQueryParameter("maxResults", "10")
                .AddQueryParameter("q", value);
            if (!string.IsNullOrEmpty(page))
                request = request.AddQueryParameter("pageToken", page);
            return request;
        }

        public Task<ListChannelResponse> ListChannel(string id)
        {
            IRequest request = _service.CreateRequest(new Uri(_appSettings.YouTubeDataApiBaseAddress), HttpMethod.Get)
                .AddPath("channels")
                .AddQueryParameter("key", _appSettings.GoogleApiKey)
                .AddQueryParameter("part", "contentDetails")
                .AddQueryParameter("maxResults", "10")
                .AddQueryParameter("id", id);
            return _restUtil.Send<ListChannelResponse>(_service, request);
        }

        public async Task<List<PlaylistItem>> ListPlaylist(string id)
        {
            IRequest request = CreateListPlaylistRequest(id);
            IResponse<ListPlaylistResponse> response = await _service.Send<ListPlaylistResponse>(request);
            _restUtil.CheckSuccess(response);
            List<PlaylistItem> items = new List<PlaylistItem>(response.Value.items);
            while (!string.IsNullOrEmpty(response.Value.nextPageToken))
            {
                request = CreateListPlaylistRequest(id, response.Value.nextPageToken);
                response = await _service.Send<ListPlaylistResponse>(request);
                _restUtil.CheckSuccess(response);
                items.AddRange(response.Value.items);
            }
            return items;
        }

        private IRequest CreateListPlaylistRequest(string id, string page = null)
        {
            IRequest request = _service.CreateRequest(new Uri(_appSettings.YouTubeDataApiBaseAddress), HttpMethod.Get)
                .AddPath("playlistItems")
                .AddQueryParameter("key", _appSettings.GoogleApiKey)
                .AddQueryParameter("part", "snippet")
                .AddQueryParameter("maxResults", "50")
                .AddQueryParameter("playlistId", id);
            if (!string.IsNullOrEmpty(page))
                request = request.AddQueryParameter("pageToken", page);
            return request;
        }
    }
}
