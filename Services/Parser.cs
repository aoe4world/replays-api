using AOEMods.Essence.Chunky.Core;
using AOEMods.Essence.Chunky.Graph;
using System.Text.RegularExpressions;

namespace AoE4WorldReplayParser.Services;

public class Parser
{
    public record struct PlayerSummary(
        string Name,
        Scores Scores,
        Dictionary<string, List<uint>> Resources,
        // Resources[] ResourcesOld,
        BuildOrderEntry[] BuildOrder
    );

    public readonly record struct Player(
        string Name
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
        float TotalScore, // 226 vs 374
        float Military, // 14 vs 42
        float Economy, // 72 vs 192
        float Technology, // 0 vs 0
        float Society, // 140 vs 140
        float[] Foos
        // float Foo1,
        // float Foo2,
        // float Foo3,
        // float Foo4,
        // float Foo5,
        // float Foo6
    );

    Scores ProcessScores(byte[] bytes) {
        int firstIconsPosition = FindByteSequencePositions(bytes, new byte[] {0x69, 0x63, 0x6F, 0x6E, 0x73}).First();
        int previousWoodPosition = FindByteSequencePositions(bytes, new byte[] {119, 111, 111, 100}).Where(p => p < firstIconsPosition).Last();
        int startPosition = previousWoodPosition + 20;
        int cursor = startPosition;

        var values = new List<float>();

        for (int i = 0; i < 15; i++) {
            values.AddRange(new float[] {
                ParseFloat(bytes, cursor),
                ParseFloat(bytes, cursor += 4),
                ParseFloat(bytes, cursor += 4),
                ParseFloat(bytes, cursor += 4),
                ParseFloat(bytes, cursor += 4),
                BitConverter.ToInt32(bytes, cursor += 4),
            });
            cursor += 4;
        }

        for (int i = 0; i < 25; i++) {
            values.AddRange(new float[] {
                BitConverter.ToInt32(bytes, cursor),
            });
            cursor += 4;
        }

        var scores = new Scores {
            // values[4],
            TotalScore = values[82],
            // values[1],
            Military = values[85],
            // Economy = values[0],
            Economy = values[84],
            // Technology = values[3],
            Technology = values[87],
            // Society = values[2],
            Society = values[86],
            Foos = debug ? values.ToArray() : new float[0]
        };

        return scores;
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

        Console.WriteLine($"{indentation}node (type: {node.Header.Type}, name: {node.Header.Name}, version: {node.Header.Version}, length: {node.Header.Length}, path: {node.Header.Path})");
        Console.WriteLine($"{indentation}{node.Header.Type}");

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
        }

        Console.WriteLine($"{indentation}{node.Header.Name}");

