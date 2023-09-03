using Autofac;
using BrassLoon.RestClient;

namespace GoneViolet.DependencyInjection
{
    public class GoneVioletModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<ChannelReader>();
            builder.RegisterType<RestUtil>().SingleInstance();
            builder.RegisterType<Service>().As<IService>();
            builder.RegisterType<YouTubeDataService>().As<IYouTubeDataService>();
        }
    }
}
