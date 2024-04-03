using GoneViolet.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoneViolet
{
    // the channel data is stored as json file in the root of the blob container
    // the get method reads and deserializes the file
    // the save method serializes the channel data and saves the file
    public class ChannelDataService : IChannelDataService
    {
        private readonly AppSettings _appSettings;
        private readonly IBlob _blob;

        public ChannelDataService(AppSettings appSettings, IBlob blob)
        {
            _appSettings = appSettings;
            _blob = blob;
        }

        public async Task<Channel> GetChannel()
        {
            Channel channel = null;
            if (!string.IsNullOrEmpty(_appSettings.ChannelDataFile))
            {
                using Stream blobStream = await _blob.Download(_appSettings, _appSettings.ChannelDataFile);
                if (blobStream != null)
                {
                    JsonSerializer serializer = JsonSerializer.Create(SerializerSettings());
                    using StreamReader streamReader = new StreamReader(blobStream, SerializerEncoding());
                    using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
                    channel = serializer.Deserialize<Channel>(jsonTextReader);
                }
            }
            return channel ?? new Channel();
        }

        private static UTF8Encoding SerializerEncoding() => new UTF8Encoding(false);

        private static JsonSerializerSettings SerializerSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
#if DEBUG
            settings.Formatting = Formatting.Indented;
#endif
            return settings;
        }

        public async Task SaveChannel(Channel channel)
        {
            if (!string.IsNullOrEmpty(_appSettings.ChannelDataFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings());
                using Stream blobStream = await _blob.OpenWrite(_appSettings, _appSettings.ChannelDataFile, contentType: "application/json");
                using StreamWriter streamWriter = new StreamWriter(blobStream, SerializerEncoding());
                using JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter);
                serializer.Serialize(jsonTextWriter, channel);
            }
        }

        public async Task SavePlaylists(List<Playlist> playlists)
        {
            if (!string.IsNullOrEmpty(_appSettings.PlaylistsDataFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings());
                using Stream blobStream = await _blob.OpenWrite(_appSettings, _appSettings.PlaylistsDataFile, contentType: "application/json");
                using StreamWriter streamWriter = new StreamWriter(blobStream, SerializerEncoding());
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(jsonTextWriter, playlists);
                }
                streamWriter.Close();
                blobStream.Close();
                await _blob.CreateSnapshot(_appSettings, _appSettings.PlaylistsDataFile);
            }
        }

        public async Task CreateSnapshot()
        {
            if (!string.IsNullOrEmpty(_appSettings.ChannelDataFile) && await _blob.Exists(_appSettings, _appSettings.ChannelDataFile))
            {
                await _blob.CreateSnapshot(_appSettings, _appSettings.ChannelDataFile);
            }
        }
    }
}
