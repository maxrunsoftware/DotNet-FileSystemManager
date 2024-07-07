using System.Collections.Frozen;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FileSystemManager.Services;

public class StorageFileHasherResult
{
    public required StoragePath Path { get; init; }
    public required StorageFileHasher Hasher { get; init; }

    public Exception? Exception { get; set; }
    public DateTimeOffset? StartedOnUtc { get; set; }
    public DateTimeOffset? CompletedOnUtc { get; set; }
    public byte[]? Hash { get; set; }
}

public record StorageFileHasherReadResult(StorageFileHasher Hasher, string Hash);

public abstract partial class StorageFileHasher(char key, string name)
{
    public static FrozenDictionary<char, StorageFileHasher> Hashers_ByKey { get; } = new List<StorageFileHasher>
    {
        new HasherCrypto('G', nameof(MD5), MD5.Create),
        new HasherCrypto('H', nameof(SHA1), SHA1.Create),
        new HasherCrypto('I', nameof(SHA256), SHA256.Create),
        new HasherCrypto('J', nameof(SHA384), SHA384.Create),
        new HasherCrypto('K', nameof(SHA512), SHA512.Create),
        new HasherNonCrypto('L', nameof(Crc32), () => new Crc32()),
        new HasherNonCrypto('M', nameof(Crc64), () => new Crc64()),
        new HasherNonCrypto('N', nameof(XxHash32), () => new XxHash32()),
        new HasherNonCrypto('O', nameof(XxHash64), () => new XxHash64()),
        new HasherNonCrypto('P', nameof(XxHash128), () => new XxHash128()),
        new HasherNonCrypto('Q', nameof(XxHash3), () => new XxHash3())
    }.ToDictionary(o => o.Key, o => o).ToFrozenDictionary();

    public static FrozenDictionary<string, StorageFileHasher> Hashers_ByName { get; } = Hashers_ByKey.Values.ToDictionary(o => o.Name, o => o).ToFrozenDictionary();

    //public static FrozenSet<int> HashLengths { get; } = Hashers_ByKey.Values.Select(o => o.HashLength).Distinct().ToFrozenSet();
    public static FrozenSet<int> HashLengths_WithPrefix { get; } = Hashers_ByKey.Values.Select(o => o.HashLength + 2).Distinct().ToFrozenSet();

    public char Key { get; } = key;
    public string Name { get; } = name;
    public int HashLength => HashEmpty.Length;
    public abstract string HashEmpty { get; }

    protected static string ToHashString(byte[] hash) => Util.Base16(hash);

    public abstract string Hash(Stream stream);

    public abstract string Hash(byte[] array);

    private sealed class HasherCrypto : StorageFileHasher
    {
        private readonly Func<HashAlgorithm> algorithmFactory;

        public override string HashEmpty { get; }

        public HasherCrypto(char key, string name, Func<HashAlgorithm> algorithmFactory) : base(key, name)
        {
            this.algorithmFactory = algorithmFactory;
            HashEmpty = Hash(Array.Empty<byte>());
        }

        public override string Hash(byte[] array)
        {
            using var a = algorithmFactory();
            return ToHashString(a.ComputeHash(array));
        }

        public override string Hash(Stream stream)
        {
            using var a = algorithmFactory();
            return ToHashString(a.ComputeHash(stream));
        }
    }

    private sealed class HasherNonCrypto : StorageFileHasher
    {
        private readonly Func<NonCryptographicHashAlgorithm> algorithmFactory;

        public override string HashEmpty { get; }

        public HasherNonCrypto(char key, string name, Func<NonCryptographicHashAlgorithm> algorithmFactory) : base(key, name)
        {
            this.algorithmFactory = algorithmFactory;
            HashEmpty = Hash(Array.Empty<byte>());
        }

        public override string Hash(byte[] array)
        {
            var a = algorithmFactory();
            a.Append(array);
            return ToHashString(a.GetHashAndReset());
        }

        public override string Hash(Stream stream)
        {
            var a = algorithmFactory();
            a.Append(stream);
            return ToHashString(a.GetHashAndReset());
        }
    }


    public static StorageFileHasherReadResult? ReadHash(string fileNamePart)
    {
        var p = fileNamePart.TrimOrNull();
        if (p == null) return null;
        var pLen = p.Length;
        if (pLen < 3) return null;
        if (!HashLengths_WithPrefix.Contains(pLen)) return null;

        if (fileNamePart[0] is not ('h' or 'H')) return null; // first character must be 'h' or 'H'
        var hasherKey = char.ToUpper(fileNamePart[1]);
        var hasher = Hashers_ByKey.GetValueOrDefault(hasherKey);
        if (hasher == null) return null;
        var hashString = fileNamePart[2..].TrimOrNull();
        if (hashString == null) return null;
        if (!HexStringRegex().IsMatch(hashString)) return null;
        return new(hasher, hashString.ToUpper());
    }

    [GeneratedRegex("^([a-fA-F0-9]{2})+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript, "en-US")]
    private static partial Regex HexStringRegex();
}
