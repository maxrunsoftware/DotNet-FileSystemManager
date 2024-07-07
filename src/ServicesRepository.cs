using FileSystemManager.Services;
using FileSystemManager.ViewModels;
using FileSystemManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FileSystemManager;

public class ServicesRepository(IServiceProvider serviceProvider)
{
    #region Items

    public MainWindow MainWindow => GetWindow<MainWindow>();
    public MainWindow_ViewModel MainWindow_ViewModel => GetViewModel<MainWindow_ViewModel>();

    public LogWindow LogWindow => GetWindow<LogWindow>();
    public LogWindow_ViewModel LogWindow_ViewModel => GetViewModel<LogWindow_ViewModel>();

    #endregion Items

    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ILogItemCollection LogItemCollection => ServiceProvider.GetRequiredService<ILogItemCollection>();
    public ISettings Settings => ServiceProvider.GetRequiredService<ISettings>();
    public IStorage Storage => ServiceProvider.GetRequiredService<IStorage>();
    public ILogger GetLogger(Type type) => ServiceProvider.GetLogger(type);
    public ILogger<T> GetLogger<T>() => ServiceProvider.GetRequiredService<ILogger<T>>();

    #region Window

    public static ImmutableArray<Type> WindowTypes { get; } = typeof(ServicesRepository).Assembly.GetTypesOf<WindowBase>().ToImmutableArray();

    public T GetWindow<T>() where T : WindowBase => (T)GetWindow(typeof(T));

    public WindowBase GetWindow(Type type) => Windows.First(o => o.GetType().IsAssignableTo(type));

    public IEnumerable<WindowBase> Windows => WindowTypes.Select(GetWindowInternal);

    private WindowBase GetWindowInternal(Type type)
    {
        var w = (WindowBase)ServiceProvider.GetRequiredService(type);
        w.DataContext ??= GetViewModel(w.DataContextType);
        return w;
    }

    public WindowBase GetWindow(ViewModelBase viewModelBase)
    {
        var viewModelBaseType = viewModelBase.GetType();
        foreach (var o in Windows)
        {
            if (o.DataContextType == viewModelBaseType) return o;
        }

        throw new NotImplementedException("No window found that supports view model of type " + viewModelBaseType.FullNameFormatted());
    }

    #endregion Window

    #region View

    public static ImmutableArray<Type> ViewTypes { get; } = typeof(ServicesRepository).Assembly.GetTypesOf<ViewBase>().ToImmutableArray();

    public T GetView<T>() where T : ViewBase => (T)GetView(typeof(T));

    public ViewBase GetView(Type type) => Views.First(o => o.GetType().IsAssignableTo(type));

    public IEnumerable<ViewBase> Views => ViewTypes.Select(GetViewInternal);

    private ViewBase GetViewInternal(Type type)
    {
        var w = (ViewBase)ServiceProvider.GetRequiredService(type);
        w.DataContext ??= GetViewModel(w.DataContextType);
        return w;
    }

    public ViewBase GetView(ViewModelBase viewModelBase)
    {
        var viewModelBaseType = viewModelBase.GetType();
        foreach (var o in Views)
        {
            if (o.DataContextType == viewModelBaseType) return o;
        }

        throw new NotImplementedException("No view found that supports view model of type " + viewModelBaseType.FullNameFormatted());
    }

    #endregion View

    #region ViewModel

    public static ImmutableArray<Type> ViewModelTypes { get; } = typeof(ServicesRepository).Assembly.GetTypesOf<ViewModelBase>().ToImmutableArray();

    public T GetViewModel<T>() where T : ViewModelBase => ServiceProvider.GetRequiredService<T>();

    public ViewModelBase GetViewModel(Type type) => (ViewModelBase)ServiceProvider.GetRequiredService(type);

    public IEnumerable<ViewModelBase> ViewModels => ViewModelTypes.Select(GetViewModelInternal);

    private ViewModelBase GetViewModelInternal(Type type)
    {
        var w = (ViewModelBase)ServiceProvider.GetRequiredService(type);
        // w.DataContext ??= GetViewModel(w.DataContextType);
        return w;
    }

    public ViewModelBase GetViewModel(ViewBase view) => GetViewModel(view.DataContextType);

    public ViewModelBase GetViewModel(WindowBase window) => GetViewModel(window.DataContextType);

    #endregion ViewModel

}
