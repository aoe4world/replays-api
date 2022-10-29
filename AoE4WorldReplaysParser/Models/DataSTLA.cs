#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class DataSTLAEntry1 : DataModelBase, IDeserializable
{
    public int unknown1;
    public int unknown2;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
    }
}

public class DataSTLAEntry2 : DataModelBase, IDeserializable
{
    public int unknown1;
    public int unknown2;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
    }
}

public class DataSTLAGroup : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string buildingType;
    public short unknown3;
    public short unknown4;
    public float damageInflicted;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadByte();
        buildingType = reader.ReadPrefixedString();
        unknown3 = reader.ReadInt16();
        unknown4 = reader.ReadInt16();
        damageInflicted = reader.ReadSingle();
    }
}

public class DataSTLA : DataModelBase, IDeserializable
{
    public DataSTLAEntry1[] entries1;
    public DataSTLAEntry2[] entries2;
    public DataSTLAGroup[] groups;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(1);

        entries1 = reader.ReadPrefixedArray<DataSTLAEntry1>();
        entries2 = reader.ReadPrefixedArray<DataSTLAEntry2>();
        groups = reader.ReadPrefixedArray<DataSTLAGroup>();
    }
}
