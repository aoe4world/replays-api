namespace AoE4WorldReplaysParser.Summary;

public class PlayerResources
{
    public float food { get; set; }
    public float gold { get; set; }
    public float stone { get; set; }
    public float wood { get; set; }
    public float merc_byz { get; set; }
}

public class PlayerTimeline
{
    public float Timestamp { get; set; }
    public PlayerResources ResourcesCurrent { get; set; }
    public PlayerResources ResourcesPerMinute { get; set; }
    public PlayerResources ResourcesUnitValue { get; set; }
    public PlayerResources ResourcesCumulative { get; set; }

    public float ScoreTotal { get; set; }
    public float ScoreEconomy { get; set; }
    public float ScoreMilitary { get; set; }
    public float ScoreSociety { get; set; }
    public float ScoreTechnology { get; set; }
}

public enum PlayerOutcome
{
    Win = 5,
    Unknown = 6,
    Surrender = 8
}

public enum PlayerColor
{
    Blue = 0,
    Red = 1,
    Yellow = 2,
    Green = 3,
    Teal = 4,
    Purple = 5,
    Orange = 6,
    Pink = 7
}

public class PlayerEntity
{
    public int PlayerId { get; set; }
    public int Id { get; set; }
    
    public int? PbgId { get; set; }
    public string EntityType { get; set; }
    
    public float? SpawnTimestamp { get; set; }
    public float? SpawnX { get; set; }
    public float? SpawnY { get; set; }
    
    public float? DeathTimestamp { get; set; }
    public float? DeathX { get; set; }
    public float? DeathY { get; set; }

    public int? KillerPlayerId { get; set; }
    public int? KillerEntityId { get; set; }
    public float? KillerX { get; set; }
    public float? KillerY { get; set; }
    public string KillerWeaponType { get; set; }

    public override string ToString()
    {
        return $"{SpawnTimestamp}: {Id}:{EntityType} [{SpawnX}, {SpawnY}]";
    }
}


public class PlayerSummary
{
    public int PlayerId { get; set; }
    public int PlayerProfileId { get; set; }
    public string PlayerName { get; set; }
    public PlayerColor PlayerColor { get; set; }
    public string Civ { get; set; }
    public PlayerOutcome Outcome { get; set; }
    public float? TimestampEliminated { get; set; }

    public PlayerResources TotalResourcesGathered { get; set; }
    public PlayerResources TotalResourcesSpent { get; set; }

    public int TechResearched { get; set; }
    public PlayerResources TotalResourcesSpentOnUpgrades { get; set; }

    public int UnitsProduced { get; set; }

    public int UnitsProducedInfantry { get; set; }
    public int UnitsProducedInfantryResources { get; set; }

    public int BuildingsLost { get; set; }
    public int UnitsLost { get; set; }
    public int UnitsLostResourceValue { get; set; }

    public int BuildingsRazed { get; set; }
    public int UnitsKilled { get; set; }
    public int UnitsKilledResourceValue { get; set; }

    public int SacredSitesCaptured { get; set; }
    public int SacredSitesLost { get; set; }
    public int SacredSitesNeutralized { get; set; }

    public int RelicsCaptured { get; set; }

    public float? Age2Timestamp { get; set; }
    public float? Age3Timestamp { get; set; }
    public float? Age4Timestamp { get; set; }

    public List<PlayerTimeline> Timeline { get; } = new List<PlayerTimeline>();

    public List<PlayerEntity> StartingUnits { get; } = new List<PlayerEntity>();
    public List<PlayerEntity> FakeStartingUnits { get; set; }
    public List<PlayerEntity> Units { get; } = new List<PlayerEntity>();
}

public class GameSummary
{

    public List<PlayerSummary> Players { get; } = new List<PlayerSummary>();
}
