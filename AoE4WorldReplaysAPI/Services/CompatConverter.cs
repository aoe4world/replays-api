using AoE4WorldReplaysParser;
using AoE4WorldReplaysParser.Summary;
using AoE4WorldReplaysParser.Services;
using static AoE4WorldReplaysParser.Services.Parser;
using System;

namespace AoE4WorldReplaysAPI.Services
{
    public static class CompatConverter
    {
        /// <summary>
        /// Convert new format to old one for easier transition
        /// </summary>
        public static Parser.PlayerSummary[] Convert(ReplaySummary replaySummary, GameSummary gameSummary)
        {
            var result = new List<Parser.PlayerSummary>();

            var i = 0;
            foreach (var player in gameSummary.Players)
            {
                result.Add(new Parser.PlayerSummary
                {
                    Name = player.PlayerName,
                    ProfileId = player.PlayerProfileId,
                    CivilizationAttrib = player.Civ,
                    Actions = MapActions(replaySummary.Players[i], player),
                    Scores = new Parser.Scores
                    {
                        Total = player.Timeline.LastOrDefault()?.ScoreTotal ?? 0.0f,
                        Economy = player.Timeline.LastOrDefault()?.ScoreEconomy ?? 0.0f,
                        Military = player.Timeline.LastOrDefault()?.ScoreMilitary ?? 0.0f,
                        Society = player.Timeline.LastOrDefault()?.ScoreSociety ?? 0.0f,
                        Technology = player.Timeline.LastOrDefault()?.ScoreTechnology ?? 0.0f
                    },
                    TotalResourcesGathered = MapResourcesTotal(player.TotalResourcesGathered),
                    TotalResourcesSpent = MapResourcesTotal(player.TotalResourcesSpent),
                    Resources = MapResources(player.Timeline, player.Civ),
                    BuildOrder = MapBuildOrder(replaySummary.Players[i], player)
                });

                i++;
            }

            return result.ToArray();
        }

        private static Dictionary<string, int> MapResourcesTotal(PlayerResources resources)
        {
            var result = new Dictionary<string, int>();
            result["food"] = (int)Math.Round(resources.food);
            result["gold"] = (int)Math.Round(resources.gold);
            result["stone"] = (int)Math.Round(resources.stone);
            result["wood"] = (int)Math.Round(resources.wood);
            result["oliveoil"] = (int)Math.Round(resources.merc_byz);
            result["total"] = result.Values.Sum();
            return result;
        }

        private static Dictionary<string, List<int>> MapResources(List<PlayerTimeline> timeline, string civilization)
        {
            var result = new Dictionary<string, List<int>>();

            result["timestamps"] = timeline.Select(v => (int)v.Timestamp).ToList();
            result["food"] = timeline.Select(v => (int)Math.Round(v.ResourcesCurrent.food)).ToList();
            result["gold"] = timeline.Select(v => (int)Math.Round(v.ResourcesCurrent.gold)).ToList();
            result["stone"] = timeline.Select(v => (int)Math.Round(v.ResourcesCurrent.stone)).ToList();
            result["wood"] = timeline.Select(v => (int)Math.Round(v.ResourcesCurrent.wood)).ToList();

            result["food_per_min"] = timeline.Select(v => (int)Math.Round(v.ResourcesPerMinute.food)).ToList();
            result["gold_per_min"] = timeline.Select(v => (int)Math.Round(v.ResourcesPerMinute.gold)).ToList();
            result["stone_per_min"] = timeline.Select(v => (int)Math.Round(v.ResourcesPerMinute.stone)).ToList();
            result["wood_per_min"] = timeline.Select(v => (int)Math.Round(v.ResourcesPerMinute.wood)).ToList();
            if (civilization == "byzantine")
            {
                result["oliveoil"] = timeline.Select(v => (int)Math.Round(v.ResourcesCurrent.merc_byz)).ToList();
                result["oliveoil_per_min"] = timeline.Select(v => (int)Math.Round(v.ResourcesPerMinute.merc_byz)).ToList();
            }

            if (timeline.Count > 0 && timeline[0].ResourcesCumulative != null)
            {
                result["food_gathered"] = timeline.Select(v => (int)Math.Round(v.ResourcesCumulative.food)).ToList();
                result["gold_gathered"] = timeline.Select(v => (int)Math.Round(v.ResourcesCumulative.gold)).ToList();
                result["stone_gathered"] = timeline.Select(v => (int)Math.Round(v.ResourcesCumulative.stone)).ToList();
                result["wood_gathered"] = timeline.Select(v => (int)Math.Round(v.ResourcesCumulative.wood)).ToList();
                if (civilization == "byzantine")
                {
                    result["oliveoil_gathered"] = timeline.Select(v => (int)Math.Round(v.ResourcesCumulative.merc_byz)).ToList();
                }
            }

            result["total"] = timeline.Select(v => (int)Math.Round(v.ScoreTotal)).ToList();
            result["military"] = timeline.Select(v => (int)Math.Round(v.ScoreMilitary)).ToList();
            result["economy"] = timeline.Select(v => (int)Math.Round(v.ScoreEconomy)).ToList();
            result["technology"] = timeline.Select(v => (int)Math.Round(v.ScoreTechnology)).ToList();
            result["society"] = timeline.Select(v => (int)Math.Round(v.ScoreSociety)).ToList();

            return result;
        }

