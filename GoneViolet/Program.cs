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
            Option<bool> manualDownloadUrl = new Option<bool>(name: "--manual-download-url");
            Option<bool> saveTube = new Option<bool>(name: "--save-tube");

            Command videoCommand = new Command("video", "Process channel video")
            {
                channelOption,
                channelId,
                playlistId,
                manualDownloadUrl,
                saveTube
            };
            videoCommand.SetHandler(
                (opt, cId, lstID, manUrl, svTube) => BeginVideoProcessing(appSettings, opt, cId, lstID, manUrl, svTube), channelOption, channelId, playlistId, manualDownloadUrl, saveTube);

            Command audioCommand = new Command("audio", "Process channel mp3")
            {
                channelOption,
                channelId,
                playlistId,
                manualDownloadUrl,
                saveTube
            };
            audioCommand.SetHandler(
                (opt, cId, lstID, manUrl, svTube) => BeginAudioProcessing(appSettings, opt, cId, lstID, manUrl, svTube), channelOption, channelId, playlistId, manualDownloadUrl, saveTube);

            RootCommand rootCommand = new RootCommand();
            rootCommand.AddCommand(videoCommand);
            rootCommand.AddCommand(audioCommand);
            await rootCommand.InvokeAsync(args);
        }

        private static async Task BeginVideoProcessing(AppSettings appSettings, string channelTitle, string channelId, string playlistId, bool manualDownloadUrl, bool saveTube)
        {
            using (ILifetimeScope scope = DependencyInjection.ContainerFactory.BeginLifetimeScope())
            {
                ILogger logger = scope.Resolve<Func<string, ILogger>>()("Program");
                try
                {
                    Channel channel = await InitializeChannel(
                        scope,
                        logger,
                        appSettings,
                        channelTitle,
                        channelId,
                        playlistId);
                    if (channel != null)
                    {
                        logger.LogInformation($"Channel processing started");
                        IVideoProcessor videoProcessor = scope.Resolve<IVideoProcessor>();
                        videoProcessor.ManualDownloadUrl = manualDownloadUrl && !saveTube;
                        videoProcessor.SaveTube = saveTube;
                        await DownloadVideos(
                            appSettings,
                            channel,
                            videoProcessor,
                            scope.Resolve<IChannelDataService>());
                        logger.LogInformation($"Channel processing ended");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
        }

        private static async Task BeginAudioProcessing(AppSettings appSettings, string channelTitle, string channelId, string playlistId, bool manualDownloadUrl, bool saveTube)
        {
            using (ILifetimeScope scope = DependencyInjection.ContainerFactory.BeginLifetimeScope())
            {
                ILogger logger = scope.Resolve<Func<string, ILogger>>()("Program");
                try
                {
                    Channel channel = await InitializeChannel(
                        scope,
                        logger,
                        appSettings,
                        channelTitle,
                        channelId,
                        playlistId);
                    if (channel != null)
                    {
                        logger.LogInformation($"Channel processing started");
                        IAudioProcessor audioProcessor = scope.Resolve<IAudioProcessor>();
                        audioProcessor.ManualDownloadUrl = manualDownloadUrl && !saveTube;
                        audioProcessor.SaveTube = saveTube;
                        await DownloadAudios(
                            appSettings,
                            channel,
                            audioProcessor,
                            scope.Resolve<IChannelDataService>());
                        logger.LogInformation($"Channel processing ended");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
        }

        private static async Task<Channel> InitializeChannel(
            ILifetimeScope scope,
            ILogger logger,
            AppSettings appSettings,
            string channelTitle,
            string channelId,
            string playlistId)
        {
            Channel channel = null;
            logger.LogInformation($"Begin Channel Initialization");
            if (string.IsNullOrEmpty(channelTitle))
            {
                logger.LogError($"Channel argument not set");
            }
            else
            {
                logger.LogInformation($"Channel argument set \"{channelTitle}\"");
                IPlayListProcessor playListProcessor = scope.Resolve<IPlayListProcessor>();
                if (string.IsNullOrEmpty(playlistId))
                    playlistId = await playListProcessor.SearchChannelPlaylistId(channelTitle, channelId);
                IChannelDataService channelDataService = scope.Resolve<IChannelDataService>();
                await channelDataService.CreateSnapshot();
                channel = await channelDataService.GetChannel();
                channel.Title = channelTitle;
                channel.Id = channelId;
                channel.PlaylistId = playlistId;
                if ((channel.YouTubDataTimestamp ?? DateTime.MinValue).ToUniversalTime() < DateTime.UtcNow.AddHours(-24))
                {
                    // every 24 hours refresh the the list of videos from the YouTube playlist
                    await playListProcessor.GetPlaylistItems(channel);
                    await channelDataService.SaveChannel(channel);
                    if (!string.IsNullOrEmpty(appSettings.PlaylistsDataFile))
                        await SavePlayLists(playListProcessor, channelDataService, logger, channelId);
                }
            }
            logger.LogInformation($"End Channel Initialization");
            return channel;
        }

        private static async Task SavePlayLists(
            IPlayListProcessor playListProcessor,
            IChannelDataService channelDataService,
            ILogger logger,
            string channelId)
        {
            logger.LogInformation("Retrieving playlists");
            List<Playlist> playlists = await playListProcessor.GetPlaylistsByChannelId(channelId);
            if (playlists != null && playlists.Count > 0)
            {
                await channelDataService.SavePlaylists(playlists);
                logger.LogInformation("Playlists saved");
            }
        }

        private static async Task DownloadVideos(AppSettings appSettings, Channel channel, IVideoProcessor videoProcessor, IChannelDataService channelDataService)
        {
            static async Task SaveVideos(ConcurrentQueue<Video> videos, Channel channel, IVideoProcessor videoProcessor, IChannelDataService channelDataService)
            {
                Video video;
                DateTime lastSnapshot = DateTime.UtcNow;
                while (videos.TryDequeue(out video))
                {
                    if (await videoProcessor.UpdateIsStored(video))
                    {
                        lock (channel)
                        {
                            channelDataService.SaveChannel(channel).Wait();
                        }
                    }
                    await videoProcessor.SaveGoogleVideo(video);
                    lock (channel)
                    {
                        channelDataService.SaveChannel(channel).Wait();
                        if (lastSnapshot.AddMinutes(6) < DateTime.UtcNow)
                        {
                            channelDataService.CreateSnapshot().Wait();
                            lastSnapshot = DateTime.UtcNow;
                        }
                    }
                }
            }
            ConcurrentQueue<Video> videos = new ConcurrentQueue<Video>(
                channel.Videos.Where(v => !string.IsNullOrEmpty(v.VideoId) && !(v.Skip ?? false)));
            List<Task> tasks = new List<Task>();
            short maxThreadCount = Math.Min(appSettings.MaxThreadCount ?? 4, (short)128);
            if (maxThreadCount > 1)
            {
                for (int i = 0; i < maxThreadCount - 1; i += 1)
                {
                    tasks.Add(Task.Run(() => SaveVideos(videos, channel, videoProcessor, channelDataService).Wait()));
                }
            }
            await SaveVideos(videos, channel, videoProcessor, channelDataService);
            await Task.WhenAll(tasks);
        }

        private static async Task DownloadAudios(AppSettings appSettings, Channel channel, IAudioProcessor audioProcessor, IChannelDataService channelDataService)
        {
            static async Task SaveAudios(ConcurrentQueue<Video> videos, Channel channel, IAudioProcessor audioProcessor, IChannelDataService channelDataService)
            {
                Video video;
                while (videos.TryDequeue(out video))
                {
                    if (await audioProcessor.UpdateIsStored(video))
                    {
                        lock (channel)
                        {
                            channelDataService.SaveChannel(channel).Wait();
                        }
                    }
                    await audioProcessor.SaveGoogleAudio(video);
                    lock (channel)
                    {
                        channelDataService.SaveChannel(channel).Wait();
                    }
                }
            }
            ConcurrentQueue<Video> videos = new ConcurrentQueue<Video>(
                channel.Videos.Where(v => !string.IsNullOrEmpty(v.VideoId) && !(v.Skip ?? false)));
            List<Task> tasks = new List<Task>();
            short maxThreadCount = Math.Min(appSettings.MaxThreadCount ?? 4, (short)128);
            if (maxThreadCount > 1)
            {
                for (int i = 0; i < maxThreadCount - 1; i += 1)
                {
                    tasks.Add(Task.Run(() => SaveAudios(videos, channel, audioProcessor, channelDataService).Wait()));
                }
            }
            await SaveAudios(videos, channel, audioProcessor, channelDataService);
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