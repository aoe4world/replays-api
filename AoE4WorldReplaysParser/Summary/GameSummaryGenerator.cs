namespace AoE4WorldReplaysParser.Summary;
 
public class GameSummaryGenerator
{
    // Units are spawned based on this number, and includes gaia units such as sheep, wolfs etc. STLS section of the replay files contains this in new unit entries, but unfortunately doesn't include starter units. But the behavior is fairly clear.
    const int firstUnitId = 50000;
    // Buildings are a bit more tricky, buildings don't get an ID, but it shows up when researching upgrades. The number increments much faster and I suspect this might be incrementing for each building blueprint placed by players (blueprint -> foundation -> completed building).
    const int minimumBuildingId = 1000000000;

    private int _nextStarterUnitId;
    private PlayerEntity[] _startingUnits;
    private List<PlayerEntity> _startingBuildings;
    private Dictionary<ReplaySummaryPlayer, PlayerSummary> _playerMap;

    private Dictionary<int, PlayerEntity> _knownUnitsById;
    private Dictionary<Tuple<float, float>, PlayerEntity> _knownBuildingsByCoord;

    private GameSummary _summary;
    private ReplaySummary _replaySummary;

    public GameSummary GenerateSummary(ReplaySummary replaySummary)
    {
        _summary = new GameSummary();
        _replaySummary = replaySummary;

        Initialize();

        // Phase 1: Initialize players global data
        foreach (var replayPlayer in replaySummary.Players)
        {
            var player = new PlayerSummary();
            _summary.Players.Add(player);
            _playerMap[replayPlayer] = player;

            player.PlayerId = replayPlayer.PlayerDetails!.playerId;
            player.PlayerProfileId = replayPlayer.PlayerDetails.playerProfileId;
            player.PlayerName = replayPlayer.PlayerDetails.playerName;
            player.PlayerColor = replayPlayer.PlayerDetails.playerColor.HasValue ? (PlayerColor)replayPlayer.PlayerDetails.playerColor : (PlayerColor)(player.PlayerId - 1000); // TODO: Handle old system
            player.Civ = replayPlayer.PlayerDetails.civ;
            player.Outcome = (PlayerOutcome)replayPlayer.PlayerDetails.outcome;
            player.TimestampEliminated = replayPlayer.PlayerDetails.timestampEliminated;

            player.TotalResourcesGathered = ConvertResources(replayPlayer.PlayerDetails.totalResourcesGathered);
            player.TotalResourcesSpent = ConvertResources(replayPlayer.PlayerDetails.totalResourcesSpent);

            player.TechResearched = replayPlayer.PlayerDetails.techResearched;
            player.TotalResourcesSpentOnUpgrades = ConvertResources(replayPlayer.PlayerDetails.totalResourcesSpentForUpgrades);

            player.UnitsProduced = replayPlayer.PlayerDetails.unitsProduced;

            player.UnitsProducedInfantry = replayPlayer.PlayerDetails.unitsProducedInfantry;
            player.UnitsProducedInfantryResources = replayPlayer.PlayerDetails.unitsProducedInfantryResources;

            player.BuildingsLost = replayPlayer.PlayerDetails.buildingsLost;
            player.UnitsLost = replayPlayer.PlayerDetails.unitsLost;
            player.UnitsLostResourceValue = replayPlayer.PlayerDetails.unitsLostResources;

            player.BuildingsRazed = replayPlayer.PlayerDetails.buildingsRazed;
            player.UnitsKilled = replayPlayer.PlayerDetails.unitsKilled;
            player.UnitsKilledResourceValue = replayPlayer.PlayerDetails.unitsKilledResources;

            player.SacredSitesCaptured = replayPlayer.PlayerDetails.sacredCaptured;
            player.SacredSitesLost = replayPlayer.PlayerDetails.sacredLost;
            player.SacredSitesNeutralized = replayPlayer.PlayerDetails.sacredNeutralized;

            player.RelicsCaptured = (int)replayPlayer.PlayerDetails.relicsCaptured;

            player.Age2Timestamp = replayPlayer.PlayerDetails.age2AvailableTimestampMsec > 500 ? replayPlayer.PlayerDetails.age2AvailableTimestampMsec / 1000.0f : null;
            player.Age3Timestamp = replayPlayer.PlayerDetails.age3AvailableTimestampMsec > 500 ? replayPlayer.PlayerDetails.age3AvailableTimestampMsec / 1000.0f : null;
            player.Age4Timestamp = replayPlayer.PlayerDetails.age4AvailableTimestampMsec > 500 ? replayPlayer.PlayerDetails.age4AvailableTimestampMsec / 1000.0f : null;

            ConvertPlayerTimeline(player, replayPlayer);
        }

        // Phase 2a: Initialize player StartingUnits lists
        foreach (var replayPlayer in replaySummary.Players)
        {
            var player = _playerMap[replayPlayer];

            InitPlayerStartingUnits(player, replayPlayer);
        }

        // Phase 2b: Fix Fake Starting Units
        // Note that this is no longer needed after 7.0.5831
        foreach (var replayPlayer in replaySummary.Players)
        {
            var player = _playerMap[replayPlayer];

            FixFakePlayerStartingUnits(player, replayPlayer);
        }


        // Phase 3: Convert Build Order
        foreach (var replayPlayer in replaySummary.Players)
        {
            var player = _playerMap[replayPlayer];

            ConvertPlayerBuildOrder(player, replayPlayer);
        }

        return _summary;
    }

