using Microsoft.AspNetCore.Mvc;
using AoE4WorldReplaysParser.Services;
using System.Net.Http;
using System;
using System.IO;
using System.IO.Compression;

namespace AoE4WorldReplaysAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class SummaryController : ControllerBase
{
    private readonly ILogger<SummaryController> _logger;

    public SummaryController(ILogger<SummaryController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetSummary")]
    public async Task<AoE4WorldReplaysParser.Services.Parser.PlayerSummary[]> Get(string url)
    {
        var compressedData = await (new HttpClient()).GetStreamAsync(url);

        MemoryStream dataStream = new MemoryStream();
        using var decompressor = new GZipStream(compressedData, CompressionMode.Decompress);
        decompressor.CopyTo(dataStream);
        dataStream.Seek(0, SeekOrigin.Begin);
        var parser = new AoE4WorldReplaysParser.Services.Parser(false);
        var result = parser.Call(dataStream);

        return result;
    }
}
