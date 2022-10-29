using AoE4WorldReplaysParser.Models;

namespace AoE4WorldReplaysParser;

public class ReplaySummaryPlayer
{
    public DataSTPD PlayerDetails { get; set; }
    public List<DataSTLU> Units { get; private set; } = new List<DataSTLU>();
    public List<DataSTLB> Buildings { get; private set; } = new List<DataSTLB>();
    public List<DataSTLP> Abilities { get; private set; } = new List<DataSTLP>();
    public List<DataSTLC> Conversions { get; private set; } = new List<DataSTLC>();
    public List<DataSTLA> DataSTLA { get; private set; } = new List<DataSTLA>();
    public List<DataSTDD> DataSTDD { get; private set; } = new List<DataSTDD>();
};
