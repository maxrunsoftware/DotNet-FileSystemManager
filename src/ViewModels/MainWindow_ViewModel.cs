using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DynamicData;
using FileSystemManager.Services;
using FileSystemManager.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using ReactiveUI;

namespace FileSystemManager.ViewModels;

public class MainWindow_ViewModel : ViewModelBase
{

    public ObservableCollection<Node> Nodes { get; }
    public ObservableCollection<Node> SelectedNodes { get; }

    public MainWindow_ViewModel()
    {
        SelectedNodes = [];
        Nodes =
        [
            new("Animals", [
                new("Mammals", [
                    new("Lion"),
                    new("Cat"),
                    new("Zebra")
                ]),
            ]),

            new("Birds", [
                new("Robin"),
                new("Condor"),
                new("Parrot"),
                new("Eagle")
            ]),

            new("Insects", [
                new("Locust"),
                new("House Fly"),
                new("Butterfly"),
                new("Moth")
            ])

        ];

        var moth = Nodes.Last().SubNodes?.Last();
        if (moth != null) SelectedNodes.Add(moth);

        SelectedNodes.CollectionChanged += OnSelectedNodesOnCollectionChanged;
    }

    private void OnSelectedNodesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        log.LogDebug("[{Sender}]: <{Action}>", sender == null ? "null" : sender.GetType().NameFormatted(), args.Action);
        log.LogDebug("  Old[{OldStartingIndex}]:", args.OldStartingIndex);
        foreach (var s in ParseItems(args.OldItems)) log.LogDebug("    {Item}", s);
        log.LogDebug("  New[{NewStartingIndex}]:", args.NewStartingIndex);
        foreach (var s in ParseItems(args.NewItems)) log.LogDebug("    {Item}", s);

        return;

        static List<string> ParseItems(IList? items)
        {
            if (items == null) return [];

            var list = new List<string>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    list.Add("null");
                    continue;
                }

                var sb = new StringBuilder();
                sb.Append($"[{item.GetType().NameFormatted()}] ");
                if (item is Node node)
                {
                    sb.Append(node.Title);
                }
                else
                {
                    sb.Append(item.ToString());
                }
                list.Add(sb.ToString());
            }

            return list;
        }
    }

    public string ScanDirectory
    {
        get
        {
            var o = services.Settings.ScanDirectory.FullName;
            Dir_Scan_Button_IsEnabled = Directory.Exists(o);
            return o;
        }
        set
        {
            var o = value;
            Dir_Scan_Button_IsEnabled = Directory.Exists(o);
            services.Settings.ScanDirectory = new(o);
        }
    }

    private bool dir_Scan_Button_IsEnabled;
    public bool Dir_Scan_Button_IsEnabled
    {
        get => dir_Scan_Button_IsEnabled;
        set => this.RaiseAndSetIfChanged(ref dir_Scan_Button_IsEnabled, value);
    }

}


public class Node
{
    public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }

    public Node(string title)
    {
        Title = title;
    }

    public Node(string title, ObservableCollection<Node> subNodes)
    {
        Title = title;
        SubNodes = subNodes;
    }
}
