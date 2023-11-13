#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class DataSTDDUpgradeSubEntry : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string unitType;
    public short unknown3;
    public short unknown4;
    public float unknown5;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadByte();
        unitType = reader.ReadPrefixedString();
        unknown3 = reader.ReadInt16();
        unknown4 = reader.ReadInt16();
        unknown5 = reader.ReadSingle();
    }
}

public class DataSTDDUnitSubEntry : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string unitType;
    public short unknown3;
    public short unknown4;
    public float unknown5;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadByte();
        unitType = reader.ReadPrefixedString();
        unknown3 = reader.ReadInt16();
        unknown4 = reader.ReadInt16();
        unknown5 = reader.ReadSingle();
    }
}

public class DataSTDDBuildingSubGroup : DataModelBase, IDeserializable
{
    public int playerId;
    public float unknown2;
    public DataSTDDUpgradeSubEntry[] entries;

    public void Deserialize(RelicBlobReader reader)
    {
        playerId = reader.ReadInt32();
        unknown2 = reader.ReadSingle();
        entries = reader.ReadPrefixedArray<DataSTDDUpgradeSubEntry>();
    }
}

public class DataSTDDUnitSubGroup : DataModelBase, IDeserializable
{
    public int playerId;
    public float unknown2;
    public DataSTDDUnitSubEntry[] entries;

    public void Deserialize(RelicBlobReader reader)
    {
        playerId = reader.ReadInt32();
        unknown2 = reader.ReadSingle();
        entries = reader.ReadPrefixedArray<DataSTDDUnitSubEntry>();
    }
}

public class DataSTDDBuildingGroup : DataModelBase, IDeserializable
{
    public int entityId;
    public int unknown2;
    public DataSTDDBuildingSubGroup[] subGroups;

    public void Deserialize(RelicBlobReader reader)
    {
        entityId = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
        subGroups = reader.ReadPrefixedArray<DataSTDDBuildingSubGroup>();
    }
}

public class DataSTDDUnitGroup : DataModelBase, IDeserializable
{
    public int entityId;
    public int unknown2;
    public DataSTDDUnitSubGroup[] subGroups;

    public void Deserialize(RelicBlobReader reader)
    {
        entityId = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
        subGroups = reader.ReadPrefixedArray<DataSTDDUnitSubGroup>();
    }
}

public class DataSTDD : DataModelBase, IDeserializable
{
    public DataSTDDBuildingGroup[] buildings;
    public DataSTDDUnitGroup[] units;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(1001);

        buildings = reader.ReadPrefixedArray<DataSTDDBuildingGroup>();
        units = reader.ReadPrefixedArray<DataSTDDUnitGroup>();
    }
}
