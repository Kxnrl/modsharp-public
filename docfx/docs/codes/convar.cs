using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;

namespace ConVarExample;

public sealed class ConVar : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    public ConVar(ISharedSystem sharedSystem,
        string                  dllPath,
        string                  sharpPath,
        Version                 version,
        IConfiguration          coreConfiguration,
        bool                    hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // That's it. Very easy.
        _sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.", ConVarFlags.Release);

        if (_sharedSystem.GetConVarManager().FindConVar("sv_cheats") is { } cheats)
        {
            cheats.Set(true);
        }

        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "ConVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}
