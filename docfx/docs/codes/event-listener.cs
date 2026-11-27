using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace EventListener;

public sealed class EventListener : IModSharpModule, IEventListener
{
    private readonly ISharedSystem _sharedSystem;

    public EventListener(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        var events = _sharedSystem.GetEventManager();

        // Install the listener, then register each event name you want dispatched.
        events.InstallEventListener(this);
        events.HookEvent("player_death");
        events.HookEvent("player_spawn");
        events.HookEvent("player_say");
        events.HookEvent("player_changename");

        return true;
    }

    public void Shutdown()
    {
        // Remove this module's listener before unloading or reloading it.
        _sharedSystem.GetEventManager().RemoveEventListener(this);
    }

    public string DisplayName   => "EventListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    public void FireGameEvent(IGameEvent e)
    {
        // This engine-owned event is only valid during the callback. Do not Fire or Dispose it.
        // use built-in event types for common events
        if (e is IEventPlayerDeath death)
        {
            var victim = death.VictimController?.PlayerName;
            var killer = death.KillerController?.PlayerName ?? "World";
            Console.WriteLine($"{victim} was killed by {killer}");
        }

        // match other events by name
        else if (e.Name.Equals("player_spawn"))
        {
            Console.WriteLine($"Player slot[{e.GetInt("userid")}] spawned");
        }
        else
        {
            Console.WriteLine($"GameEvent {e.Name} fired");
        }
    }

    // it has default implementation that always returns true, 
    // you don't need to implement it if you don't want to use it.
    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        if (e.Name.Equals("player_say", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Blocked GameEvent fire: {e.Name}");

            // Block the event from firing.
            return false;
        }

        if (e.Name.Equals("player_changename", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Silence GameEvent: {e.Name}");

            // Keep the event server-only instead of broadcasting it to clients.
            serverOnly = true;
        }

        return true;
    }

    // just write it according to the example.
    int IEventListener.ListenerVersion => IEventListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IEventListener.ListenerPriority => 0;
}