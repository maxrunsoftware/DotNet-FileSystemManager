using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FileSystemManager;

public class UserControlBase : UserControl
{
    protected ILogger log => Program.GetLogger(GetType());
}