        private static Dictionary<string, List<uint>> MapActions(ReplaySummaryPlayer replaySummary, AoE4WorldReplaysParser.Summary.PlayerSummary playerSummary)
        {
            var result = new Dictionary<string, List<uint>>();

            foreach (var ability in replaySummary.Abilities)
            {
                var actionName = ability.commandType;

                if (!actionName.StartsWith("upgrade_") && !actionName.EndsWith("_age") && !actionName.Contains("relic") && !actionName.Contains("holy"))
                    continue;

                if (result.TryGetValue(ability.commandType, out var item) || item == null)
                    item = result[ability.commandType] = new List<uint>();

                if (ability.details.Any())
                {
                    foreach (var detail in ability.details)
                    {
                        item.Add((uint)detail.timestamp);
                    }
                }
                else
                {
                    item.Add((uint)ability.timestampFirstUse);
                }
            }

            return result;                    
        }

        private static BuildOrderEntry[] MapBuildOrder(ReplaySummaryPlayer replaySummary, AoE4WorldReplaysParser.Summary.PlayerSummary playerSummary)
        {
            var result = new Dictionary<string, BuildOrderEntry>();

            // Older versions of the game duplicated starting units, the GameSummaryGenerator detects this, but the raw structure doesn't, so piggyback on that mechanism.
            var fakes = playerSummary.FakeStartingUnits?.ToList() ?? new List<PlayerEntity>();
            var fakeTCSkip = 0;

            foreach (var unit in replaySummary.PlayerDetails.unitTimeline)
            {
                if (string.IsNullOrEmpty(unit.unitIcon))
                    continue;

                var id = unit.pbgid?.ToString() ?? unit.modid?.ToHex() ?? unit.unitIcon;

                var timestamp = (uint)unit.timestamp;
                var icon = unit.unitIcon.Replace('\\', '/');
                var type = BuildOrderEntryType.Unknown;
                var typeId = (uint)unit.unknown6;

                if (icon.Contains("/buildings/"))
                {
                    type = BuildOrderEntryType.Building;
                }
                else if (icon.Contains("/units/"))
                {
                    type = BuildOrderEntryType.Unit;
                }
                else if (icon.Contains("/upgrades/") || icon.Contains("/uprades/"))
                {
                    type = BuildOrderEntryType.Upgrade;
                }
                else if (icon.Contains("/animals/"))
                {
                    type = BuildOrderEntryType.Animal;
                }
                else if (icon.Contains("/age/"))
                {
                    type = BuildOrderEntryType.Age;
                }

                if (timestamp == 0)
                {
                    var fake = fakes.FirstOrDefault(v => v.EntityType == "fake:" + unit.unitIcon);

                    if (fake != null && unit.unitIcon.Contains("town_center") && fakeTCSkip == 0)
                    {
                        fakeTCSkip++;
                    }
                    else if (fake != null)
                    {
                        // Skip one of em
                        fakes.Remove(fake);
                        continue;
                    }
                }

                if (!result.TryGetValue(id, out var buildOrderEntry))
                {
                    buildOrderEntry = new BuildOrderEntry(
                        unit.unitLabel.Trim('$'),
                        icon,
                        unit.pbgid,
                        unit.modid?.ToHex(),
                        type,
                        new List<uint>(),
                        new List<uint>(),
                        new List<uint>(),
                        new List<uint>(),
                        new List<uint>(),
                        new List<uint>(),
                        new Dictionary<uint, List<uint>>()
                        );

                    result[id] = buildOrderEntry;
                }

                if (typeId == 1)
                {
                    buildOrderEntry.Finished.Add(timestamp);
                }
                else if (typeId == 2)
                {
                    buildOrderEntry.Destroyed.Add(timestamp);
                }
                else if (typeId == 3)
                {
                    if (type == BuildOrderEntryType.Building)
                    {
                        buildOrderEntry.Packed.Add(timestamp);
                    }
                    else
                    {
                        buildOrderEntry.Finished.Add(timestamp);
                    }
                }
                else if (typeId == 4)
                {
                    buildOrderEntry.Destroyed.Add(timestamp);
                }
                else if (typeId == 5)
                {
                    var isUnpacking =
                        buildOrderEntry.Packed.Count > 0
                        && (buildOrderEntry.Constructed.Count == 0 || ((buildOrderEntry.Packed.Last() > buildOrderEntry.Constructed.Last())))
                        && (buildOrderEntry.Unpacked.Count == 0 || (buildOrderEntry.Packed.Last() > buildOrderEntry.Unpacked.Last()));

                    if (isUnpacking)
                    {
                        buildOrderEntry.Unpacked.Add(timestamp);
                    }
                    else
                    {
                        buildOrderEntry.Constructed.Add(timestamp);
                    }
                }
                else if (typeId == 6)
                {
                    if (type == BuildOrderEntryType.Building)
                    {
                        buildOrderEntry.Destroyed.Add(timestamp);
                    }
                    else
                    {
                        // ignore as it already exists in the 4 list for siege
                        buildOrderEntry.Unknown.TryAdd(typeId, new List<uint>());
                        buildOrderEntry.Unknown[typeId].Add(timestamp);
                    }
                }
                else if (typeId == 8)
                {
                    buildOrderEntry.Finished.Add(timestamp);
                }
                else if (typeId == 12)
                {
                    buildOrderEntry.Finished.Add(timestamp);
                }
                else
                {
                    buildOrderEntry.Unknown.TryAdd(typeId, new List<uint>());
                    buildOrderEntry.Unknown[typeId].Add(timestamp);
                }
            }

            return result.Values.ToArray();
        }
    }
}
