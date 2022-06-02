using AOEMods.Essence.Chunky.Core;
using AOEMods.Essence.Chunky.Graph;
using System.Text.RegularExpressions;

namespace AoE4WorldReplayParser.Services;

public class Parser
{
    public record struct PlayerSummary(
        string Name,
        Dictionary<string, List<uint>> Actions,
        Scores Scores,
        Dictionary<string, List<uint>> Resources,
        // Resources[] ResourcesOld,
        BuildOrderEntry[] BuildOrder
    );

    public readonly record struct Player(
        string Name
    );

    public readonly record struct BuildOrderAction(
        uint Timestamp,
        float Foo1,
        float Foo2
    );

    public readonly record struct Resources(
        uint Timestamp,
        float Action,
        float Command,
        float Food,
        float Gold,
        float Militia,
        float Popcap,
        float Stone,
        float Wood
    );

    public enum BuildOrderEntryType {
        Unit,
        Building,
        Upgrade,
        Animal,
        Age,
        Unknown
    }

    public record struct BuildOrderEntry(
        string Id,
        string Icon,

        BuildOrderEntryType Type,

        List<uint> Finished,
        List<uint> Constructed,
        List<uint> Packed,
        List<uint> Unpacked,
        List<uint> Transformed,
        List<uint> Destroyed,

        Dictionary<uint, List<uint>> Unknown
    );

    private bool debug;
    private int fileCounter = 0;

    public readonly record struct Scores(
        float Total,
        float Military,
        float Economy,
        float Technology,
        float Society
    );


    class ByteStrings {
        public static readonly byte[] ACTION = new byte[] {0x61, 0x63, 0x74, 0x69, 0x6F, 0x6E};
        public static readonly byte[] COMMAND = new byte[] {0x63, 0x6F, 0x6D, 0x6D, 0x61, 0x6E, 0x64};
        public static readonly byte[] FOOD = new byte[] {102, 111, 111, 100};
        public static readonly byte[] GOLD = new byte[] {103, 111, 108, 100};
        public static readonly byte[]  MILITIA_HRE = new byte[] {0x6D, 0x69, 0x6C, 0x69, 0x74, 0x69, 0x61, 0x5F, 0x68, 0x72, 0x65};
        public static readonly byte[] POPCAP = new byte[] {0x70, 0x6F, 0x70, 0x63, 0x61, 0x70};
        public static readonly byte[] STONE = new byte[] {115, 116, 111, 110, 101};
        public static readonly byte[] WOOD = new byte[] {119, 111, 111, 100};
        public static readonly byte[] ICONS = new byte[] {0x69, 0x63, 0x6F, 0x6E, 0x73};
    }

    Scores ProcessScores(Span<byte> bytes, Dictionary<string, List<uint>> result) {
        int firstIconsPosition = FindByteSequencePositions(bytes, ByteStrings.ICONS).First();
        int previousWoodPosition = FindByteSequencePositions(bytes, ByteStrings.WOOD).Where(p => p < firstIconsPosition).Last();
        int startPosition = previousWoodPosition + 20;
        int cursor = startPosition;

        var values = new List<float>();

        while (cursor < firstIconsPosition) {
            var f1 = ParseFloat(bytes, cursor);
            var f2 = ParseFloat(bytes, cursor += 4);
            var f3 = ParseFloat(bytes, cursor += 4);
            var f4 = ParseFloat(bytes, cursor += 4);
            var f5 = ParseFloat(bytes, cursor += 4);
            var i = BitConverter.ToInt32(bytes.Slice(cursor += 4));

            if (f1 < 0 || f2 < 0 || f3 < 0 || f4 < 0 || f5 < 0) {
                break;
            }

            if ((f1 > 0 && f1 < 0.001) || (f2 > 0 && f2 < 0.001) || (f3 > 0 && f3 < 0.001) || (f4 > 0 && f4 < 0.001) || (f5 > 0 && f5 < 0.001)) {
                break;
            }

            if (f1 > 999999 || f2 > 999999 || f3 > 999999 || f4 > 999999 || f5 > 999999) {
                break;
            }

            values.AddRange(new float[] {f1, f2, f3, f4, f5, i});
            cursor += 4;
        }

        result.Add("total", new List<uint>{ 0 });
        result.Add("military", new List<uint>{ 0 });
        result.Add("economy", new List<uint>{ 0 });
        result.Add("technology", new List<uint>{ 0 });
        result.Add("society", new List<uint>{ 0 });

        for (int i = 0; i < values.Count - 6; i += 6) {
            result["economy"].Add((uint)values[i]);
            result["military"].Add((uint)values[i + 1]);
            result["society"].Add((uint)values[i + 2]);
            result["technology"].Add((uint)values[i + 3]);
            result["total"].Add((uint)values[i + 4]);
        }

        return new Scores {
            Economy = values[values.Count - 6],
            Military = values[values.Count - 5],
            Society = values[values.Count - 4],
            Technology = values[values.Count - 3],
            Total = values[values.Count - 2]
        };
    }

