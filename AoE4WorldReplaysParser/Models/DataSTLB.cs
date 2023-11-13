#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class DataSTLB : DataModelBase, IDeserializable
{
    public byte unknown1;
    public string buildingType;
    public short unknown2;
    public short unknown3;
    public float damageDealt;
    public int created;
    public int destroyed;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(2);

        unknown1 = reader.ReadByte();
        buildingType = reader.ReadPrefixedString();
        Identifier += $" - {buildingType}";
        unknown2 = reader.ReadInt16();
        unknown3 = reader.ReadInt16();
        damageDealt = reader.ReadSingle();
        created = reader.ReadInt32();
        destroyed = reader.ReadInt32();
    }
}
