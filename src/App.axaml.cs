using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FileSystemManager.ViewModels;
using FileSystemManager.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.EventLog;
using WindowBase = FileSystemManager.Views.WindowBase;

namespace FileSystemManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        //if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new Main { DataContext = new Main_ViewModel(null, null), };

        // If you use CommunityToolkit, line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var vm = Program.Services.GetViewModel<MainWindow_ViewModel>();

        var primaryWindows = Program.Services.Windows.Where(o => o.IsPrimary).ToArray();
        if (primaryWindows.Length < 1) throw new ApplicationException($"No {nameof(WindowBase)}.{nameof(WindowBase.IsPrimary)}=true defined in assembly");
        if (primaryWindows.Length > 1) throw new ApplicationException($"Multiple {nameof(WindowBase)}.{nameof(WindowBase.IsPrimary)}=true defined in assembly: " + primaryWindows.Select(o => o.GetType().FullNameFormatted()).ToStringDelimited(", "));
        var primaryWindow = primaryWindows[0];

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            primaryWindow.DataContext = vm;
            desktop.MainWindow = primaryWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            primaryWindow.DataContext = vm;
            singleViewPlatform.MainView = primaryWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
