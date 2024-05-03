using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace FileSystemManager;

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
