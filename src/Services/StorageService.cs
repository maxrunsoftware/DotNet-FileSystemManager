using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileSystemManager;

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
        log.LogDebug("  {Message}: {File}", dbFile.Exists ? "using existing db file": "creating db to save data to", dbFile.FullName);

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
            o = new() { Name = name, Value = value};
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
               ?? Util.PathDirectoryOrParentDirectory(Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
               ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        set => SetSetting(nameof(ScanDirectory), value.FullName);
    }
}
