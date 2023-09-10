using Autofac;
using GoneViolet.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections;
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
                (channelOptionValue, channelIdValue, playlistIdValue)
                => BeginProcessing(channelOptionValue, channelIdValue, playlistIdValue), channelOption, channelId, playlistId);
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
                            await reader.GetPlaylistItems(channel);
                            await channelDataService.SaveChannel(channel);
                        }
                        IVideoProcessor videoProcessor = scope.Resolve<IVideoProcessor>();
                        Queue<Task> tasks = new Queue<Task>();
                        foreach (Video video in channel.Videos.Where(v => !string.IsNullOrEmpty(v.VideoId) && !v.IsStored))
                        {
                            while (tasks.Count >= 2)
                            {
                                await tasks.Dequeue();
                                await channelDataService.SaveChannel(channel);
                            }
                            tasks.Enqueue(Task.Run(async () =>
                            {
                                await videoProcessor.SaveGoogleVideo(video);
                            }));
                        }
                        await Task.WhenAll(tasks);
                        await channelDataService.SaveChannel(channel);
                    }
                    logger.LogInformation($"Channel processing ended");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
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