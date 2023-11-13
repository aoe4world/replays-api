namespace AoE4WorldReplaysParser;

public interface IDeserializable
{
    void Deserialize(RelicBlobReader reader);
}
