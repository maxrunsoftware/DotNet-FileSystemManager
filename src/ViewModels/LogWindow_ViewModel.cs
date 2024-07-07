using System.Collections.ObjectModel;
using FileSystemManager.Services;
using FileSystemManager.Views;

namespace FileSystemManager.ViewModels;

public class LogWindow_ViewModel : ViewModelBase
{
    public ObservableCollection<LogItem> Logs => services.LogItemCollection.Logs;
}
