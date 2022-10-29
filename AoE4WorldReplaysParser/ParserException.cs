namespace AoE4WorldReplaysParser;

public class ParserException : Exception
{
    public string Identifier { get; private set; }
    public long FilePosition { get; private set; }
    public long Position { get; private set; }

    public ParserException(string identifier, long filePosition, long position, string message)
        : base($"{message} [before {filePosition:X}h->{identifier}:{position:X}h]")
    {
        Identifier = identifier;
        FilePosition = filePosition;
        Position = position;
    }
}

