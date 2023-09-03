using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AppSettings appSettings = BindAppSettings(
                LoadConfiguration());
            DependencyInjection.ContainerFactory.Initialize(appSettings);
            Argument<string> channelOption = new Argument<string>(name: "channel");
            RootCommand rootCommand = new RootCommand
            {
                channelOption
            };
            rootCommand.SetHandler(
                (channelOptionValue) => BeginProcessing(channelOptionValue), channelOption);
            await rootCommand.InvokeAsync(args);
        }

        private static async Task BeginProcessing(string channel)
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
                        // quota problems
                        // ChannelReader reader = scope.Resolve<ChannelReader>();
                        // await reader.SearchChannel(channel);
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