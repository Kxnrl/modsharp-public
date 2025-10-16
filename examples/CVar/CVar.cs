using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Objects;

namespace CVar;

// ReSharper disable once UnusedMember.Global
internal class CVar : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;
    public CVar(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);

        _sharedSystem = sharedSystem;
    }


    public bool Init()
    {
        // That's it. Very easy.
        _sharedSystem.GetConVarManager().CreateConVar("my_cvar", 0, "This is my cvar.");
        if (_sharedSystem.GetConVarManager().FindConVar("sv_cheats") is { } cheats)
        {
            cheats.SetString("1");
        }
        return true;
    }



    public void Shutdown()
    {
    }

    public string DisplayName => "CVar Example";
    public string DisplayAuthor => "ModSharp dev team";
}
