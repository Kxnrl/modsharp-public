using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;

namespace GameListenerExample;

public sealed class GameListener : IModSharpModule, IGameListener
{
    private readonly ISharedSystem _sharedSystem;

    public GameListener(ISharedSystem sharedSystem,
        string                        dllPath,
        string                        sharpPath,
        Version                       version,
        IConfiguration                coreConfiguration,
        bool                          hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // install listener, any class what inherits IGameListener can be a listener.
        _sharedSystem.GetModSharp().InstallGameListener(this);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public string DisplayName   => "GameListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // all callbacks are optional to implement, you can just implement what you need.

    // safe to get sv/globals
    public void OnServerInit()
    {
        Console.WriteLine("[OnServerInit] You can get GlobalVars at here.");
    }

    // safe to execute .cfg
    public void OnServerSpawn()
    {
        Console.WriteLine("[OnServerSpawn]");
    }

    public void OnServerActivate()
    {
        Console.WriteLine("[OnServerActivate] Or you can treat this as OnMapStart. This is after OnGameInit");
    }

    public void OnResourcePrecache()
    {
        // you only can precache resources in this callback!!!
        Console.WriteLine("[OnResourcePrecache] Precaching resources...");
        _sharedSystem.GetModSharp().PrecacheResource("models/foo/bar.vmdl");
    }

    // safe to get GameRules
    public void OnGameInit()
    {
        Console.WriteLine("[OnGameInit] You can treat this is OnMapStart if you want to migrate from CS#/SM");
    }

    public void OnGamePostInit()
    {
        Console.WriteLine("[OnGamePostInit]");
    }

    public void OnGameActivate()
    {
        Console.WriteLine("[OnGameActivate]");
    }

    public void OnGameDeactivate()
    {
        Console.WriteLine("[OnGameDeactivate] You can treat this is OnMapEnd.");
    }

    public void OnGamePreShutdown()
    {
        Console.WriteLine("[OnGamePreShutdown]");
    }

    public void OnGameShutdown()
    {
        Console.WriteLine("[OnGameShutdown] or you can use it as OnMapEnd. But in this term you cannot use GameRules");
    }

    public void OnRoundRestart()
    {
        Console.WriteLine("[OnRoundRestart] This and OnRoundRestarted can replace round_prestart/round_start stuff.");
    }

    public void OnRoundRestarted()
    {
        Console.WriteLine("[OnRoundRestarted] This and OnRoundRestart can replace round_prestart/round_start stuff.");
    }

    public ECommandAction ConsoleSay(string message)
    {
        Console.WriteLine($"[ConsoleSay] {message}");

        return ECommandAction.Skipped;
    }

    // just write it according to the example.
    int IGameListener.ListenerVersion => IGameListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IGameListener.ListenerPriority => 0;
}
