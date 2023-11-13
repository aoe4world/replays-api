using Microsoft.AspNetCore.Mvc;
using AoE4WorldReplaysParser.Services;
using System.Net.Http;
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace AoE4WorldReplaysAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DebugController : ControllerBase {
    private readonly ILogger<DebugController> _logger;

    public DebugController(ILogger<DebugController> logger) {
        _logger = logger;
    }

    [HttpGet(Name = "GetDebug")]
    public AoE4WorldReplaysParser.Services.Parser.PlayerSummary[] Get(string path) {
        System.IO.DirectoryInfo di = new DirectoryInfo("output");

        foreach (FileInfo file in di.GetFiles()) {
            file.Delete();
        }

        var dataStream = new MemoryStream((System.IO.File.ReadAllBytes(path)));

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var parser = new AoE4WorldReplaysParser.Services.Parser(true);
        var result = parser.Call(dataStream);
        stopwatch.Stop();

        Response.Headers.Add("X-Parser-Elapsed", stopwatch.ElapsedMilliseconds.ToString());

        return result;
    }
}
