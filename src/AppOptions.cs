using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace FileSystemManager;

[Options("FileSystemManager")]
public class AppOptions
{
    private string? settingsFile;
    private volatile bool settingsFileDirty = true;

    public string? SettingsFile
    {
        get
        {
            if (settingsFileDirty || string.IsNullOrWhiteSpace(settingsFile))
            {
                var path = settingsFile;
                if (string.IsNullOrWhiteSpace(path)) path = "SpecialFolder.LocalApplicationData/MaxRunSoftware/FileSystemManager.xml";

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
                settingsFile = file.FullName;
                settingsFileDirty = false;
            }

            return settingsFile;
        }
        set
        {
            settingsFile = value;
            settingsFileDirty = true;
        }
    }






}
