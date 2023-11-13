#nullable disable

namespace AoE4WorldReplaysParser.Models;


public class DataSTLPEntry : DataModelBase, IDeserializable
{
    public float x;
    public float y;
    public int timestamp;

    public void Deserialize(RelicBlobReader reader)
    {
        x = reader.ReadSingle();
        y = reader.ReadSingle();
        timestamp = reader.ReadInt32();
    }
}

public class DataSTLP : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string commandType;
    public short unknown2;
    public short category;
    public int amount;
    public int timestampFirstUse;
    public DataSTLPEntry[] details;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(2002);

        unknown1 = reader.ReadByte();
        commandType = reader.ReadPrefixedString();
        Identifier += $" - {commandType}";
        unknown2 = reader.ReadInt16();
        category = reader.ReadInt16();
        amount = reader.ReadInt32();
        timestampFirstUse = reader.ReadInt32();
        details = reader.ReadPrefixedArray<DataSTLPEntry>();
    }
}
