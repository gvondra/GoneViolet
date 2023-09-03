using BrassLoon.RestClient;
using GoneViolet.Model;
using System;
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

        public async Task<ListChannelResponseItem> ListChannel(
            string value,
            Func<string, ListChannelResponseItem, bool> predicate)
        {
            IRequest request = CreateListChannelRequest(value);
            ListChannelResponseItem item = null;
            ListChannelResponse listChannelResponse = await _restUtil.Send<ListChannelResponse>(_service, request);
            item = listChannelResponse.items.SingleOrDefault(i => predicate(value, i));
            while (item == null && !string.IsNullOrEmpty(listChannelResponse.nextPageToken))
            {
                request = CreateListChannelRequest(value, listChannelResponse.nextPageToken);
                listChannelResponse = await _restUtil.Send<ListChannelResponse>(_service, request);
                item = listChannelResponse.items.SingleOrDefault(i => predicate(value, i));
            }
            return item;
        }

        private IRequest CreateListChannelRequest(string value, string page = null)
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
    }
}
