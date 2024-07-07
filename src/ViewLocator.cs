using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FileSystemManager.ViewModels;
using FileSystemManager.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FileSystemManager;

public class ViewLocator : IDataTemplate
{
    protected readonly ILogger log;

    public ViewLocator()
    {
        log = Program.Services.GetLogger(GetType());
    }

    public Control? Build(object? data)
    {
        if (data == null)
        {
            log.LogWarningMethod(new(data), nameof(data) + " was null");
            return new TextBlock { Text = $"No ViewModel provided ({nameof(data)}=null)" };
        }
        var viewModel = data as ViewModelBase;
        if (viewModel == null)
        {

            log.LogWarningMethod(new(data), nameof(data) + " was {TypeData} but was expecting {TypeViewModelBase}", data.GetType().FullNameFormatted(), typeof(ViewModelBase).FullNameFormatted());
            return new TextBlock { Text = $"Provided {nameof(data)}={data.GetType().FullNameFormatted()} is not {typeof(ViewModelBase).FullNameFormatted()}"};
        }

        var view = Program.Services.GetView(viewModel);
        if (view.DataContext != null)
        {
            if (view.DataContext != viewModel)
                throw new ApplicationException(string.Format(
                    "{0}.{1}={2} but received request to assign {3}",
                    view.GetType().FullNameFormatted(),
                    nameof(IDataContext.DataContext),
                    view.DataContext.GetType().NameFormatted(),
                    viewModel.GetType().NameFormatted()
                ));
        }
        else
        {
            view.DataContext = data;
        }
        return view;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
