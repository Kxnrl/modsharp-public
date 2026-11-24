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
        var manager = _sharedSystem.GetConVarManager();
        if (manager.CreateConVar("example_value", 0, "Example integer value.", ConVarFlags.Release) is not { } value)
        {
            return false;
        }

        value.Set(1);
        Console.WriteLine($"example_value = {value.GetInt32()}");

        if (manager.FindConVar("sv_cheats") is { } cheats)
        {
            // Read an existing ConVar without changing the server's cheat setting.
            Console.WriteLine($"sv_cheats = {cheats.GetBool()}");
        }

        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "ConVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}
