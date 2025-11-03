using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Extensions.GameEventManager;
using Sharp.Shared;
using Sharp.Shared.Abstractions;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace GameEventManagerExample;

public sealed class GameEventManagerExample : IModSharpModule
{
    private readonly IServiceProvider  _provider;
    private readonly IGameEventManager _gameEventManager;

    public GameEventManagerExample(ISharedSystem sharedSystem,
        string                                   dllPath,
        string                                   sharpPath,
        Version                                  version,
        IConfiguration                           coreConfiguration,
        bool                                     hotReload)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sharedSystem);
        services.AddGameEventManager();

        _provider         = services.BuildServiceProvider();
        _gameEventManager = _provider.GetRequiredService<IGameEventManager>();
    }

    public bool Init()
    {
        _provider.LoadAllSharpExtensions();

        _gameEventManager.HookEvent("player_changename", OnPlayerChangeName);
        _gameEventManager.ListenEvent("cs_intermission", OnIntermission);
        _gameEventManager.HookEvent("player_death", OnPlayerDeath);

        return true;
    }

    public void Shutdown()
    {
        _provider.ShutdownAllSharpExtensions();
    }

    // block player_changename event
    private HookReturnValue<bool> OnPlayerChangeName(IGameEvent ev, ref bool serverOnly)
    {
        // [[unlikely]] this should never happen
        if (ev is not IEventPlayerChangeName e)
        {
            return new HookReturnValue<bool>();
        }

        Console.WriteLine($"{e.OldName} changed name to {e.NewName}");

        // block event fire
        return new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride);
    }

    // listen cs_intermission event
    private void OnIntermission(IGameEvent e)
    {
        Console.WriteLine($"[OnIntermission] {e.Name}");
    }

    // this example will replace original player_death event with a modified one and broadcast it to all clients,
    // the original event will be silenced to clients, only server will receive it.
    private HookReturnValue<bool> OnPlayerDeath(IGameEvent e, ref bool serverOnly)
    {
        IGameEvent? clone = null;
        var         sv    = _provider.GetRequiredService<ISharedSystem>().GetModSharp().GetIServer();

        try
        {
            clone = _gameEventManager.CloneEvent(e)
                    ?? throw new InvalidOperationException("Failed to clone event");

            // modify kill feed icon
            clone.SetString("weapon", "SawnLake");

            foreach (var client in sv.GetGameClients())
            {
                if (client is { SignOnState: SignOnState.Full, IsFakeClient: false })
                {
                    clone.FireToClient(client);
                }
            }

            // hide original event
            serverOnly = true;

            return new HookReturnValue<bool>();
        }
        finally
        {
            clone?.Dispose();
        }
    }

    public string DisplayName   => "GameEventManager Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
