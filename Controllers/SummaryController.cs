using Microsoft.AspNetCore.Mvc;
using AoE4WorldReplayParser.Services;
using System.Net.Http;
using System;
using System.IO;
using System.IO.Compression;

namespace AoE4WorldReplaysAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class SummaryController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<SummaryController> _logger;

    public SummaryController(ILogger<SummaryController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetSummary")]
    public async Task<Parser.PlayerSummary[]> Get(string url)
    {
        // var replayDataUrl = "https://rl0aoelivemk2blob.blob.core.windows.net/cloudfiles/9310721/aoelive_/age4/replay/windows/4.0.0/12973/M_33896209_48674bfc7be50002ecbae63c9f8cc3d9077d09e2bcb8ead1b373203933db2d66.gz?sig=VmWEvslRorZrWz3gbBzklLkg7j0%2FZUQPtYOw70cm5Gk%3D&se=2022-04-25T12%3A27%3A07Z&sv=2019-02-02&sp=r&sr=b";

        var compressedData = await (new HttpClient()).GetStreamAsync(url);


        var fileName = "replay";
        using FileStream outputFileStream = System.IO.File.Create(fileName);
        using var decompressor = new GZipStream(compressedData, CompressionMode.Decompress);
        decompressor.CopyTo(outputFileStream);
        outputFileStream.Close();

        // using var decompressor = new GZipStream(compressedData, CompressionMode.Decompress);
        // var outputFileStream = new MemoryStream();
        // decompressor.CopyTo(outputFileStream);

        // var result = new Parser.PlayerSummary[] {};

        // using (var bigStream = new GZipStream(compressedData, CompressionMode.Decompress))
        // using (var bigStreamOut = new MemoryStream())
        // {
        //     bigStream.CopyTo(bigStreamOut);
        //     // result = big
        //     // output = Encoding.UTF8.GetString(bigStreamOut.ToArray());
        //     result = Parser.Call(bigStreamOut);
        // }

        var fileStream = new MemoryStream((System.IO.File.ReadAllBytes(fileName)));


        var result = Parser.Call(fileStream);

        return result;
    }
}
