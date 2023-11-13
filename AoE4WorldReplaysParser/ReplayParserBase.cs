using AOEMods.Essence.Chunky.Graph;

namespace AoE4WorldReplaysParser;

public abstract class ReplayParserBase
{
    protected string _name;
    protected Stream _stream;

    public ReplayParserBase()
    {

    }

    public ReplayParserBase(Stream stream, string name)
    {
        _stream = stream;
        _name = name;
    }

    public abstract void Parse();

    protected static IEnumerable<IChunkyDataNode> EnumerateNode(IEnumerable<IChunkyNode> nodes)
    {
        foreach (var childNode in nodes)
            foreach (var dataNode in EnumerateNode(childNode))
                yield return dataNode;
    }

    protected static IEnumerable<IChunkyDataNode> EnumerateNode(IChunkyNode node)
    {
        if (node is IChunkyFolderNode folderNode)
        {
            foreach (var dataNode in EnumerateNode(folderNode.Children))
                yield return dataNode;
        }
        else if (node is IChunkyDataNode dataNode)
        {
            yield return dataNode;
        }
    }
}
