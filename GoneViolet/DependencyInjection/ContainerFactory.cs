using Autofac;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Globalization;
using System.Reflection;

namespace GoneViolet.DependencyInjection
{
    public static class ContainerFactory
    {
        private static IContainer _container;

        static ContainerFactory()
        {
            Initialize();
        }

        public static void Initialize(
            AppSettings appSettings = null)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterModule(new GoneVioletModule());
            if (appSettings != null)
            {
                builder.RegisterInstance(appSettings);
                RegisterLogging(builder, appSettings);
            }
            _container = builder.Build();
        }

        private static void RegisterLogging(ContainerBuilder builder, AppSettings settings)
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(settings.LogFile))
            {
                loggerConfiguration.WriteTo.File(
                    settings.LogFile,
                    formatProvider: CultureInfo.InvariantCulture);
            }
            builder.Register(c => LoggerFactory.Create(b =>
            {
                b.AddSerilog(loggerConfiguration.CreateLogger());
            })).SingleInstance();
            builder.RegisterGeneric((context, types) =>
            {
                ILoggerFactory loggerFactory = context.Resolve<ILoggerFactory>();
                Type factoryType = typeof(LoggerFactoryExtensions);
                MethodInfo methodInfo = factoryType.GetMethod("CreateLogger", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(ILoggerFactory) });
                methodInfo = methodInfo.MakeGenericMethod(types);
                return methodInfo.Invoke(null, new object[] { loggerFactory });

            }).As(typeof(ILogger<>));
            builder.Register<string, Microsoft.Extensions.Logging.ILogger>((context, categoryName) =>
            {
                ILoggerFactory loggerFactory = context.Resolve<ILoggerFactory>();
                return loggerFactory.CreateLogger(categoryName);
            });
        }

        public static ILifetimeScope BeginLifetimeScope() => _container.BeginLifetimeScope();
    }
}