    private void Initialize()
    {
        _nextStarterUnitId = firstUnitId;
        _startingUnits = null;
        _startingBuildings = new List<PlayerEntity>();

        // Helper array to track starting units that aren't explicitly logged
        var firstUnitCreated = _replaySummary.DataSTLS.createdEntities.Where(v => v.entityId != 0 && v.entityId < 1000000000).FirstOrDefault()?.entityId ?? firstUnitId;
        _startingUnits = new PlayerEntity[firstUnitCreated - firstUnitId];

        _playerMap = new Dictionary<ReplaySummaryPlayer, PlayerSummary>();
        _knownUnitsById = new Dictionary<int, PlayerEntity>();
        _knownBuildingsByCoord = new Dictionary<Tuple<float, float>, PlayerEntity>();
    }

    protected void ConvertPlayerTimeline(PlayerSummary player, ReplaySummaryPlayer replayPlayer)
    {
        var scoreTimelineOffset = (replayPlayer.PlayerDetails.scoreTimeline.Length > 0 && replayPlayer.PlayerDetails.scoreTimeline[0].timestamp == 20) ? -1 : 0;
        for (var i = 0; i < replayPlayer.PlayerDetails!.resourceTimeline.Length; i++)
        {
            var resourceTimeline = replayPlayer.PlayerDetails.resourceTimeline[i];
            var scoreTimelineIndex = i + scoreTimelineOffset;
            var scoreTimeline = (scoreTimelineIndex >= 0 && scoreTimelineIndex < replayPlayer.PlayerDetails.scoreTimeline.Length) ? replayPlayer.PlayerDetails.scoreTimeline[scoreTimelineIndex] : new Models.DataSTPDScoreEntry();
            player.Timeline.Add(new PlayerTimeline
            {
                Timestamp = resourceTimeline.timestamp,
                ResourcesCurrent = ConvertResources(resourceTimeline.current),
                ResourcesPerMinute = ConvertResources(resourceTimeline.perMinute),
                ResourcesUnitValue = ConvertResources(resourceTimeline.units),
                ResourcesCumulative = resourceTimeline.cumulative != null ? ConvertResources(resourceTimeline.cumulative) : null,

                ScoreTotal = scoreTimeline.total,
                ScoreEconomy = scoreTimeline.economy,
                ScoreMilitary = scoreTimeline.military,
                ScoreSociety = scoreTimeline.society,
                ScoreTechnology = scoreTimeline.technology
            });
        }
    }

    protected static PlayerResources ConvertResources(Models.ResourceDict data)
    {
        return new PlayerResources
        { 
            food = data.food,
            gold = data.gold,
            stone = data.stone,
            wood = data.wood,
            merc_byz = data.merc_byz,
        };
    }

