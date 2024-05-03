using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace FileSystemManager.Views;


public partial class Main_Dir : UserControlBase
{
    private static IStorageService StorageService => Program.StorageService;

    public Main_Dir()
    {
        InitializeComponent();
    }

        private async void Dir_Browse_Button_Click(object? sender, RoutedEventArgs args)
    {
        log.LogInformationMethod(new(sender, args), "clicked");

        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this)!;

        // Start async operation to open the dialog.
        var dirs = await topLevel.StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Scan Directory",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(StorageService.ScanDirectory.FullName),
        });

        var dir = dirs.Select(o => o.TryGetLocalPath()).TrimOrNull().WhereNotNull().FirstOrDefault();
        if (dir == null)
        {
            log.LogDebug($"No directory selected");
            return;
        }

        if (!Directory.Exists(dir))
        {
            log.LogDebug("Selected directory does not exist: {ScanDirectory}", dir);
            Dir_Scan_Button!.IsEnabled = false;
            return;
        }

        StorageService.ScanDirectory = new(dir);
        var dirInfo = StorageService.ScanDirectory;
        log.LogDebug("Selected directory: {ScanDirectory}", dirInfo.FullName);

        Dir_Path_TextBox!.Text = dirInfo.FullName;
        Dir_Scan_Button!.IsEnabled = true;

        /*
        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();
        }
        */
    }

    private async void Dir_Scan_Button_Click(object? sender, RoutedEventArgs args)
    {
        var dir = StorageService.ScanDirectory;
        Dir_Path_TextBox!.Text = dir.FullName;

        log.LogInformation("Attempting scan of directory: {Directory}", dir.FullName);
        if (!dir.Exists)
        {
            log.LogInformation("Selected directory does not exist: {ScanDirectory}", dir);
            return;
        }

        //Config.Instance.FileSystemCache.Clear();
        //Config.Instance.FileSystemCache.Add(dir, recursive: true);

        //log.LogInformation("Cached files: {FileCount}", Config.Instance.FileSystemCache.FileCount);

        /*
        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();
        }
        */
    }
}
