using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Concurrent;
using FileSystemManager.ViewModels;
using FileSystemManager.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FileSystemManager;

public class Program
{
    private static readonly Lzy<IHost> host;
    private static ImmutableArray<string> hostArgs { get; set; } = [];
    private static IHost Host_Create()
    {
        var asm = typeof(Program).Assembly;

        var builder = Host.CreateApplicationBuilder(hostArgs.ToArray());
        var s = builder.Services;
        s.AddSingleton<IConfiguration>(builder.Configuration);
        s.AddLogging();
        s.AddLoggerForwarderProvider();
        s.AddOptionsAndBind(asm);
        s.AddServiceAttributeServices(asm, Log);

        var typesAvalonia = new List<(Type, ServiceLifetime)>();
        typesAvalonia.AddRange(GetTypes<ViewBase>(ServiceLifetime.Singleton));
        typesAvalonia.AddRange(GetTypes<WindowBase>(ServiceLifetime.Singleton));
        typesAvalonia.AddRange(GetTypes<ViewModelBase>(ServiceLifetime.Singleton));
        typesAvalonia = typesAvalonia.OrderByOrdinalIgnoreCaseThenOrdinal(o => o.Item1.NameFormatted()).ToList();

        foreach (var (t, lifetime) in typesAvalonia)
        {
            LogRaw(t, null, lifetime);
            builder.Services.Add(new(t, t, lifetime));
        }

        return builder.Build();

        static (Type, ServiceLifetime)[] GetTypes<T>(ServiceLifetime lifetime) => Assembly.GetAssembly(typeof(Program))!
            .GetTypesOf<T>()
            .Select(o => (o, lifetime))
            .ToArray();

        static bool Log(Type type, ServiceAttribute serviceAttribute) => LogRaw(type, serviceAttribute.InterfaceType, serviceAttribute.Lifetime);

        static bool LogRaw(Type type, Type? interfaceType, ServiceLifetime lifetime)
        {
            var sb = new StringBuilder();
            sb.Append(type.NameFormatted());
            if (interfaceType != null && interfaceType != type) sb.Append($"<{interfaceType.NameFormatted()}>");
            sb.Append($" [{lifetime}]");
            Console.WriteLine(sb.ToString());
            return true;
        }
    }

    private static readonly Lzy<ServicesRepository> services;
    public static ServicesRepository Services => services.Value;
    private static ServicesRepository Services_Create() => new(host.Value.Services);

    static Program()
    {
        host = Lzy.Create(Host_Create);
        services = Lzy.Create(Services_Create);
    }



    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        hostArgs = [..args];
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

}
