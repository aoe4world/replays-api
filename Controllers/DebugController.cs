using Microsoft.AspNetCore.Mvc;
using AoE4WorldReplayParser.Services;
using System.Net.Http;
using System;
using System.IO;
using System.IO.Compression;

namespace AoE4WorldReplaysAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DebugController : ControllerBase {
    private readonly ILogger<DebugController> _logger;

    public DebugController(ILogger<DebugController> logger) {
        _logger = logger;
    }

    [HttpGet(Name = "GetDebug")]
    public Parser.PlayerSummary[] Get(string path) {
        System.IO.DirectoryInfo di = new DirectoryInfo("output");

        foreach (FileInfo file in di.GetFiles()) {
            file.Delete();
        }

        var dataStream = new MemoryStream((System.IO.File.ReadAllBytes(path)));

        var parser = new Parser(true);
        var result = parser.Call(dataStream);

        return result;
    }
}
