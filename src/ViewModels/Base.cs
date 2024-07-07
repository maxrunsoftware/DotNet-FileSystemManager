using ReactiveUI;

namespace FileSystemManager.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    protected readonly ServicesRepository services;
    protected readonly ILogger log;

    protected ViewModelBase()
    {
        services = Program.Services;
        log = services.GetLogger(GetType());
    }
}
