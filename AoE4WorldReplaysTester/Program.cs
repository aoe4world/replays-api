
using System.Diagnostics;
using System.IO.Compression;
using AoE4WorldReplaysParser;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: Replay folder as first parameter");
    Environment.ExitCode = -1;
    return;
}


var replayDir = args[0];

if (!Directory.Exists(replayDir))
{
    Console.WriteLine($"Replay folder '{replayDir}' does not exist");
    Environment.ExitCode = -1;
    return;
}

var reportsDir = Path.Combine(replayDir, "reports");
var parser = new ReplaySummaryParser();

if (!Directory.Exists(reportsDir))
{
    Directory.CreateDirectory(reportsDir);
}

void WriteJsonObject(String filename, Object o)   {
    var options = new JsonSerializerOptions { WriteIndented = true };
    var jsonString = JsonSerializer.Serialize(o, options);
    File.WriteAllText(filename, jsonString);
}

var stopwatch = new Stopwatch();
stopwatch.Start();
foreach (var file in Directory.GetFiles(replayDir))
{
    if (file.EndsWith(".md"))
        continue;

    var fileName = Path.GetFileName(file);
    var fileDir = Path.GetDirectoryName(file);
    var outputDir = Path.Combine(fileDir, "output");
    System.IO.Directory.CreateDirectory(outputDir);
    var summaryParsedFile = Path.Combine(outputDir, $"{fileName}.summaryParsed.txt");
    var summaryFinalFile = Path.Combine(outputDir, $"{fileName}.summaryFinal.txt");

    using (var dataStream = new MemoryStream())
    {
        if (file.EndsWith(".gz"))
        {
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))    
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(dataStream);
                dataStream.Position = 0;
            }
        }
        else
        {
            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))    
            {
                fileStream.CopyTo(dataStream);
                dataStream.Position = 0;
            }
        }

        if (ReplaySummaryParser.IsReplaySummaryFile(dataStream))
        {
            try
            {
                parser.Load(dataStream, fileName);
                parser.Parse();
                var playerNames = string.Join(", ", parser.Summary.Players.Select(v => v.PlayerDetails.playerName));
                Console.WriteLine($"{fileName}: ReplaySummary for {playerNames}");

                WriteJsonObject(summaryParsedFile, parser.Summary);
                var summary = new AoE4WorldReplaysParser.Summary.GameSummaryGenerator().GenerateSummary(parser.Summary);

                WriteJsonObject(summaryFinalFile, summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{fileName}: ReplaySummary failed to parse: {ex.ToString()}");
            }
        }
        else if (ReplayFullParser.IsReplayFullFile(dataStream))
        {
            Console.WriteLine($"{fileName}: Full Replay file, further parsing not implemented");
        }
        else
        {
            Console.WriteLine($"{fileName}: Unsupported file type or corrupted replay file");
        }
    }
}

// We generate a bunch of csv files for each struct type, with all parsed replay summaries lobbed together, this allowed comparitive analysis of unknown fields.
parser.GenerateReport(reportsDir);