using System.ComponentModel;
using System.IO.Hashing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FileSystemManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FileSystemManager.Views;

public partial class MainWindow : WindowBase<MainWindow_ViewModel>
{
    public override bool IsPrimary => true;

    public MainWindow()
    {
        InitializeComponent();
    }




    private void Menu_File_Exit_Click(object? sender, RoutedEventArgs args)
    {
        log.LogInformationMethod(new(sender, args), "Closing");
        Close();
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
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(DataContextTyped.ScanDirectory),
        });

        var dir = dirs.OrEmpty().Select(o => o.TryGetLocalPath()).FirstOrDefault(o => !string.IsNullOrWhiteSpace(o));
        if (dir == null)
        {
            log.LogDebug($"No directory selected");
            return;
        }

        dir = Path.GetFullPath(dir);
        if (!Directory.Exists(dir))
        {
            log.LogDebug("Selected directory does not exist: {ScanDirectory}", dir);
            Dir_Scan_Button!.IsEnabled = false;
            return;
        }

        DataContextTyped.ScanDirectory = dir;
        log.LogDebug("Selected directory: {ScanDirectory}", dir);

        Dir_Path_TextBox!.Text = dir;
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
        if (!Dir_Scan_Button.IsEnabled) return;
        var dir = DataContextTyped.ScanDirectory;
        Dir_Path_TextBox!.Text = dir;

        log.LogInformation("Attempting scan of directory: {Directory}", dir);
        if (!Directory.Exists(dir))
        {
            log.LogInformation("Selected directory does not exist: {ScanDirectory}", dir);
            return;
        }

        var s = services.Storage;

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        await s.Cache(new(dir), cancellationToken);

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

    private void Log_CheckBox_Click(object? sender, RoutedEventArgs args)
    {
        var isChecked = Log_CheckBox.IsChecked;
        log.LogInformationMethod(new(sender, args), "Is checked: {IsChecked}", isChecked == null ? "null" : isChecked.Value.ToString());
        if (isChecked == null) return;

        var logWindow = services.LogWindow;
        if (isChecked.Value)
        {
            log.LogDebugMethod(new(sender, args), "Showing log window");
            var posThis = Position;
            logWindow.Position = new(posThis.X + 32, posThis.Y - 32);
            logWindow.Show();
            Activate();
        }
        else
        {
            log.LogDebugMethod(new(sender, args), "Hiding log window");
            logWindow.Hide();
            Activate();
        }
    }


}
