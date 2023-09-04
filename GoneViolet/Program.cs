﻿using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.IO;
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
            Option<string> pageUrl = new Option<string>(name: "--pageUrl");
            RootCommand rootCommand = new RootCommand
            {
                channelOption,
                channelId,
                playlistId,
                pageUrl
            };
            rootCommand.SetHandler(
                (channelOptionValue, channelIdValue, playlistIdValue, pageUrlValue)
                => BeginProcessing(channelOptionValue, channelIdValue, playlistIdValue, pageUrlValue), channelOption, channelId, playlistId, pageUrl);
            await rootCommand.InvokeAsync(args);
        }

        private static async Task BeginProcessing(string channel, string channelId, string playlistId, string pageUrl)
        {
            using (ILifetimeScope scope = DependencyInjection.ContainerFactory.BeginLifetimeScope())
            {
                ILogger logger = scope.Resolve<Func<string, ILogger>>()("Program");
                try
                {
                    logger.LogInformation($"Channel processing started");
                    if (string.IsNullOrEmpty(channel))
                    {
                        logger.LogError($"Channel argument not set");
                    }
                    else
                    {
                        logger.LogInformation($"Channel argument set \"{channel}\"");
                        ChannelReader reader = scope.Resolve<ChannelReader>();
                        if (string.IsNullOrEmpty(playlistId))
                            playlistId = await reader.SearchChannelPlaylistId(channel, channelId);
                        await reader.GetPlaylistItesm(playlistId);
                    }
                    if (!string.IsNullOrEmpty(pageUrl))
                    {
                        IVideoProcessor processor = scope.Resolve<IVideoProcessor>();
                        await processor.Process(pageUrl);
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