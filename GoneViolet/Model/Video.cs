using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoneViolet.Model
{
    public class Video
    {
        private DateTime? _publishedTimestamp;

        [Obsolete("Mispelled")]
        public DateTime? PublishedTimestammp { get => _publishedTimestamp; set => _publishedTimestamp = !_publishedTimestamp.HasValue ? value : _publishedTimestamp; }
        [JsonIgnore]
        public DateTime? PublishedTimestamp { get => _publishedTimestamp; set => _publishedTimestamp = value; }
        public string Title { get; set; }
        public string VideoId { get; set; }
        public string BlobName { get; set; }
        public string AudioBlobName { get; set; }
        public bool IsStored { get; set; }
        public bool IsAudioStored { get; set; }
        public bool? Skip { get; set; }
        public List<string> Tags { get; set; }
    }
}
