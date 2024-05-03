using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Concurrent;
using FileSystemManager.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileSystemManager;

sealed class Program
{
    public static ImmutableArray<string> Args { get; private set; } = [];

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Args = [..args];
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();


    private static IHost? hostInstance;

    public static IHost HostInstance
    {
        get
        {
            if (hostInstance != null) return hostInstance;

            var builder = Host.CreateApplicationBuilder(Args.ToArray());
            var s = builder.Services;
            var services = ServiceAttribute.GetTypesWithAttribute<Program>();
            // items
            s.AddSingleton<IConfiguration>(builder.Configuration);
            s.AddLogging();
            s.AddSingleton<ILoggerProvider, LoggerForwarderProvider>();
            s.AddOptions<AppOptions>().BindConfiguration(AppOptions.SECTION);
            foreach (var (type, attribute) in services) s.Add(attribute.ToServiceDescriptor(type));

            s.AddTransient<Main_ViewModel>();

            // logging
            //s.AddLogging();
            /*
            s.AddLogging(b =>
            {
                b.AddSimpleConsole(c =>
                {
                    c.SingleLine = true;
                    c.ColorBehavior = LoggerColorBehavior.Default;
                });
            });
            */

            // Creates a ServiceProvider containing services from the provided IServiceCollection
            return hostInstance = builder.Build();
        }
    }

    #region Services

    private static readonly ConcurrentDictionary<Type, ILogger> loggers = new();
    public static ILogger GetLogger(Type type)
    {
        return loggers.GetOrAdd(type, CreateLogger);

        static ILogger CreateLogger(Type type)
        {
            var genericType = typeof(ILogger<>).MakeGenericType([type]);
            var loggerObj = HostInstance.Services.GetRequiredService(genericType);
            Console.WriteLine("Got logger: " + loggerObj.GetType().FullNameFormatted());
            var logger = (ILogger)loggerObj;
            return logger;
        }
    }

    public static IStorageService StorageService => HostInstance.Services.GetRequiredService<IStorageService>();

    public static ILogItemCollection LogItemCollection => HostInstance.Services.GetRequiredService<ILogItemCollection>();

    #endregion Services

}
