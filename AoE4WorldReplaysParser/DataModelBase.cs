#nullable disable

using System.Text.Json.Serialization;

namespace AoE4WorldReplaysParser;

public abstract class DataModelBase
{
    [JsonIgnore]
    public string File { get; set; }
    [JsonIgnore]
    public string Identifier { get; set; }
}