    public Parser(bool debugEnabled) {
        debug = debugEnabled;
    }

    public PlayerSummary[] Call(MemoryStream replayData)
    {
        ChunkyFile replay = ChunkyFile.FromStream(replayData);

        List<PlayerSummary> result = new List<PlayerSummary>();

        foreach (ChunkyNode rootNode in replay.RootNodes) {
            ChunkHeader header = rootNode.Header;

            Traverse(result, rootNode, 0);
        }

        return result.ToArray();
    }

    void Traverse(List<PlayerSummary> result, IChunkyNode node, int depth) {
        var indentation = new string(' ', depth * 2);

        if (debug) {
            Console.WriteLine($"{indentation}node (type: {node.Header.Type}, name: {node.Header.Name}, version: {node.Header.Version}, length: {node.Header.Length}, path: {node.Header.Path})");
            Console.WriteLine($"{indentation}{node.Header.Type}");
        }

        if (node.Header.Type == "FOLD") {
            var folder = (IChunkyFolderNode) node;

            foreach (var child in folder.Children) {
                Traverse(result, child, depth + 1);
            }
        } else if (node.Header.Type == "DATA") {
            var data = (IChunkyDataNode) node;

            ProcessDataNode(result, data, depth);
        } else {
            Console.WriteLine($"{indentation}unhandled type: {node.Header.Type}");
        }
    }

    void ProcessDataNode(List<PlayerSummary> result, IChunkyDataNode node, int depth) {
        var indentation = new string(' ', depth * 2);
        var data = node.GetData();

        var bytes = data.ToArray();

        if (debug) {
            var outPath = $"output/{fileCounter++}_{node.Header.Name}.bin";
            File.WriteAllBytes(outPath, bytes);
            Console.WriteLine($"{indentation}{node.Header.Name}");
        }

        var bytesSpan = bytes.AsSpan();

        if (node.Header.Name == "STLS") {
            ProcessResources(bytesSpan);
        } else if (node.Header.Name == "STPD") {
            var buildOrder = ProcessBuildOrder(bytesSpan);
            var (resourcesV1, resourcesV2) = ProcessResources(bytesSpan);
            var scores = ProcessScores(bytesSpan, resourcesV2);
            var player = ProcessPlayer(bytesSpan);

            var playerSummary = new PlayerSummary(
                player.Name,
                new Dictionary<string, List<uint>>(),
                scores,
                resourcesV2,
                // resourcesV1,
                buildOrder
            );

            result.Add(playerSummary);
        } else if (node.Header.Name == "STLP") {
            var (name, timestamps) = ProcessActions(bytesSpan);
            var currentPlayerSummary = result.Last();

            if (shouldReturnAction(name)) {
                currentPlayerSummary.Actions.Add(name, timestamps);
            }
        } else {
            // Console.WriteLine($"unhandled data node: {node.Header.Name}");
        }
    }

    bool shouldReturnAction(string actionName) {
        return actionName.StartsWith("upgrade_") || actionName.EndsWith("_age") || actionName.Contains("relic") || actionName.Contains("holy");
    }

