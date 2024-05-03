using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FileSystemManager.Views;

public partial class Main_Menu : UserControlBase
{
    public Main_Menu()
    {
        InitializeComponent();
    }

    private void File_Exit_Click(object? sender, RoutedEventArgs args)
    {
        //log.LogTraceMethod(new(sender, args), "Started");
        //Close();
        //log.LogTraceMethod(new(sender, args), "Complete");
    }
}