    protected void InitPlayerStartingUnits(PlayerSummary player, ReplaySummaryPlayer replayPlayer)
    {
        // Starter Units aren't added via STLS and we don't get their IDS
        // It'd be easier to simply load them from attrib, since there's a full list of starter units and positions. But we gotta try without so we can handle mods.

        // There's a bug in replays where players starter units are doubled, but only for standard games, not Nomad. I checked Nomad, it spawns 3 vils and nothing else, no duping.
        // So the mechanic here is to assume there's at least 1 unit that has 1 official instance, but is duped. So 2 TC, 2 scout, 12 vils and 2 sheeps. allowing us to detect the dupes.
        
        var startingUnitEntries = replayPlayer.PlayerDetails.unitTimeline.TakeWhile(v => v.timestamp == 0).ToList();
        var startingUnitMinCount = startingUnitEntries.GroupBy(v => v.pbgid).Select(g => g.Count()).DefaultIfEmpty().Min();
        if (startingUnitMinCount == 2 && startingUnitEntries.Count != 2)
        {
            // So this has to be a Standard game with dupes. Take the first half and register that as starting unit.
            // Note that I suspect that it's actually the second half that's 'real', with TC coming last, but it doesn't really matter.
            // For a regular chinese game: TC, scout, 6x vil, sheep, scout, 6x vil, sheep, TC

            // Anyway, let's fix this up. Also keep a list of the fake units and keep track of em, we need them later to fix vil count, unit count and resource value, which yes, is duped too.
            var fakeUnits = startingUnitEntries.Skip(startingUnitEntries.Count / 2).ToList();
            startingUnitEntries = startingUnitEntries.Take(startingUnitEntries.Count / 2).ToList();

            player.FakeStartingUnits = new List<PlayerEntity>();
            foreach (var unitEntry in fakeUnits)
            {
                var entity = new PlayerEntity
                {
                    PlayerId = replayPlayer.PlayerDetails!.playerId,
                    Id = 0,
                    PbgId = unitEntry.pbgid,
                    EntityType = "fake:" + unitEntry.unitIcon,

                    SpawnTimestamp = unitEntry.timestamp,
                    SpawnX = null,
                    SpawnY = null
                };

                player.FakeStartingUnits.Add(entity);
            }
        }

        foreach (var unitEntry in startingUnitEntries)
        {
            var entity = new PlayerEntity
            {
                PlayerId = replayPlayer.PlayerDetails!.playerId,
                Id = unitEntry.unitIcon.Contains("buildings") ? 0 : _nextStarterUnitId++,
                PbgId = unitEntry.pbgid,
                EntityType = "unknown:" + unitEntry.unitIcon,

                SpawnTimestamp = unitEntry.timestamp,
                SpawnX = null,
                SpawnY = null
            };

            if (entity.Id != 0)
            {
                var index = entity.Id - firstUnitId;
                if (index >= _startingUnits.Length)
                    Array.Resize(ref _startingUnits, index + 1);
                _startingUnits[index] = entity;
                _knownUnitsById[entity.Id] = entity;
            }
            else
            {
                _startingBuildings.Add(entity);
                //_knownBuildingsByCoord[Tuple.Create(entity.SpawnX.Value, entity.SpawnY.Value)] = entity;
            }

            player.StartingUnits.Add(entity);
            player.Units.Add(entity);
        }
    }

    protected void FixFakePlayerStartingUnits(PlayerSummary player, ReplaySummaryPlayer replayPlayer)
    {
        // There is a replay where units beyond the initial starting units are 'killed', notably by aoe damage.
        // However, the order in which these units appear remains tbd.

        // Also, the Fake units as mentioned in InitPlayerStartingUnits do count toward unit totals and unit values in the timeline.
        // TODO: Here we correct the player timeline and stats
    }

