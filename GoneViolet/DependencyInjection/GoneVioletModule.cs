using Autofac;
using BrassLoon.RestClient;

namespace GoneViolet.DependencyInjection
{
    public class GoneVioletModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<AudioProcessor>().As<IAudioProcessor>();
            builder.RegisterType<Blob>().As<IBlob>();
            builder.RegisterType<ChannelDataService>().As<IChannelDataService>();
            builder.RegisterType<PlayListProcessor>().As<IPlayListProcessor>();
            builder.RegisterType<RestUtil>().SingleInstance();
            builder.RegisterType<Service>().As<IService>();
            builder.RegisterType<SignatureCipherDecoder>().As<ISignatureCipherDecoder>();
            builder.RegisterType<VideoDownloader>().As<IVideoDownloader>();
            builder.RegisterType<VideoProcessor>().As<IVideoProcessor>();
            builder.RegisterType<YouTubeDataService>().As<IYouTubeDataService>();
            builder.RegisterType<YouTubeHtmlParser>().As<IYouTubeHtmlParser>();
        }
    }
}
