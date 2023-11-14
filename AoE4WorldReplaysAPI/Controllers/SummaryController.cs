using Microsoft.AspNetCore.Mvc;
using AoE4WorldReplaysParser.Services;
using System.Net.Http;
using System;
using System.IO;
using System.IO.Compression;
using AoE4WorldReplaysParser.Summary;
using AoE4WorldReplaysAPI.Services;

namespace AoE4WorldReplaysAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class SummaryController : ControllerBase
{
    private readonly ILogger<SummaryController> _logger;
    private readonly HttpClient _httpClient;

    public SummaryController(ILogger<SummaryController> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    [HttpGet(Name = "GetSummary")]
    public async Task<AoE4WorldReplaysParser.Services.Parser.PlayerSummary[]> GetSummaryCompat(string url)
    {
        var name = Path.GetFileName(new Uri(url).AbsolutePath);

        _logger.LogInformation("Processing summary {0} from url {0}", name, url);

        var compressedData = await _httpClient.GetStreamAsync(url);

        MemoryStream dataStream = new MemoryStream();
        using (var decompressor = new GZipStream(compressedData, CompressionMode.Decompress))
        {
            decompressor.CopyTo(dataStream);
            dataStream.Seek(0, SeekOrigin.Begin);
        }

        var parser = new AoE4WorldReplaysParser.ReplaySummaryParser(dataStream, name);
        parser.Parse();

        var replaySummary = parser.Summary;
        var gameSummary = new GameSummaryGenerator().GenerateSummary(replaySummary);

        var result = CompatConverter.Convert(replaySummary, gameSummary);

        return result;
    }

    [HttpGet("file")]
    public async Task<AoE4WorldReplaysParser.Services.Parser.PlayerSummary[]> GetSummaryFile(string path)
    {
        var compressedData = new FileStream(path, FileMode.Open, FileAccess.Read);

        MemoryStream dataStream = new MemoryStream();
        using var decompressor = new GZipStream(compressedData, CompressionMode.Decompress);
        await decompressor.CopyToAsync(dataStream);
        dataStream.Seek(0, SeekOrigin.Begin);
        var parser = new AoE4WorldReplaysParser.Services.Parser(false);
        var result = parser.Call(dataStream);

        return result;
    }

    [HttpGet("old")]
    public async Task<AoE4WorldReplaysParser.Services.Parser.PlayerSummary[]> GetSummaryOld(string url)
    {
        var compressedData = await _httpClient.GetStreamAsync(url);

        MemoryStream dataStream = new MemoryStream();
        using var decompressor = new GZipStream(compressedData, CompressionMode.Decompress);
        await decompressor.CopyToAsync(dataStream);
        dataStream.Seek(0, SeekOrigin.Begin);
        var parser = new AoE4WorldReplaysParser.Services.Parser(false);
        var result = parser.Call(dataStream);

        return result;
    }

    [HttpGet("newfile")]
    public async Task<object> GetGameSummaryFile(string path)
    {
        var compressedData = new FileStream(path, FileMode.Open, FileAccess.Read);

        MemoryStream dataStream = new MemoryStream();
        using (var decompressor = new GZipStream(compressedData, CompressionMode.Decompress))
        {
            decompressor.CopyTo(dataStream);
            dataStream.Seek(0, SeekOrigin.Begin);
        }

        var name = Path.GetFileName(path);
        var parser = new AoE4WorldReplaysParser.ReplaySummaryParser(dataStream, name);
        parser.Parse();

        var replaySummary = parser.Summary;
        var gameSummary = new GameSummaryGenerator().GenerateSummary(replaySummary);

        return new
        {
            gameSummary = gameSummary,
            replaySummary = replaySummary
        };
    }

    [HttpGet("new")]
    public async Task<object> GetGameSummary(string url)
    {
        var compressedData = await (new HttpClient()).GetStreamAsync(url);

        MemoryStream dataStream = new MemoryStream();
        using (var decompressor = new GZipStream(compressedData, CompressionMode.Decompress))
        {
            decompressor.CopyTo(dataStream);
            dataStream.Seek(0, SeekOrigin.Begin);
        }

        var name = Path.GetFileName(new Uri(url).AbsolutePath);
        var parser = new AoE4WorldReplaysParser.ReplaySummaryParser(dataStream, name);
        parser.Parse();

        var replaySummary = parser.Summary;
        var gameSummary = new GameSummaryGenerator().GenerateSummary(replaySummary);

        return new
        {
            gameSummary = gameSummary,
            replaySummary = replaySummary
        };
    }
}
