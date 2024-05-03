using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace FileSystemManager;

public class LogItem(int index, LogEvent logEvent)
{
    public int Index { get; } = index;

    public string CategoryName => logEvent.CategoryName;
    public LogLevel LogLevel => logEvent.LogLevel;
    public DateTimeOffset Timestamp => logEvent.Timestamp;
    public string Message => logEvent.Text;
    public Exception? Exception => logEvent.Exception;
}

public interface ILogItemCollection
{
    public ObservableCollection<LogItem> Logs { get; }
}

[Service<ILogItemCollection>(ServiceLifetime.Singleton)]
public class LogItemCollection : ILogItemCollection
{
    public ObservableCollection<LogItem> Logs { get; } = [];
}

[Service<ILoggerForwarderHandler>(ServiceLifetime.Singleton)]
public class LogEventHandler(ILogItemCollection logItemCollection) : ILoggerForwarderHandler
{
    private int indexCounter = 0;
    public void AddLogEvent(LogEvent logEvent)
    {
        //Console.WriteLine("Get LogItem");
        logItemCollection.Logs.Add(new(Interlocked.Increment(ref indexCounter), logEvent));
    }
}
