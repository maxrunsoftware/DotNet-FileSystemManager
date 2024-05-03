using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileSystemManager.Views;

public partial class Main_Log : UserControl
{
    public Main_Log()
    {
        InitializeComponent();
        DataGrid.Loaded += (sender, args) => SortRows();
    }

    private void SortRows()
    {
        Program.GetLogger(GetType()).LogInformation("Trying to sort");
        var c = DataGrid.Columns.FirstOrDefault(o => o.DisplayIndex == 0);
        if (c != null)
        {

            c.Sort(ListSortDirection.Descending);
        }
    }


}