    (string, List<uint>) ProcessActions(Span<byte> bytes) {
        var actions = new List<BuildOrderAction>();
        var timestamps = new List<uint>();

        var name = ParseString(bytes.Slice(5));

        var firstTimestampPosition = 5 + name.Length + 8;

        var firstAction = new BuildOrderAction {
            Timestamp = BitConverter.ToUInt32(bytes.Slice(firstTimestampPosition)),
            Foo1 = BitConverter.ToInt32(bytes.Slice(firstTimestampPosition - 4)),
            Foo2 = BitConverter.ToInt32(bytes.Slice(firstTimestampPosition + 4))
        };
        actions.Add(firstAction);

        var secondTimestampPosition = firstTimestampPosition + 16;

        if (secondTimestampPosition > bytes.Length) {
            timestamps.Add(firstAction.Timestamp);
            return (name, timestamps);
        }

        var secondAction = new BuildOrderAction {
            Timestamp = BitConverter.ToUInt32(bytes.Slice(secondTimestampPosition)),
            Foo1 = ParseFloat(bytes.Slice(secondTimestampPosition - 4), 0),
            Foo2 = ParseFloat(bytes.Slice(secondTimestampPosition - 8), 0)
        };
        timestamps.Add(secondAction.Timestamp);
        actions.Add(secondAction);

        var cursor = secondTimestampPosition + 12;
        while (cursor < bytes.Length) {
            var action = new BuildOrderAction {
                Timestamp = BitConverter.ToUInt32(bytes.Slice(cursor)),
                Foo1 = ParseFloat(bytes.Slice(cursor - 4), 0),
                Foo2 = ParseFloat(bytes.Slice(cursor - 8), 0)
            };

            timestamps.Add(action.Timestamp);
            actions.Add(action);

            cursor += 12;
        }

        return (name, timestamps);
    }

    float ParseFloat(Span<byte> bytes, int position) {
        if (bytes.Length < position + 4) {
            return Single.NaN;
        }

        var segment = bytes.Slice(position, 4);
        var value = BitConverter.ToSingle(segment);
        if (float.IsFinite(value)) {
            return value;
        } else {
            return Single.NaN;
        }
    }

    Player ProcessPlayer(Span<byte> bytes) {
        var playerNameLength = BitConverter.ToUInt32(bytes[4..8]);

        var playerName = "";

        for (int i = 0; i < playerNameLength; i++) {
            var charBytes = bytes.Slice(8 + i * 2, 2);
            var ch = BitConverter.ToChar(charBytes);

            playerName += ch;
        }

        if (debug) {
            Console.WriteLine($"Player: {playerName}");
        }

        return new Player(playerName);
    }

    string ParseUnicodeString(Span<byte> bytes) {
        var playerNameLength = BitConverter.ToUInt32(bytes.Slice(4, 4));

        var value = "";

        for (int i = 0; i < playerNameLength; i++) {
            var charBytes = bytes.Slice(8 + i * 2, 2);

            if (charBytes[0] == 0 && charBytes[1] == 0) {
                break;
            }

            var ch = BitConverter.ToChar(charBytes);

            value += ch;
        }

        return value;
    }

