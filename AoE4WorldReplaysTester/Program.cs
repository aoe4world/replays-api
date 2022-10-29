
using System.Diagnostics;
using AoE4WorldReplaysParser;

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

var stopwatch = new Stopwatch();
stopwatch.Start();
var parser = new ReplaySummaryParser();
foreach (var file in Directory.GetFiles(replayDir))
{
    if (file.EndsWith(".gz"))
        continue;

    if (!file.Contains("_summary"))
        continue;

    using (var dataStream = new FileStream(file, FileMode.Open, FileAccess.Read))
    {
        if (!ReplaySummaryParser.IsReplaySummaryFile(dataStream))
            continue;

        parser.Load(dataStream, Path.GetFileName(file));
        parser.Parse();

        var summary = new AoE4WorldReplaysParser.Summary.GameSummaryGenerator().GenerateSummary(parser.Summary);
    }
}

parser.GenerateReport(reportsDir);