using System.Text;
using AOEMods.Essence.Chunky.Core;

namespace AoE4WorldReplaysParser;

public partial class RelicBlobReader : BinaryReader
{
    // These limits are to ensure there won't be too much memory allocations for corrupt files
    public const int MaxArrayLength = 100000;
    public const int MaxStringLength = 1000;

    private string _file;
    private string _path;
    private Stack<DataModelBase> _structParents = new Stack<DataModelBase>();

    public ChunkHeader Header { get; set; }
    public int StructVersion { get; set; }
    public Action<object> NewStructCallback;

    public string Identifier { get; set; }

    public RelicBlobReader(string file, string path, Stream input)
        : base(input, Encoding.UTF8, true)
    {
        _file = file;
        _path = path;
    }

    public string ReadPrefixedString()
    {
        var length = ReadInt32();

        Assert(length < MaxStringLength, $"String length {length} exceeds allowed safety maximum");

        var bytes = ReadBytes(length);

        var str = Encoding.UTF8.GetString(bytes);

        return str;
    }

    public string ReadPrefixedUnicodeString()
    {
        var length = ReadInt32();

        Assert(length < MaxStringLength, $"String length {length} exceeds allowed safety maximum");

        var bytes = ReadBytes(length * 2);

        var str = Encoding.Unicode.GetString(bytes);

        return str;
    }

    public int[] ReadInt32Array(int count)
    {
        Assert(count < MaxArrayLength, $"Array length {count} exceeds allowed safety maximum");

        var result = new int[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = ReadInt32();
        }

        return result;
    }

    public T ReadStruct<T>()
        where T : IDeserializable, new()
    {
        var result = new T();

        if (result is DataModelBase model)
        {
            if (_structParents.TryPeek(out var parent))
            {
                model.File = parent.File;
                model.Identifier = parent.Identifier;
            }
            else
            {
                model.File = _file;
                model.Identifier = Identifier ?? string.Empty;
            }
            _structParents.Push(model);
        }
        
        result.Deserialize(this);

        if (result is DataModelBase)
        {
            _structParents.Pop();
        }

        NewStructCallback?.Invoke(result);

        return result;
    }

    [Obsolete]
    public T[] ReadArray<T>(int count, Func<RelicBlobReader, T> func)
    {
        Assert(count < MaxArrayLength, $"Array length {count} exceeds allowed safety maximum");

        var result = new T[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = func(this);
        }

        return result;
    }

    [Obsolete]
    public T[] ReadPrefixedArray<T>(Func<RelicBlobReader, T> func)
    {
        var count = ReadInt32();

        Assert(count < MaxArrayLength, $"Array length {count} exceeds allowed safety maximum");

        return ReadArray(Math.Max(0, count), func);
    }

    public T[] ReadPrefixedArray<T>()
        where T : IDeserializable, new()
    {
        var count = ReadInt32();

        Assert(count < MaxArrayLength, $"Array length {count} exceeds allowed safety maximum");

        var result = new T[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = ReadStruct<T>();
        }

        return result;
    }

    public int PeekInt32()
    {
        var pos = BaseStream.Position;
        var result = ReadInt32();
        BaseStream.Position = pos;
        return result;
    }

    public void Assert(bool condition, string message = null)
    {
        if (!condition)
        {
            throw new ParserException(_path, BaseStream.Position + (Header?.DataPosition ?? 0), BaseStream.Position, message ?? "Unknown parser failure");
        }
    }

    public void AssertStructVersion(params int[] versions)
    {
        if (!versions.Contains(StructVersion))
            throw new ParserException(_path, BaseStream.Position + (Header?.DataPosition ?? 0), BaseStream.Position, $"Struct version {StructVersion} not supported, wait for developers to update parser");

    }
}