        if (node.Header.Name == "STLS") {
            ProcessResources(bytes);
        } else if (node.Header.Name == "STPD") {
            var buildOrder = ProcessBuildOrder(bytes);
            var (resourcesV1, resourcesV2) = ProcessResources(bytes);
            var scores = ProcessScores(bytes);

            var playerSummary = new PlayerSummary(
                ProcessPlayer(bytes).Name,
                scores,
                resourcesV2,
                // resourcesV1,
                buildOrder
            );

            result.Add(playerSummary);
        } else {
            // Console.WriteLine($"unhandled data node: {node.Header.Name}");
        }
    }

    float ParseFloat(byte[] bytes, int position) {
        var segment = bytes[(position)..(position + 4)];
        var value = BitConverter.ToSingle(segment);
        if (float.IsFinite(value)) {
            return value;
        } else {
            return 9999999999.0f;
        }
    }

    Player ProcessPlayer(byte[] bytes) {
        var playerNameLength = BitConverter.ToUInt32(bytes[4..8]);

        var playerName = "";

        for (int i = 0; i < playerNameLength; i++) {
            var charBytes = bytes[(8 + i * 2)..(8 + (i + 1) * 2)];
            var ch = BitConverter.ToChar(charBytes);

            playerName += ch;
        }

        Console.WriteLine($"Player: {playerName}");

        return new Player(playerName);
    }

    string ParseUnicodeString(byte[] bytes) {
        var playerNameLength = BitConverter.ToUInt32(bytes[4..8]);

        var value = "";

        for (int i = 0; i < playerNameLength; i++) {
            var charBytes = bytes[(8 + i * 2)..(8 + (i + 1) * 2)];

            if (charBytes[0] == 0 && charBytes[1] == 0) {
                break;
            }

            var ch = BitConverter.ToChar(charBytes);

            value += ch;
        }

        return value;
    }

    BuildOrderEntry[] ProcessBuildOrder(byte[] bytes) {
        var positions = FindByteSequencePositions(bytes, new byte[] {0x69, 0x63, 0x6F, 0x6E, 0x73}); // icons

        var buildOrderMap = new Dictionary<string, BuildOrderEntry>();
        var spawnIdMap = new Dictionary<string, byte>();

        var scanningInitial = true;

        foreach (var position in positions) {
            var segment = bytes[(position - 21)..];
            var timestampSegment = bytes[(position - 8)..];

            var timestamp = BitConverter.ToUInt32(timestampSegment[0..4]);
            var icon = ParseString(bytes[position..]).Replace('\\', '/');
            var id = ParseUnicodeString(bytes[(position + icon.Length - 2)..]);
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

        Console.WriteLine();

        return buildOrderMap.Values.ToArray();
    }

    (Resources[], Dictionary<string, List<uint>>) ProcessResources(byte[] bytes) {
        var resourcesV2 = new Dictionary<string, List<uint>>();

        var positions = FindByteSequencePositions(bytes, new byte[] {0x61, 0x63, 0x74, 0x69, 0x6F, 0x6E});

        var allResourcesSnapshots = new List<Resources>();
        var relevantResourcesSnapshots = new List<Resources>();

        foreach (var position in positions) {
            var segment = bytes[(position - 12)..];

            var record = new Resources(
                BitConverter.ToUInt32(segment[0..4]),
                FindByteSequenceValue("action", segment, new byte[] {0x61, 0x63, 0x74, 0x69, 0x6F, 0x6E}),
                FindByteSequenceValue("command", segment, new byte[] {0x63, 0x6F, 0x6D, 0x6D, 0x61, 0x6E, 0x64}),
                FindByteSequenceValue("food", segment, new byte[] {102, 111, 111, 100}),
                FindByteSequenceValue("gold", segment, new byte[] {103, 111, 108, 100}),
                FindByteSequenceValue("militia_hre", segment, new byte[] {0x6D, 0x69, 0x6C, 0x69, 0x74, 0x69, 0x61, 0x5F, 0x68, 0x72, 0x65}),
                FindByteSequenceValue("popcap", segment, new byte[] {0x70, 0x6F, 0x70, 0x63, 0x61, 0x70}),
                FindByteSequenceValue("stone", segment, new byte[] {115, 116, 111, 110, 101}),
                FindByteSequenceValue("wood", segment, new byte[] {119, 111, 111, 100})
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

        foreach (var resources in allResourcesSnapshots) {
            Console.WriteLine($"{resources}");
        }

        return (relevantResourcesSnapshots.ToArray(), resourcesV2);
    }

    string ParseString(byte[] bytes) {
        var chars = new List<char>();

        for (int i = 0; i < 100; i++) {
            if (bytes[i] == 9 || bytes[i] == 0) {
                break;
            }
            chars.Add((char) bytes[i]);
        }

        return new string(chars.ToArray());
    }

    float FindByteSequenceValue(string label, byte[] bytes, byte[] sequence) {
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
                var segment = bytes[(i + j)..(i + j + 4)];
                return BitConverter.ToSingle(segment);
            }

        }

        return 0;
    }

    byte FindByteSequenceValueByte(string label, byte[] bytes, byte[] sequence) {
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
                var segment = bytes[(i + j)..(i + j + 4)];
                return bytes[i + j];
                // return BitConverter.ToSingle(segment);
            }

        }

        return 0;
    }

    int[] FindByteSequencePositions(byte[] bytes, byte[] sequence) {
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

        return positions.ToArray();
    }
}
