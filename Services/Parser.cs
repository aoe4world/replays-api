using AOEMods.Essence.Chunky.Core;
using AOEMods.Essence.Chunky.Graph;

namespace AoE4WorldReplayParser.Services;

public class Parser
{
    public record struct PlayerSummary(
        Player Player,
        BuildOrderStep[] BuildOrder,
        BuildOrderEntry[] BuildOrderV2,
        Resources[] Resources
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

    public readonly record struct BuildOrderStep(
        uint Timestamp,
        string Icon,
        // uint Foo,
        string Type,
        float Foo
    );

    public record struct BuildOrderEntry(
        string Icon,
        List<uint> Spawned,
        List<uint> Destroyed
    );

    private bool debug;
    private int fileCounter = 0;

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
            var (buildOrderV1, buildOrderV2) = ProcessBuildOrder(bytes);
            var playerSummary = new PlayerSummary(
                ProcessPlayer(bytes),
                buildOrderV1,
                buildOrderV2,
                ProcessResources(bytes)
            );

            result.Add(playerSummary);
        } else {
            // Console.WriteLine($"unhandled data node: {node.Header.Name}");
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

    (BuildOrderStep[], BuildOrderEntry[]) ProcessBuildOrder(byte[] bytes) {
        var positions = FindByteSequencePositions(bytes, new byte[] {0x69, 0x63, 0x6F, 0x6E, 0x73}); // icons

        var buildOrder = new List<BuildOrderStep>();
        var buildOrderMap = new Dictionary<string, BuildOrderEntry>();
        var spawnIdMap = new Dictionary<string, byte>();

        foreach (var position in positions) {
            var segment = bytes[(position - 21)..];
            var timestampSegment = bytes[(position - 8)..];

            var timestamp = BitConverter.ToUInt32(timestampSegment[0..4]);
            var icon = ParseString(bytes[position..]);
            var typeId = FindByteSequenceValueByte("$ 0", timestampSegment, new byte[] {0x24, 0x00, 0x30, 0x00});

            if (!spawnIdMap.ContainsKey(icon)) {
                spawnIdMap.Add(icon, typeId);
            }

            var type = "";

            if (timestamp == 0) {
                type = "initial";
            } else if (spawnIdMap[icon] == typeId) {
                type = "spawn";
            } else {
                type = "death";
            }

            var step = new BuildOrderStep(
                timestamp,
                icon,
                // segment[0],
                type,
                typeId
            );

            buildOrder.Add(step);

            if (type != "initial") {
                buildOrderMap.TryAdd(icon, new BuildOrderEntry(icon, new List<uint>(), new List<uint>()));
                var buildOrderEntry = buildOrderMap[icon];

                if (type == "spawn") {
                    buildOrderEntry.Spawned.Add(timestamp);
                } else if (type == "death") {
                    buildOrderEntry.Destroyed.Add(timestamp);
                }
            }
        }

        foreach (var step in buildOrder) {
            Console.WriteLine($"{step}");
        }

        Console.WriteLine();

        return (buildOrder.ToArray(), buildOrderMap.Values.ToArray());
    }

    Resources[] ProcessResources(byte[] bytes) {
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
        }

        foreach (var resources in allResourcesSnapshots) {
            Console.WriteLine($"{resources}");
        }

        return relevantResourcesSnapshots.ToArray();
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
