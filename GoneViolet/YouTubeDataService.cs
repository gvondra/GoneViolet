using BrassLoon.RestClient;
using GoneViolet.Model;
using System;
using System.Diagnostics;
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

        public async Task<dynamic> ListChannel(string id)
        {
            IRequest request = _service.CreateRequest(new Uri(_appSettings.YouTubeDataApiBaseAddress), HttpMethod.Get)
                .AddPath("channels")
                .AddQueryParameter("key", _appSettings.GoogleApiKey)
                .AddQueryParameter("part", "contentDetails,snippet")
                .AddQueryParameter("maxResults", "10")
                .AddQueryParameter("id", id);
            IResponse response = await _service.Send(request);
            Debug.WriteLine(await response.Message.Content.ReadAsStringAsync());
            return null;
        }

        public async Task<dynamic> ListPlaylist(string id)
        {
            IRequest request = CreateListPlaylistRequest(id);
            IResponse response = await _service.Send(request);
            Debug.WriteLine(await response.Message.Content.ReadAsStringAsync());
            return null;
        }

        private IRequest CreateListPlaylistRequest(string id, string page = null)
        {
            IRequest request = _service.CreateRequest(new Uri(_appSettings.YouTubeDataApiBaseAddress), HttpMethod.Get)
                .AddPath("playlistItems")
                .AddQueryParameter("key", _appSettings.GoogleApiKey)
                .AddQueryParameter("part", "contentDetails,snippet")
                .AddQueryParameter("maxResults", "50")
                .AddQueryParameter("id", id);
            if (!string.IsNullOrEmpty(page))
                request = request.AddQueryParameter("pageToken", page);
            return request;
        }
    }
}
