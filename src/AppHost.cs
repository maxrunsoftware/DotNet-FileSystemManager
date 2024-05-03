using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using FileSystemManager.ViewModels;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FileSystemManager;

public class AppHost
{
    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var s = builder.Services;
        var services = ServiceAttribute.GetTypesWithAttribute<Program>();
        // items
        s.AddSingleton<IConfiguration>(builder.Configuration);
        s.AddLogging();
        s.AddSingleton<ILoggerProvider, LoggerForwarderProvider>();
        s.AddOptions<AppOptions>().BindConfiguration(AppOptions.SECTION);
        foreach (var (type, attribute) in services) s.Add(attribute.ToServiceDescriptor(type));

        s.AddTransient<Main_ViewModel>();

        return builder.Build();
    }

    private static readonly ConcurrentDictionary<Type, ILogger> loggers = new();
    public static ILogger GetLogger(Type type)
    {
        return loggers.GetOrAdd(type, CreateLogger);

        static ILogger CreateLogger(Type type)
        {
            var genericType = typeof(ILogger<>).MakeGenericType([type]);
            var loggerObj = CreateHost([]).Services.GetRequiredService(genericType);
            Console.WriteLine("Got logger: " + loggerObj.GetType().FullNameFormatted());
            var logger = (ILogger)loggerObj;
            return logger;
        }
    }

    public class AppOptions
    {
        public static readonly string SECTION = typeof(AppOptions).Namespace!;

        private static readonly ConcurrentDictionary<string, string> databaseFileCache = new();

        private string databaseFile;

        public required string DatabaseFile
        {
            get
            {
                var p = databaseFile.TrimOrNull();
                if (p == null) return p!;
                return databaseFileCache.GetOrAdd(p, path =>
                {
                    // https://johnkoerner.com/csharp/special-folder-values-on-windows-versus-mac/
                    // changed for .net8   https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/8.0/getfolderpath-unix

                    foreach (var specialFolder in Enum.GetValues<Environment.SpecialFolder>())
                    {
                        var name = nameof(Environment.SpecialFolder) + "." + specialFolder;
                        if (!path.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;

                        path = path.RemoveLeft(name.Length);
                        path = path.TrimStart([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

                        var dir = Path.GetFullPath(Environment.GetFolderPath(specialFolder));
                        dir = dir.TrimEnd([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
                        path = Path.GetFullPath(Path.Combine(dir, path));
                    }

                    var file = new FileInfo(path);
                    if (!file.Exists) Directory.CreateDirectory(file.DirectoryName!);
                    return file.FullName;
                });
            }

            [MemberNotNull(nameof(databaseFile))] set => databaseFile = value;
        }
    }

    public class Services
    {
        public class LogItem(int index, LogEvent logEvent)
        {
            public int Index { get; } = index;

            public string CategoryName => logEvent.CategoryName;
            public LogLevel LogLevel => logEvent.LogLevel;
            public DateTimeOffset Timestamp => logEvent.Timestamp;
            public string Message => logEvent.Text;
            public Exception? Exception => logEvent.Exception;
        }

        public interface ILogItemCollection
        {
            public ObservableCollection<LogItem> Logs { get; }
        }

        [Service<ILogItemCollection>(ServiceLifetime.Singleton)]
        public class LogItemCollection : ILogItemCollection
        {
            public ObservableCollection<LogItem> Logs { get; } = [];
        }

        [Service<ILoggerForwarderHandler>(ServiceLifetime.Singleton)]
        public class LogEventHandler(ILogItemCollection logItemCollection) : ILoggerForwarderHandler
        {
            private int indexCounter;

            public void AddLogEvent(LogEvent logEvent)
            {
                //Console.WriteLine("Get LogItem");
                logItemCollection.Logs.Add(new(Interlocked.Increment(ref indexCounter), logEvent));
            }
        }


        public interface IStorageService
        {
            public DirectoryInfo ScanDirectory { get; set; }
        }

        [Service<IStorageService>(ServiceLifetime.Singleton)]
        public class StorageService : IStorageService
        {
            private readonly ILogger log;
            private readonly LiteDatabase db;

            public StorageService(ILogger<StorageService> log, IOptions<AppOptions> options)
            {
                this.log = log;

                log.LogDebug("Initializing {Type}", GetType().NameFormatted());
                var dbFile = new FileInfo(options.Value.DatabaseFile);
                log.LogDebug("  {Message}: {File}",
                    dbFile.Exists ? "using existing db file" : "creating db to save data to", dbFile.FullName);

                var connectionString = $"Filename={dbFile.FullName};Connection=direct";
                log.LogDebug("  " + nameof(LiteDatabase) + ": {ConnectionString}", connectionString);
                db = new(connectionString);

                log.LogDebug("  COMPLETE");

                log.LogInformation("Using database file: {File}", dbFile.FullName);
            }

            private string? GetSetting(string name)
            {
                log.LogTraceMethod(new(name), "");
                name = name.ToLower();
                var os = db.GetCollection<Setting>();
                var o = os.FindOne(x => x.Name.ToLower() == name);
                return o?.Value;
            }

            private void SetSetting(string name, string? value)
            {
                log.LogTraceMethod(new(name, value), "");
                var os = db.GetCollection<Setting>();
                var o = os.FindOne(x => x.Name.ToLower() == name);
                if (o == null)
                {
                    o = new() { Name = name, Value = value };
                    os.Insert(o);
                }
                else
                {
                    o.Value = value;
                    os.Update(o);
                }
            }

            public DirectoryInfo ScanDirectory
            {
                get => Util.PathDirectoryOrParentDirectory(GetSetting(nameof(ScanDirectory)).TrimOrNull())
                       ?? Util.PathDirectoryOrParentDirectory(
                           Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                       ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                set => SetSetting(nameof(ScanDirectory), value.FullName);
            }
        }

        public interface IItem { }

        public class Setting : IItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? Value { get; set; }
        }

    }

    /*
        {
         "Logging": {
           "LogLevel": {
             "Default": "Trace",
             "Microsoft": "Warning",
             "Microsoft.Hosting.Lifetime": "Information"
           },
           "Console": {
             "LogLevel": {
               "Default": "Trace",
               "Microsoft": "Warning",
               "Microsoft.Hosting.Lifetime": "Information",
               "FileSystemManager": "Trace"
             },
             "FormatterName": "systemd",
             "FormatterOptions": {
               "IncludeScopes": true,
               "TimestampFormat": "HH:mm:ss ",
               "UseUtcTimestamp": false
             }
           }
         },

         "FileSystemManager": {
           "DatabaseFile": "SpecialFolder.LocalApplicationData/FileSystemManager/FileSystemManager.litedb"
         }
       }

     */
}