    BuildOrderEntry[] ProcessBuildOrder(Span<byte> bytes) {
        var positions = FindByteSequencePositions(bytes, new byte[] {0x69, 0x63, 0x6F, 0x6E, 0x73}); // icons

        var buildOrderMap = new Dictionary<string, BuildOrderEntry>();
        var spawnIdMap = new Dictionary<string, byte>();

        var scanningInitial = true;

        foreach (var position in positions) {
            var segment = bytes.Slice(position - 21);
            var timestampSegment = bytes.Slice(position - 8);

            var timestamp = BitConverter.ToUInt32(timestampSegment[0..4]);
            var icon = ParseString(bytes.Slice(position)).Replace('\\', '/');
            var id = ParseUnicodeString(bytes.Slice(position + icon.Length - 2));
            var typeId = FindByteSequenceValueByte("$ 0", timestampSegment, new byte[] {0x24, 0x00, 0x30, 0x00});
            var normalizedIcon = Regex.Replace(icon, @"_\d$", ",");

            if (!spawnIdMap.ContainsKey(normalizedIcon)) {
                spawnIdMap.Add(normalizedIcon, typeId);
            }

            if (scanningInitial && timestamp != 0) {
                scanningInitial = false;

                foreach (var entry in buildOrderMap.Values) {
                    entry.Finished.RemoveRange(entry.Finished.Count / 2, entry.Finished.Count / 2);
                    entry.Constructed.RemoveRange(entry.Constructed.Count / 2, entry.Constructed.Count / 2);
                    entry.Packed.RemoveRange(entry.Packed.Count / 2, entry.Packed.Count / 2);
                }
            }

            var type = BuildOrderEntryType.Unknown;

            if (icon.Contains("/buildings/")) {
                type = BuildOrderEntryType.Building;
            } else if (icon.Contains("/units/")) {
                type = BuildOrderEntryType.Unit;
            } else if (icon.Contains("/upgrades/") || icon.Contains("/uprades/")) {
                type = BuildOrderEntryType.Upgrade;
            } else if (icon.Contains("/animals/")) {
                type = BuildOrderEntryType.Animal;
            } else if (icon.Contains("/age/")) {
                type = BuildOrderEntryType.Age;
            }

            buildOrderMap.TryAdd(
                icon,
                new BuildOrderEntry {
                    Id = id,
                    Icon = icon,
                    Type = type,
                    Finished = new List<uint>(),
                    Constructed = new List<uint>(),
                    Packed = new List<uint>(),
                    Unpacked = new List<uint>(),
                    Transformed = new List<uint>(),
                    Destroyed = new List<uint>(),
                    Unknown = new Dictionary<uint, List<uint>>()
                }
            );
            var buildOrderEntry = buildOrderMap[icon];

            if (typeId == 1) {
                buildOrderEntry.Finished.Add(timestamp);
            } else if (typeId == 2) {
                buildOrderEntry.Destroyed.Add(timestamp);
            } else if (typeId == 3) {
                if (type == BuildOrderEntryType.Building) {
                    buildOrderEntry.Packed.Add(timestamp);
                } else {
                    buildOrderEntry.Finished.Add(timestamp);
                }
            } else if (typeId == 4) {
                buildOrderEntry.Destroyed.Add(timestamp);
            } else if (typeId == 5) {
                var isUnpacking =
                    buildOrderEntry.Packed.Count > 0
                    && (buildOrderEntry.Constructed.Count == 0 || ((buildOrderEntry.Packed.Last() > buildOrderEntry.Constructed.Last())))
                    && (buildOrderEntry.Unpacked.Count == 0 || (buildOrderEntry.Packed.Last() > buildOrderEntry.Unpacked.Last()));

                if (isUnpacking) {
                    buildOrderEntry.Unpacked.Add(timestamp);
                } else {
                    buildOrderEntry.Constructed.Add(timestamp);
                }
            } else if (typeId == 6) {
                if (type == BuildOrderEntryType.Building) {
                    buildOrderEntry.Destroyed.Add(timestamp);
                } else {
                    // ignore as it already exists in the 4 list for siege
                    buildOrderEntry.Unknown.TryAdd(typeId, new List<uint>());
                    buildOrderEntry.Unknown[typeId].Add(timestamp);
                }
            } else if (typeId == 8) {
                buildOrderEntry.Finished.Add(timestamp);
            } else if (typeId == 12) {
                buildOrderEntry.Finished.Add(timestamp);
            } else {
                buildOrderEntry.Unknown.TryAdd(typeId, new List<uint>());
                buildOrderEntry.Unknown[typeId].Add(timestamp);
            }
        }

        return buildOrderMap.Values.ToArray();
    }

