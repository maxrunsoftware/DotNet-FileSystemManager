using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace FileSystemManager;

/*
public class PathResolver
{
    public static PathResolver Instance { get; } = new();

    private readonly ConcurrentDictionary<string, string?> cache = new();

    public string? Resolve(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : cache.GetOrAdd(path, Resolve_Internal);
    }

    private static string? Resolve_Internal(string path)
    {

    }
}


public class FileSystemItem
{
    public IFileSystemProvider FileSystemProvider { get; }
    public FileSystemInfo Info { get; private set; }
    public DateTimeOffset UtcRefreshedOn { get; private set; }
    public DateTimeOffset? UtcCreatedOn { get; private set; }
    public DateTimeOffset? UtcModifiedOn { get; private set; }
    public string? NameExtension { get; private set; }
    public long? Size { get; private set; }

    public string Name { get; private set; }

    public bool IsFile { get; private set; }
    public bool IsDirectory => !IsFile;
    public bool IsExists { get; private set; }
    public string? Path { get; private set; }

    private static readonly IReadOnlyCollection<Exception> exceptions_empty = Array.Empty<Exception>();
    private List<Exception>? exceptions;
    public IReadOnlyCollection<Exception> Exceptions => exceptions ?? exceptions_empty;


    protected FileSystemItem(IFileSystemProvider fileSystemProvider, FileSystemInfo info)
    {
        FileSystemProvider = fileSystemProvider;
        Info = info;
        UtcRefreshedOn = DateTimeOffset.UtcNow;
        Name = info.Name;

        try
        {
            Path = info.FullName;
            UtcCreatedOn = info.CreationTimeUtc;
            UtcModifiedOn = info.LastWriteTimeUtc;
        }
        catch (Exception e)
        {
            Exception = e;
        }
    }

    protected

    protected abstract DirectoryInfo? GetParent();
}


public class FileDetails
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public required string ParentPath { get; set; }
}

public interface IFileSystemDirectoryListing
{
    public FileSystemDirectory Directory { get; }
    public IReadOnlyCollection<FileSystemDirectory> Directories { get; }
    public IReadOnlyCollection<FileSystemFile> Files { get; }
    public Exception? Exception { get; }
}

public interface IFileSystemProvider
{   public IFileSystemDirectoryListing GetListing(FileSystemDirectory directory);
    public FileSystemDirectory GetDirectory(DirectoryInfo directory);
    public FileSystemFile GetFile(FileInfo file);
}

public static class FileSystemProviderExtensions
{
//    public static IReadOnlyCollection<FileSystemFile> GetFiles(this IFileSystemProvider provider, FileSystemDirectory directory) => provider.GetListing(directory).Files;
//    public static IReadOnlyCollection<FileSystemDirectory> GetDirectories(this IFileSystemProvider provider, FileSystemDirectory directory) => provider.GetListing(directory).Directories;
    public static FileSystemDirectory GetDirectory(this IFileSystemProvider provider, string directory) => provider.GetDirectory(new(directory));
    public static FileSystemFile GetFile(this IFileSystemProvider provider, string file) => provider.GetFile(new(file));
}

public class FileSystemProvider(ILogger<FileSystemProvider> log, IStorageService storage) : IFileSystemProvider
{
    private class CacheEntry : IFileSystemDirectoryListing
    {
        private static readonly IReadOnlyCollection<FileSystemFile> EMPTY_FILES = Array.Empty<FileSystemFile>();
        private static readonly IReadOnlyCollection<FileSystemDirectory> EMPTY_DIRECTORIES = Array.Empty<FileSystemDirectory>();

        public FileSystemDirectory Directory { get; }
        public List<FileSystemDirectory>? Directories { get; }
        public List<FileSystemFile>? Files { get; }
        public Exception? Exception { get; }

        IReadOnlyCollection<FileSystemDirectory> IFileSystemDirectoryListing.Directories => Directories ?? EMPTY_DIRECTORIES;
        IReadOnlyCollection<FileSystemFile> IFileSystemDirectoryListing.Files => Files ?? EMPTY_FILES;

        public CacheEntry(IFileSystemProvider fileSystemProvider, FileSystemDirectory directory)
        {
            Directory = directory;
            Exception? eEntry = null;
            Exception? eOverall = null;

            try
            {
                var entry = Directory.Info.GetFileSystemEntries(false).First();
                eEntry = entry.Exception;
                if (entry.Directories.Length > 0) Directories = entry.Directories.Select(fileSystemProvider.GetDirectory).ToList();
                if (entry.Files.Length > 0) Files = entry.Files.Select(fileSystemProvider.GetFile).ToList();
            }
            catch (Exception e)
            {
                eOverall = e;
            }

            Exception = eOverall ?? eEntry ?? null;
        }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> cacheEntries = new();
    private readonly ConcurrentDictionary<string, FileSystemDirectory> cacheDirectories = new();
    private readonly ConcurrentDictionary<string, FileSystemFile> cacheFiles = new();


    private CacheEntry? GetCacheEntry(FileSystemDirectory directory)
    {
        if (directory.Exception != null) return null;
        var path = directory.Path;
        if (string.IsNullOrEmpty(path)) return null;

        return cacheEntries.GetOrAdd(
            path,
            static (_, providerAndFso) => new(providerAndFso.Item1, providerAndFso.Item2),
            (this, directory)
        );
    }

    public (IReadOnlyCollection<FileSystemDirectory> Directories, IReadOnlyCollection<FileSystemFile> Files) GetListing(FileSystemDirectory directory)
    {
        var entry = GetCacheEntry(directory);
        var d = entry?.Directories ?? EMPTY_DIRECTORIES;
        var f = entry?.Files ?? EMPTY_FILES;
        return (d, f);
    }

    public FileSystemDirectory GetDirectory(DirectoryInfo directory) =>
        cacheDirectories.GetOrAdd(
            directory.FullName,
            static (_, providerAndInfo) => FileSystemDirectory.Create(providerAndInfo.Item1, providerAndInfo.Item2),
            (this, directory)
        );

    public FileSystemFile GetFile(FileInfo file) =>
        cacheFiles.GetOrAdd(
            file.FullName,
            static (_, providerAndInfo) => FileSystemFile.Create(providerAndInfo.Item1, providerAndInfo.Item2),
            (this, file)
        );
}


public abstract class FileSystemObjectBase<T> where T : FileSystemInfo
{
    public IFileSystemProvider FileSystemProvider { get; }
    public T Info { get; }
    public DateTimeOffset UtcRefreshedOn { get; set; }
    public string Name { get; set; }

    public string? Path { get; set; }
    public Exception? Exception { get; set; }
    public DateTimeOffset? UtcCreatedOn { get; set; }
    public DateTimeOffset? UtcModifiedOn { get; set; }

    protected FileSystemObjectBase(IFileSystemProvider fileSystemProvider, T info)
    {
        FileSystemProvider = fileSystemProvider;
        Info = info;
        UtcRefreshedOn = DateTimeOffset.UtcNow;
        Name = info.Name;

        try
        {

            Path = info.FullName;
            UtcCreatedOn = info.CreationTimeUtc;
            UtcModifiedOn = info.LastWriteTimeUtc;
        }
        catch (Exception e)
        {
            Exception = e;
        }
    }

    protected abstract DirectoryInfo? GetParent();
}

public class FileSystemDirectory : FileSystemObjectBase<DirectoryInfo>
{
    public FileSystemDirectory(IFileSystemProvider fileSystemProvider, DirectoryInfo info) : base(fileSystemProvider, info) { }

    public static FileSystemDirectory Create(IFileSystemProvider fileSystemProvider, DirectoryInfo info) => new(fileSystemProvider, info);

    protected override DirectoryInfo? GetParent()
    {
        return Info.Parent;
    }
}

public class FileSystemFile : FileSystemObjectBase<FileInfo>
{
    public string Extension { get; }
    public long? Length { get; set; } = null;

    public FileSystemFile(IFileSystemProvider fileSystemProvider, FileInfo info) : base(fileSystemProvider, info)
    {
        var extension = Info.Extension;
        Extension = extension == Name ? string.Empty : extension;

        if (Exception != null)
        {
            Length = null;
            try
            {
                Length = Info.GetLength();
            }
            catch (Exception _)
            {
                try
                {
                    Length = Info.Length;
                }
                catch (Exception ee)
                {
                    Exception = ee;
                }
            }
        }
    }

    public static FileSystemFile Create(IFileSystemProvider fileSystemProvider, FileInfo info) => new(fileSystemProvider, info);
    protected override DirectoryInfo? GetParent()
    {
        Info.Directory;
    }
}
*/
