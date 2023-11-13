using AoE4WorldReplaysParser.Models;

namespace AoE4WorldReplaysParser;

public class ReplaySummary
{
    public DataSTLS DataSTLS { get; set; }

    public List<ReplaySummaryPlayer> Players { get; private set; } = new List<ReplaySummaryPlayer>();
}
