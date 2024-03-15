using Autofac;
using GoneViolet.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoneViolet
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            AppSettings appSettings = BindAppSettings(
                LoadConfiguration());
            DependencyInjection.ContainerFactory.Initialize(appSettings);

            if (!string.IsNullOrEmpty(appSettings.WorkingDirectory) && !Directory.Exists(appSettings.WorkingDirectory))
                Directory.CreateDirectory(appSettings.WorkingDirectory);

            Argument<string> channelOption = new Argument<string>(name: "channel");
            Option<string> channelId = new Option<string>(name: "--channel-id");
            Option<string> playlistId = new Option<string>(name: "--playlist-id");
            RootCommand rootCommand = new RootCommand
            {
                channelOption,
                channelId,
                playlistId
            };
            rootCommand.SetHandler(
                BeginProcessing, channelOption, channelId, playlistId);
            await rootCommand.InvokeAsync(args);
        }

        private static async Task BeginProcessing(string channelTitle, string channelId, string playlistId)
        {
            using (ILifetimeScope scope = DependencyInjection.ContainerFactory.BeginLifetimeScope())
            {
                ILogger logger = scope.Resolve<Func<string, ILogger>>()("Program");
                try
                {
                    logger.LogInformation($"Channel processing started");
                    if (string.IsNullOrEmpty(channelTitle))
                    {
                        logger.LogError($"Channel argument not set");
                    }
                    else
                    {
                        logger.LogInformation($"Channel argument set \"{channelTitle}\"");
                        ChannelReader reader = scope.Resolve<ChannelReader>();
                        if (string.IsNullOrEmpty(playlistId))
                            playlistId = await reader.SearchChannelPlaylistId(channelTitle, channelId);
                        IChannelDataService channelDataService = scope.Resolve<IChannelDataService>();
                        Channel channel = await channelDataService.GetChannel();
                        channel.Title = channelTitle;
                        channel.Id = channelId;
                        channel.PlaylistId = playlistId;
                        if ((channel.YouTubDataTimestamp ?? DateTime.MinValue).ToUniversalTime() < DateTime.UtcNow.AddHours(-24))
                        {
                            // every 24 hours refresh the the list of videos from the YouTube playlist
                            await reader.GetPlaylistItems(channel);
                            await channelDataService.SaveChannel(channel);
                        }
                        IVideoProcessor videoProcessor = scope.Resolve<IVideoProcessor>();
                        await DownloadVideos(channel, videoProcessor, channelDataService);
                    }
                    logger.LogInformation($"Channel processing ended");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
        }

        private static async Task DownloadVideos(Channel channel, IVideoProcessor videoProcessor, IChannelDataService channelDataService)
        {
            static async Task SaveVideos(ConcurrentQueue<Video> videos, Channel channel, IVideoProcessor videoProcessor, IChannelDataService channelDataService)
            {
                Video video;
                while (videos.TryDequeue(out video))
                {
                    await videoProcessor.SaveGoogleVideo(video);
                    lock (channel)
                    {
                        channelDataService.SaveChannel(channel).Wait();
                    }
                }
            }
            ConcurrentQueue<Video> videos = new ConcurrentQueue<Video>(
                channel.Videos.Where(v => !string.IsNullOrEmpty(v.VideoId) && !v.IsStored && !(v.Skip ?? false)));
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 2; i += 1)
            {
                tasks.Add(Task.Run(() => SaveVideos(videos, channel, videoProcessor, channelDataService).Wait()));
            }
            await SaveVideos(videos, channel, videoProcessor, channelDataService);
            await Task.WhenAll(tasks);
        }

        private static IConfiguration LoadConfiguration()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            ;
            return builder.Build();
        }

        private static AppSettings BindAppSettings(IConfiguration configuration, AppSettings appSettings = null)
        {
            if (appSettings == null)
                appSettings = new AppSettings();
            ConfigurationBinder.Bind(configuration, appSettings);
            return appSettings;
        }
    }
}