AOE4 Replay Parser
==================


Contributing
------------

See `docs` for two `.bt` file which are 010 Editor Template files. Any changes in the parser should be reflected both in the template file and the actual parser before pushing.
Also note that most of the template has correct struct layouts, if there's a field missing it's likely due to replay version changes. Do not blindly add the field, include a version check to make sure the parser continues to function with older replay files.

The `examples` contains a number of historic replay files of various versions.

Testing
-------

The parser main entrypoint for the production environment is the API. But there's a separate commandline testing tool to help with improving the parsing and analysing replay file structures.

**With Visual Studio**

Open the project and run `AoE4WorldReplaysTester` with the appropriate commandline parameter to point to a replay directory (or `examples`).
The tool will scan the directory for replays and generate a reports directory.

**With VSCode and devcontainer**

Open the directory as Dev Container in VSCode on a machine with docker, this will start a docker container with dotnet preinstalled.
```
> cd AoE4WorldReplaysTester
> dotnet run
```

This will build and run the tester application.

