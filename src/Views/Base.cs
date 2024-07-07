using Avalonia.Controls;
using FileSystemManager.ViewModels;

namespace FileSystemManager.Views;

public interface IDataContext<out T> : IDataContext where T : ViewModelBase
{
    public T DataContextTyped { get; }
}

public interface IDataContext
{
    public object? DataContext { get; set; }
    public Type DataContextType { get; }
}

public abstract class ViewBase<T> : ViewBase, IDataContext<T> where T : ViewModelBase
{
    public override Type DataContextType => typeof(T);
    public T DataContextTyped => (T)DataContext.CheckNotNull();
}

public abstract class ViewBase : UserControl, IDataContext
{
    protected readonly ServicesRepository services;
    protected readonly ILogger log;

    public abstract Type DataContextType { get; }

    protected ViewBase()
    {
        services = Program.Services;
        log = services.GetLogger(GetType());
    }
}

public abstract class WindowBase<T> : WindowBase, IDataContext<T> where T : ViewModelBase
{
    public override Type DataContextType => typeof(T);
    public T DataContextTyped => (T)DataContext.CheckNotNull();
}

public abstract class WindowBase : Window, IDataContext
{
    public virtual bool IsPrimary => false;

    protected readonly ServicesRepository services;
    protected readonly ILogger log;

    public abstract Type DataContextType { get; }

    protected WindowBase()
    {
        services = Program.Services;
        log = services.GetLogger(GetType());
        Closing += HandleClosing;
    }

    private void HandleClosing(object? sender, WindowClosingEventArgs args)
    {
        if (IsPrimary)
        {
            HandleClosingPrimary(sender, args);
        }
        else
        {
            HandleClosingSecondary(sender, args);
        }
    }

    private void HandleClosingPrimary(object? sender, WindowClosingEventArgs args)
    {
        var windowsNonPrimary = Program.Services.Windows.Where(o => !o.IsPrimary).ToArray();
        log.LogDebug("Closing {Count} child windows", windowsNonPrimary.Length);
        foreach (var windowNonPrimary in windowsNonPrimary)
        {
            windowNonPrimary.Close();
        }
    }

    private void HandleClosingSecondary(object? sender, WindowClosingEventArgs args)
    {
        if (sender == null)
        {
            log.LogWarningMethod(new(sender, args), "Expecting '" + nameof(sender) + "' to be not null");
            return;
        }

        if (sender is not Window w)
        {
            log.LogErrorMethod(new(sender, args), "Expecting '" + nameof(sender) + "' to be of type {TypeWindow} but it was instead {TypeSender}", typeof(Window).FullNameFormatted(), sender.GetType().FullNameFormatted());
            return;
        }

        HandleClosingSecondary(w, args);
    }

    private void HandleClosingSecondary(Window w, WindowClosingEventArgs args)
    {
        if (args.IsProgrammatic)
        {
            log.LogDebug("Closing window: {WindowType}", w.GetType().FullNameFormatted());
        }
        else
        {
            log.LogDebug("Hiding window: {WindowType}", w.GetType().FullNameFormatted());
            w.Hide();
            args.Cancel = true;
        }
    }

}
