using BrassLoon.RestClient;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class VideoDownloader : IVideoDownloader
    {   
        private readonly IService _service;
        private readonly RestUtil _restUtil;

        public VideoDownloader(
            IService service,
            RestUtil restUtil)
        {
            _service = service;
            _restUtil = restUtil;
        }

        public async Task<string> DownloadWebContent(string url)
        {
            IRequest request = _service.CreateRequest(new Uri(url), HttpMethod.Get);
            IResponse response = await _service.Send(request);
            _restUtil.CheckSuccess(response);
            return await response.Message.Content.ReadAsStringAsync();
        }

        public async Task Download(string url, Stream output)
        {
            using (Stream stream = await _service.GetStream(new Uri(url), TimeSpan.FromMinutes(30)))
            {
                await stream.CopyToAsync(output);
            }
        }
    }
}
