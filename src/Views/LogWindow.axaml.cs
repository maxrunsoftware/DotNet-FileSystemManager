using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FileSystemManager.ViewModels;

namespace FileSystemManager.Views;

public partial class LogWindow : WindowBase<LogWindow_ViewModel>
{
    public LogWindow()
    {
        InitializeComponent();
        DataGrid.Loaded += (sender, args) => SortRows();
        SizeChanged += HandleSizeChanged;
    }

    protected override void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.IsVisibleChanged(e);
        if (e.NewValue is bool b)
        {
            services.MainWindow.Log_CheckBox.IsChecked = b;
        }
    }

    private void HandleSizeChanged(object? sender, SizeChangedEventArgs args)
    {
        log.LogDebugMethod(new(sender, args), "Size changed");
        var itemsSource = DataGrid.ItemsSource;
        if (itemsSource != null)
        {
            var items = itemsSource.Cast<object?>().ToList();
            log.LogInformation("Rows: {Count}", items.Count);
        }

        log.LogInformation(nameof(DataContextTyped) + ": {DataContextTyped}", DataContextTyped.GetType().NameFormatted());
    }

    private void SortRows()
    {
        log.LogInformation("Trying to sort");
        var c = DataGrid.Columns.FirstOrDefault(o => o.DisplayIndex == 0);
        if (c != null)
        {
            c.Sort(ListSortDirection.Descending);
        }
    }
}
