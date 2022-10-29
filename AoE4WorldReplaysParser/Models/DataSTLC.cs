#nullable disable

namespace AoE4WorldReplaysParser.Models;


public class DataSTLCEntry : DataModelBase, IDeserializable
{
    public int pbgid;
    public int lost;
    public int gained;

    public void Deserialize(RelicBlobReader reader)
    {
        pbgid = reader.ReadInt32();
        lost = reader.ReadInt32();
        gained = reader.ReadInt32();
    }
}

// StatLogging Conversions
public class DataSTLC : DataModelBase, IDeserializable
{
    public DataSTLCEntry[] details;

    public void Deserialize(RelicBlobReader reader)
    {
        reader.AssertStructVersion(1);

        details = reader.ReadPrefixedArray<DataSTLCEntry>();
    }
}
