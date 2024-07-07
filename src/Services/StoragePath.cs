namespace FileSystemManager.Services;

public sealed class StoragePath
{
    private static readonly StringComparer SC = Constant.Path_StringComparer;
    public static readonly StoragePath EMPTY = new();
    public static readonly StoragePathComp Comparer = StoragePathComp.Instance;

    public sealed class StoragePathComp : ComparerBaseClass<StoragePath>
    {

        public static readonly StoragePathComp Instance = new();
        private StoragePathComp() {}
        protected override bool EqualsInternal(StoragePath x, StoragePath y) => x.Parts.SequenceEqual(y.Parts, SC);

        protected override int GetHashCodeInternal(StoragePath obj) => SC.GetHashCode(obj.Path);

        protected override int CompareInternal(StoragePath x, StoragePath y) => CompareOrdinalIgnoreCaseThenOrdinal(x.Parts, y.Parts) ?? 0;
    }

    private static readonly char[] SEPS = [System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar];

    public string PathRaw { get; }
    public string Path { get; }
    public IReadOnlyList<string> Parts { get; }

    public bool IsValid => PathRaw == string.Empty;

    private StoragePath()
    {
        PathRaw = string.Empty;
        Path = string.Empty;
        Parts = ArraySegment<string>.Empty;
    }

    public StoragePath(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        PathRaw = path;
        if (!System.IO.Path.IsPathFullyQualified(path)) throw new ArgumentException($"Path is not fully qualified: {path}", nameof(path));
        //var pathRoot = Path.GetPathRoot(path);

        var parts = path.Split(SEPS, StringSplitOptions.None).ToList();
        while (parts.Count > 0 && string.IsNullOrEmpty(parts[0])) parts.PopHead();
        while (parts.Count > 0 && string.IsNullOrEmpty(parts[^1])) parts.PopTail();
        Parts = parts;

        Path = parts.ToStringDelimited(System.IO.Path.DirectorySeparatorChar);
    }

    public StoragePath(FileSystemInfo info) : this(info.FullName) { }

    public override string ToString() => PathRaw;

    public bool IsChildOf(StoragePath parent) => IsParentChild(parent, this);

    public bool IsParentOf(StoragePath child) => IsParentChild(this, child);

    private static bool IsParentChild(StoragePath parent, StoragePath child)
    {
        if (parent.Equals(child)) return false;
        if (parent.Parts.Count >= child.Parts.Count) return false;
        for (var i = 0; i < parent.Parts.Count; i++)
        {
            if (!SC.Equals(parent.Parts[i], child.Parts[i])) return false;
        }

        return true;
    }
}
