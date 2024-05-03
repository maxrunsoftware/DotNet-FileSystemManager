using System.Collections.ObjectModel;

namespace FileSystemManager.ViewModels;

public class Main_ViewModel : ViewModelBase
{
    public ObservableCollection<LogItem> Logs => Program.LogItemCollection.Logs;
}