    protected void ConvertPlayerBuildOrder(PlayerSummary player, ReplaySummaryPlayer replayPlayer)
    {
        foreach (var createdEntity in _replaySummary.DataSTLS!.createdEntities)
        {
            if (createdEntity.playerId != replayPlayer.PlayerDetails!.playerId)
                continue;

            // Starter buildings are actually spawned.
            if (createdEntity.timestamp == 0)
            {
                if (createdEntity.entityType.Contains("_town_center_"))
                {
                    var starterEntity = player.StartingUnits.FirstOrDefault(v => v.EntityType.Contains("town_center"));

                    if (starterEntity != null)
                    {
                        starterEntity.EntityType = createdEntity.entityType;
                        starterEntity.SpawnX = createdEntity.x;
                        starterEntity.SpawnY = createdEntity.y;

                        continue;
                    }
                    else
                    {
                        //System.Diagnostics.Debugger.Break();
                    }
                }
                else
                {
                    //System.Diagnostics.Debugger.Break();
                }
            }
            
            if (createdEntity.entityCategory == Models.EntityCategory.Upgrade)
            {
                if (_knownBuildingsByCoord.TryGetValue(Tuple.Create(createdEntity.x, createdEntity.y), out var buildingEntity))
                {
                    if (buildingEntity.Id == 0)
                        buildingEntity.Id = createdEntity.entityId;
                    //else if (buildingEntity.Id != createdEntity.entityId) // Note, this can happen if a building is already destroyed and another one built in it's place.
                    //    System.Diagnostics.Debugger.Break();

                }
                else
                {
                    //System.Diagnostics.Debugger.Break();
                }

                continue;
            }

            var entity = new PlayerEntity
            {
                PlayerId = replayPlayer.PlayerDetails!.playerId,
                Id = createdEntity.entityId,

                //PbgId = createdEntity. // TODO: We can populate PbgId once we have Pbg database
                EntityType  = createdEntity.entityType,

                SpawnTimestamp = createdEntity.timestamp,
                SpawnX = createdEntity.x,
                SpawnY = createdEntity.y
            };

            player.Units.Add(entity);
        }

        foreach (var lostEntity in _replaySummary.DataSTLS!.lostEntities)
        {
            if (lostEntity.targetPlayerId != replayPlayer.PlayerDetails!.playerId)
                continue;

            var targetEntity = player.Units.FirstOrDefault(v => lostEntity.targetEntityId == 0 ? v.SpawnX == lostEntity.targetX && v.SpawnY == lostEntity.targetY : v.Id == lostEntity.targetEntityId);

            if (lostEntity.targetEntityId >= firstUnitId && lostEntity.targetEntityId < (_startingUnits.Length + firstUnitId))
            {
                // Starting units don't have proper entityType yet (we will once we have pbgid database), so assign them when we find them.
                var entity = _startingUnits[lostEntity.targetEntityId - firstUnitId];
                if (entity == null)
                {
                    // A Fake starting unit was killed, probably by aoe damage.
                    // It doesn't appear to affect the unit value left over at the endo f the game, but we may have to investigate later if this actually affects other stats.

                    // Example:
                    // Beasty_vs_fkensh_summary_M_44748553_2943e57a748445439fdb29d6036423d33ec60242fa86e1a9862178f8c91c3b4a contains lostEntity {2325.125: 50016:unit_villager_1_mon [148.5, -38.5]}. However the mongol is the second player, 50016 should not be a mongol unit given player one was chinese. So it's unclear why this happens.
                }
                else
                {
                    entity.EntityType = lostEntity.targetUnitType;
                }
            }

            if (lostEntity.targetEntityId != 0 && lostEntity.targetEntityId < (_startingUnits.Length + firstUnitId))
            {
            }

            if (targetEntity == null)
            {
                continue;
            }

            if (targetEntity.PlayerId != lostEntity.targetPlayerId)
            {
                System.Diagnostics.Debugger.Break();
            }

            targetEntity.DeathTimestamp = lostEntity.timestamp;
            targetEntity.DeathX = lostEntity.targetX;
            targetEntity.DeathY = lostEntity.targetY;

            if (lostEntity.hasAttacker != 0)
            {
                targetEntity.KillerPlayerId = lostEntity.attackerPlayerId;
                targetEntity.KillerEntityId = lostEntity.attackerEntityId;
                targetEntity.KillerX = lostEntity.attackerX;
                targetEntity.KillerY = lostEntity.attackerY;
                targetEntity.KillerWeaponType = lostEntity.weaponType;
            }
        }
    }
}
