#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class DataSTLUGroup : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string unitType;
    public short unknown2;
    public short unknown3;
    public int unknown4;
    public int unknown5;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadByte();
        unitType = reader.ReadPrefixedString();
        unknown2 = reader.ReadInt16();
        unknown3 = reader.ReadInt16();
        unknown4 = reader.ReadInt32();
        unknown5 = reader.ReadInt32();
    }
}
public class DataSTLUEntry : DataModelBase, IDeserializable
{
    public int unknown1;
    public int unknown2;

    public void Deserialize(RelicBlobReader reader)
    {
        unknown1 = reader.ReadInt32();
        unknown2 = reader.ReadInt32();
    }
};

//  STLU appears to contain certain interactions, it lists units of the opponent.
//  These aren't always 'lost' units, so could be simply attacked units. It doesn't list own units.
public class DataSTLU : DataModelBase, IDeserializable
{
    public int pbgid;
    public ResourceDict unknownResources;
    public int numProduced;
    public int unknown3;
    public int numLost;
    public int unknown5;
    public int unknown6;
    public int unknown7;
    public int unknown8;
    public int unknown9;
    public int unknown10;
    public int unknown11;
    public int unknown12;
    public int unknown13;
    public int unknown14;
    public int unknown15;
    public int unknown16;
    public int unknown17;
    public int unknown18;
    public DataSTLUGroup[] groups;
    public int[] unknown19;
    public int numTotal;
    public DataSTLUEntry[] entries;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(2009);

        pbgid = reader.ReadInt32();
        Identifier += $" - {pbgid}";
        unknownResources = reader.ReadStruct<ResourceDict>();
        numProduced = reader.ReadInt32();
        unknown3 = reader.ReadInt32();
        numLost = reader.ReadInt32();
        unknown5 = reader.ReadInt32();
        unknown6 = reader.ReadInt32();
        unknown7 = reader.ReadInt32();
        unknown8 = reader.ReadInt32();
        unknown9 = reader.ReadInt32();
        unknown10 = reader.ReadInt32();
        unknown11 = reader.ReadInt32();
        unknown12 = reader.ReadInt32();
        unknown13 = reader.ReadInt32();
        unknown14 = reader.ReadInt32();
        unknown15 = reader.ReadInt32();
        unknown16 = reader.ReadInt32();
        unknown17 = reader.ReadInt32();
        unknown18 = reader.ReadInt32();
        groups = reader.ReadPrefixedArray<DataSTLUGroup>();
        unknown19 = reader.ReadInt32Array(12);
        numTotal = reader.ReadInt32();
        entries = reader.ReadPrefixedArray<DataSTLUEntry>();
    }
};
