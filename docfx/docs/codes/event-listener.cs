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
        // install listener, any class what inherits IEventListener can be a listener.
        _sharedSystem.GetEventManager().InstallEventListener(this);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetEventManager().RemoveEventListener(this);
    }

    public string DisplayName   => "EventListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    public void FireGameEvent(IGameEvent e)
    {
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

            // 阻止事件发射
            return false;
        }

        if (e.Name.Equals("player_changename", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Silence GameEvent: {e.Name}");

            // 将事件设置为仅限服务器而不下发至客户端
            serverOnly = true;
        }

        return true;
    }

    // just write it according to the example.
    int IEventListener.ListenerVersion => IEventListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IEventListener.ListenerPriority => 0;
}