    (Resources[], Dictionary<string, List<uint>>) ProcessResources(Span<byte> bytes) {
        var resourcesV2 = new Dictionary<string, List<uint>>();

        var positions = FindByteSequencePositions(bytes, ByteStrings.ACTION);

        var allResourcesSnapshots = new List<Resources>();
        var relevantResourcesSnapshots = new List<Resources>();

        foreach (var position in positions) {
            var segment = bytes.Slice(position - 12);

            var record = new Resources(
                BitConverter.ToUInt32(segment),
                FindByteSequenceValue("action", segment, ByteStrings.ACTION),
                FindByteSequenceValue("command", segment, ByteStrings.COMMAND),
                FindByteSequenceValue("food", segment, ByteStrings.FOOD),
                FindByteSequenceValue("gold", segment, ByteStrings.GOLD),
                FindByteSequenceValue("militia_hre", segment, ByteStrings.MILITIA_HRE),
                FindByteSequenceValue("popcap", segment, ByteStrings.POPCAP),
                FindByteSequenceValue("stone", segment, ByteStrings.STONE),
                FindByteSequenceValue("wood", segment, ByteStrings.WOOD)
            );

            allResourcesSnapshots.Add(record);

            if (record.Action == -1 || debug) {
                relevantResourcesSnapshots.Add(record);
            }

            if (record.Action == -1) {
                resourcesV2.TryAdd("timestamps", new List<uint>());
                resourcesV2.TryAdd("food", new List<uint>());
                resourcesV2.TryAdd("gold", new List<uint>());
                resourcesV2.TryAdd("stone", new List<uint>());
                resourcesV2.TryAdd("wood", new List<uint>());

                resourcesV2["timestamps"].Add(record.Timestamp);
                resourcesV2["food"].Add((uint) Math.Round(record.Food));
                resourcesV2["gold"].Add((uint) Math.Round(record.Gold));
                resourcesV2["stone"].Add((uint) Math.Round(record.Stone));
                resourcesV2["wood"].Add((uint) Math.Round(record.Wood));
            }
        }

        if (debug) {
            foreach (var resources in allResourcesSnapshots) {
                Console.WriteLine($"{resources}");
            }
        }

        return (relevantResourcesSnapshots.ToArray(), resourcesV2);
    }

    string ParseString(Span<byte> bytes) {
        var chars = new List<char>();

        for (int i = 0; i < 100; i++) {
            if (bytes[i] == 9 || bytes[i] == 0) {
                break;
            }
            chars.Add((char) bytes[i]);
        }

        return new string(chars.ToArray());
    }

    float FindByteSequenceValue(string label, Span<byte> bytes, byte[] sequence) {
        int maxDistance = 10_000;

        for (int i = 0; i < bytes.Length - sequence.Length; i++) {
            if (i > maxDistance) {
                break;
            }

            bool found = true;
            int j = 0;

            for (j = 0; j < sequence.Length; j++) {
                if (bytes[i + j] != sequence[j]) {
                    found = false;
                    break;
                }
            }

            if (found) {
                var segment = bytes.Slice(i + j, 4);
                return BitConverter.ToSingle(segment);
            }

        }

        return 0;
    }

    byte FindByteSequenceValueByte(string label, Span<byte> bytes, byte[] sequence) {
        for (int i = 0; i < bytes.Length - sequence.Length; i++) {
            bool found = true;
            int j = 0;

            for (j = 0; j < sequence.Length; j++) {
                if (bytes[i + j] != sequence[j]) {
                    found = false;
                    break;
                }
            }

            if (found) {
                var segment = bytes.Slice(i + j, 4);
                return bytes[i + j];
                // return BitConverter.ToSingle(segment);
            }

        }

        return 0;
    }

    List<int> FindByteSequencePositions(Span<byte> bytes, byte[] sequence) {
        var positions = new List<int>();

        for (int i = 0; i < bytes.Length - sequence.Length; i++) {
            bool found = true;
            int j = 0;

            for (j = 0; j < sequence.Length; j++) {
                if (bytes[i + j] != sequence[j]) {
                    found = false;
                    break;
                }
            }

            if (found) {
                positions.Add(i);
            }
        }

        return positions;
    }
}
