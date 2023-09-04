using GoneViolet.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class ChannelDataService : IChannelDataService
    {
        private readonly AppSettings _appSettings;

        public ChannelDataService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public Task<Channel> GetChannel()
        {
            Channel channel;
            if (!string.IsNullOrEmpty(_appSettings.ChannelDataFile) && File.Exists(_appSettings.ChannelDataFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings());
                using FileStream fileStream = new FileStream(_appSettings.ChannelDataFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using StreamReader streamReader = new StreamReader(fileStream, SerializerEncoding());
                using JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
                channel = serializer.Deserialize<Channel>(jsonTextReader);
            }
            else
            {
                channel = new Channel();
            }
            return Task.FromResult(channel);
        }

        private static Encoding SerializerEncoding() => new UTF8Encoding(false);

        private static JsonSerializerSettings SerializerSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings() { ContractResolver = new DefaultContractResolver() };
#if DEBUG
            settings.Formatting = Formatting.Indented;
#endif
            return settings;
        }

        public Task SaveChannel(Channel channel)
        {
            if (!string.IsNullOrEmpty(_appSettings.ChannelDataFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings());
                using FileStream fileStream = new FileStream(_appSettings.ChannelDataFile, FileMode.Create, FileAccess.Write, FileShare.None);
                using StreamWriter streamWriter = new StreamWriter(fileStream, SerializerEncoding());
                using JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter);
                serializer.Serialize(jsonTextWriter, channel);
            }
            return Task.CompletedTask;
        }
    }
}
