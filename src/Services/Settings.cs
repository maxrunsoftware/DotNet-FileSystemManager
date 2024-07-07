using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileSystemManager.Services;

public interface ISettings
{
    public DirectoryInfo ScanDirectory { get; set; }
}

[Service<ISettings>(ServiceLifetime.Singleton)]
public class Settings(IOptions<AppOptions> options, ILogger<Settings> log) : ISettings
{

    private SettingsFile GetSettingsFile() => new(options.Value.SettingsFile!, log);

    public DirectoryInfo ScanDirectory
    {
        get
        {
            log.LogDebugMethod(new(), string.Empty);
            var s = GetSettingsFile();
            var xml = s.GetSetting(nameof(ScanDirectory));
            if (string.IsNullOrWhiteSpace(xml.Value))
            {
                var d = GetDefaultScanDirectory();
                xml.Value = d.FullName;
                s.Save();
                return d;
            }

            try
            {
                var d = new DirectoryInfo(xml.Value);
                var dd = Util.PathDirectoryOrParentDirectory(d.FullName);
                if (dd != null)
                {
                    if (d.FullName != dd.FullName)
                    {
                        xml.Value = dd.FullName;
                        s.Save();
                    }
                    return dd;
                }
            }
            catch (Exception)
            {
                /* ignore */
            }

            var ddd = GetDefaultScanDirectory();
            xml.Value = ddd.FullName;
            s.Save();
            return ddd;

            static DirectoryInfo GetDefaultScanDirectory()
            {
                var d = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var dd = Util.PathDirectoryOrParentDirectory(d);
                if (dd == null) dd = new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if (dd.Exists) return dd;
                if (Constant.Path_Current_Directory != null)
                {
                    dd = new(Constant.Path_Current_Directory);
                    if (dd.Exists) return dd;
                }

                dd = new(".");
                return dd;
            }
        }

        set
        {
            log.LogDebugMethod(new(value.FullName), string.Empty);
            var path = value.FullName; // get path early in case we throw exception

            var s = GetSettingsFile();
            var xml = s.GetSetting(nameof(ScanDirectory));
            xml.Value = path;
            s.Save();
        }
    }
}

public class SettingsFile
{
    private static readonly string XML_ROOT = "MaxRunSoftware";
    private static readonly string XML_APP = "FileSystemManager";
    private static readonly string XML_SETTINGS = "Settings";

    private readonly string path;
    private readonly ILogger log;

    public XmlElement Root { get; }
    public XmlElement App { get; }
    public XmlElement Settings { get; }

    public SettingsFile(string path, ILogger log)
    {
        this.log = log;
        this.path = path;
        if (!File.Exists(path)) Util.FileWrite(path, new XmlElement(XML_ROOT).ToStringXml(), Encoding.UTF8);

        var xml = XmlElement.FromXml(Util.FileRead(path, Encoding.UTF8));
        if (!xml.Name.EqualsOrdinalIgnoreCase(XML_ROOT)) throw new ArgumentException($"Expecting root element <{XML_ROOT}> but was instead <{xml.Name}> in file: {path}", nameof(path));
        Root = xml;

        App = GetOrAddElementSingle(Root, XML_APP);
        Settings = GetOrAddElementSingle(App, XML_SETTINGS);
    }

    public XmlElement GetSetting(string name) => GetOrAddElementSingle(Settings, name);

    private XmlElement GetOrAddElementSingle(XmlElement parent, string childName)
    {
        XmlElement child;
        var items = parent.GetChildren(StringComparer.OrdinalIgnoreCase, childName).ToList();
        if (items.Count == 0)
        {
            child = new(childName);
            parent.Children.Add(child);
        }
        else
        {
            child = items.PopTail();
            foreach (var additional in items) parent.Children.Remove(additional); // if there additional elements remove them
        }

        return child;
    }

    public void Save()
    {
        log.LogDebugMethod(new(), "Saving to file: {File}", path);
        Util.FileWrite(path, Root.ToStringXml(), Encoding.UTF8);
    }
}
