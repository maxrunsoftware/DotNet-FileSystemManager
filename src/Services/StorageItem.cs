using System.Security;
using System.Text.RegularExpressions;

namespace FileSystemManager.Services;

public interface IStorageItem
{
    public string Name { get; }
    public StoragePath Path { get; }
    public Exception? Exception { get; }
}

public abstract class StorageItem : IStorageItem
{
    public string Name { get; }
    public StoragePath Path { get; }
    public Exception? Exception { get; protected set; }

    protected StorageItem(FileSystemInfo info)
    {
        Name = info.Name;
        try
        {
            Path = new(info);
        }
        catch (Exception e)
        {
            Path = StoragePath.EMPTY;
            Exception = e;
        }
    }
}

public abstract class StorageItem<T>(FileSystemInfo info) : StorageItem(info) where T : StorageItem
{
    private static ILogger<T>? logInstance;
    protected static ILogger log => logInstance ??= Program.Services.GetLogger<T>();
}

public interface IStorageDir : IStorageItem
{
    public IReadOnlyCollection<IStorageDir> Dirs { get; }
    public IReadOnlyCollection<IStorageFile> Files { get; }
    public Exception? ChildrenException { get; }
}

public sealed class StorageDir(DirectoryInfo info) : StorageItem<StorageDir>(info), IStorageDir
{

    private record Listing(StorageDir[] Dirs, StorageFile[] Files);

    private Listing? listing;

    private Listing CacheListing => listing ??= GetListing();

    public IReadOnlyCollection<StorageDir> Dirs => CacheListing.Dirs;
    IReadOnlyCollection<IStorageDir> IStorageDir.Dirs => Dirs;

    public IReadOnlyCollection<StorageFile> Files => CacheListing.Files;
    IReadOnlyCollection<IStorageFile> IStorageDir.Files => Files;

    public Exception? ChildrenException { get; set; }

    public void Cache(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        foreach (var dir in Dirs)
        {
            dir.Cache(cancellationToken);
        }
    }

    public IReadOnlyCollection<StorageDir> GetDirsAll()
    {
        var d = new Dictionary<StoragePath, StorageDir>();
        GetDirsAll(d);
        return d.Values;
    }

    private void GetDirsAll(Dictionary<StoragePath, StorageDir> d)
    {
        if (!d.TryAdd(Path, this)) return;
        foreach (var dd in Dirs)
        {
            dd.GetDirsAll(d);
        }
    }

    private Listing GetListing()
    {
        if (Exception != null) return new([], []);

        List<StorageDir> dirs = [];
        List<StorageFile> files = [];

        try
        {
            var info = new DirectoryInfo(Path.PathRaw);
            var fsos = info.GetFileSystemInfos();
            log.LogTrace("Found [{Count}] children of directory: {Dir}", fsos.Length, Path);

            foreach (var fso in fsos)
            {
                switch (fso)
                {
                    case FileInfo fi:
                        var sf = new StorageFile(fi);
                        if (sf.Path.IsChildOf(Path)) files.Add(sf);
                        break;
                    case DirectoryInfo di:
                        var sd = new StorageDir(di);
                        if (sd.Path.IsChildOf(Path)) dirs.Add(sd);
                        break;
                    default:
                        throw new NotImplementedException(fso.ToString());
                }
            }
        }
        catch (IOException ioe)
        {
            log.LogWarning(ioe, "Error trying to read directory [{Type}]: {Dir}", ioe.GetType().NameFormatted(), Path);
            ChildrenException = ioe;
        }
        catch (SecurityException se)
        {
            log.LogWarning(se, "Error trying to read directory [{Type}]: {Dir}", se.GetType().NameFormatted(), Path);
            ChildrenException = se;
        }

        return new(dirs.Count == 0 ? [] : dirs.ToArray(), files.Count == 0 ? [] : files.ToArray());
    }
}

public interface IStorageFile : IStorageItem
{
    public long Size { get; }
    public string Ext { get; }
    public string? Hash { get; }
    public string? HashType { get; }
}

public class StorageFile : StorageItem<StorageFile>, IStorageFile
{

    public long Size { get; }
    public string Ext { get; }
    public string? Hash { get; private set; }
    public string? HashType { get; private set; }



    public StorageFile(FileInfo info) : base(info)
    {
        if (Exception != null)
        {
            Size = -1L;
            Ext = string.Empty;
            return;
        }

        Ext = info.Extension;


        var size = -1L;
        try
        {
            size = info.GetLength();
        }
        catch (IOException ioe)
        {
            Exception = ioe;

        }
        catch (SecurityException se)
        {
            Exception = se;
        }

        Size = size;
        if (Exception != null)
        {
            log.LogWarning(Exception, "Could not read size of file: {File}", Path);
        }



        var tokens = StorageFileNameParser.Parse(Name);
        var hashInfo = tokens.Select(StorageFileHasher.ReadHash).WhereNotNull().FirstOrDefault();
        if (hashInfo != null)
        {
            Hash = hashInfo.Hash;
            HashType = hashInfo.Hasher.Name;
        }



    }

}

public static partial class StorageFileNameParser
{
    public static string[] Parse(string name)
    {
        name = name.Trim();
        if (name.Length == 0) return [];
        var regex = FileNameTokenRegex();
        return regex.MatchAll(name).Select(o => o.TrimStart('[').TrimEnd(']')).Where(o => o.Length > 0).ToArray();
    }

    [GeneratedRegex(@"\[([a-zA-Z0-9]*)\]", RegexOptions.Compiled | RegexOptions.ECMAScript)]
    private static partial Regex FileNameTokenRegex();
}
