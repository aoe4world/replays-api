using System.Text;
using AoE4WorldReplaysParser.Models;
using AOEMods.Essence.Chunky.Graph;

namespace AoE4WorldReplaysParser;

public class ReplayFullParser : ReplayParserBase
{
    public Dictionary<Type, List<object>> AllStructs { get; set; } = new Dictionary<Type, List<object>>();

    public ReplayFullParser()
    {

    }

    public ReplayFullParser(Stream stream, string name)
        : base(stream, name)
    {

    }

    public void Load(Stream stream, string name)
    {
        _stream = stream;
        _name = name;
    }

    public static bool IsReplayFullFile(Stream stream)
    {
        var pos = stream.Position;
        var magic1 = new byte[4];
        var magic2 = new byte[8];
        stream.Read(magic1);
        stream.Read(magic2);
        stream.Position = pos;

        // Note 'magic1' might be a version identifier, different values have been seen

        return Encoding.Default.GetString(magic2) == "AOE4_RE\0";
    }

    public override void Parse()
    {
        throw new NotImplementedException();
    }
}