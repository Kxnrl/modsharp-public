using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace GameDataExample;

public sealed class GameDataExample : IModSharpModule
{
    private const string GameDataFile = "gamedata-example.games.jsonc";
    private readonly IGameData _gameData;
    private bool _registrationAttempted;

    public GameDataExample(ISharedSystem sharedSystem,
        string                          dllPath,
        string                          sharpPath,
        Version                         version,
        IConfiguration                  coreConfiguration,
        bool                            hotReload)
        => _gameData = sharedSystem.GetModSharp().GetGameData();

    public bool Init()
    {
        // This file and its namespaced key must be owned by this module alone.
        // Register tracks the path before native loading, even when loading fails.
        _registrationAttempted = true;
        _gameData.Register(GameDataFile);

        // Read the host's current values, which can include native auto-resolution.
        var signonStateOffset = _gameData.GetOffset("CServerSideClient::m_SignonState");
        var collisionRulesIndex = _gameData.GetVFuncIndex("CBaseEntity::CollisionRulesChanged");
        var builtInExport = _gameData.GetAddress("LoggingSystem_IsChannelEnabled");
        var customExport = _gameData.GetAddress("GameDataExample::LoggingSystem_IsChannelEnabled");

        Console.WriteLine($"Signon-state byte offset: {signonStateOffset}");
        Console.WriteLine($"CollisionRulesChanged vtable slot: {collisionRulesIndex}");
        Console.WriteLine($"Built-in logging export: 0x{builtInExport:X}");
        Console.WriteLine($"Custom logging export: 0x{customExport:X}");

        // Resolving an address does not determine its ABI; this sample does not call it.
        return true;
    }

    public void Shutdown()
    {
        // The loader also calls Shutdown after a failed Init.
        // Real hooks/call wrappers must stop using these keys before this point.
        if (_registrationAttempted)
        {
            _gameData.Unregister(GameDataFile);
            _registrationAttempted = false;
        }
    }

    public string DisplayName => "GameData Example";
    public string DisplayAuthor => "ModSharp dev team";
}
