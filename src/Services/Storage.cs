using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileSystemManager.Services;

public interface IStorage
{
    public Task Cache(DirectoryInfo info, CancellationToken cancellationToken);
}

[Service<IStorage>(ServiceLifetime.Singleton)]
public class Storage : IStorage
{
    private static readonly StringComparer SC = Constant.Path_StringComparer;
    private readonly IOptions<AppOptions> options;
    public AppOptions Options => options.Value;

    private readonly ILogger log;

    public Storage(IOptions<AppOptions> options, ILoggerFactory loggerFactory)
    {
        this.options = options;
        log = loggerFactory.CreateLogger(GetType());
    }




    private ImmutableArray<StorageDir> cacheDirs = [];
    private readonly object cacheDirsLock = new();


    public async Task Cache(DirectoryInfo info, CancellationToken cancellationToken) => await Task.Run(() => Cache_Internal(info, cancellationToken), cancellationToken).ConfigureAwait(false);

    private void Cache_Internal(DirectoryInfo info, CancellationToken cancellationToken)
    {
        var dir = new StorageDir(info);
        if (!dir.Path.IsValid)
        {
            log.LogError(dir.Exception, "Error reading directory: {Dir}", info.Name);
            return;
        }

        // time intensive, so do it outside lock
        dir.Cache(cancellationToken);

        lock (cacheDirsLock)
        {
            var cDirsOld = cacheDirs;
            var cDirsNew = new Dictionary<StoragePath, StorageDir>();

            // remove entries that we are re-caching
            foreach (var cDirOld in cDirsOld)
            {
                if (cDirOld.Equals(dir) || cDirOld.Path.IsChildOf(dir.Path))
                {
                    log.LogTrace("Removing " + nameof(cacheDirs) + " entry: {Dir}", cDirOld.Path);
                }
                else
                {
                    log.LogTrace("Keeping  " + nameof(cacheDirs) + " entry: {Dir}", cDirOld.Path);
                    cDirsNew.TryAdd(cDirOld.Path, cDirOld);
                }
            }

            // if we are not cancelled then go ahead with adding our new entries
            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var cDirNew in dir.GetDirsAll())
                {
                    cDirsNew.TryAdd(cDirNew.Path, cDirNew);
                }
            }

            // finally update cache
            cacheDirs = [..cDirsNew.Values];


        }
    }
}
