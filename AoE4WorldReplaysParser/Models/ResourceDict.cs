#nullable disable

namespace AoE4WorldReplaysParser.Models;

public class ResourceDict : IDeserializable
{
    public float action;
    public float command;
    public float food;
    public float gold;
    public float merc_byz;
    public float militia_hre;
    public float popcap;
    public float stone;
    public float wood;

    public void Deserialize(RelicBlobReader reader)
    {
        var keyPairCount = reader.ReadInt32();
        reader.Assert(keyPairCount == 8 || keyPairCount == 9);

        action = DeserializePair(reader, nameof(action));
        command = DeserializePair(reader, nameof(command));
        food = DeserializePair(reader, nameof(food));
        gold = DeserializePair(reader, nameof(gold));
        if (keyPairCount == 9)
          merc_byz = DeserializePair(reader, nameof(merc_byz));
        militia_hre = DeserializePair(reader, nameof(militia_hre));
        popcap = DeserializePair(reader, nameof(popcap));
        stone = DeserializePair(reader, nameof(stone));
        wood = DeserializePair(reader, nameof(wood));
    }

    public override string ToString()
    {
        return $"{food}f {gold}g {stone}s {wood}w";
    }

    private float DeserializePair(RelicBlobReader reader, string expectedKey)
    {
        var key = reader.ReadPrefixedString();
        reader.Assert(key == expectedKey);

        return reader.ReadSingle();
    }
}
