using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace MultiListenerExample;

public sealed class MultiListener : IModSharpModule,
    IClientListener,
    IEntityListener,
    IEventListener,
    IGameListener
{
    private readonly ISharedSystem _sharedSystem;

    public MultiListener(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // install listener
        _sharedSystem.GetClientManager().InstallClientListener(this);
        _sharedSystem.GetEntityManager().InstallEntityListener(this);
        _sharedSystem.GetEventManager().InstallEventListener(this);
        _sharedSystem.GetModSharp().InstallGameListener(this);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetClientManager().RemoveClientListener(this);
        _sharedSystem.GetEntityManager().RemoveEntityListener(this);
        _sharedSystem.GetEventManager().RemoveEventListener(this);
        _sharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public string DisplayName   => "Multiple Listener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // implement listener methods below

    public void FireGameEvent(IGameEvent @event)
    {
    }

    // just write the ListenerVersion according to the example.
    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IClientListener.ListenerVersion  => IClientListener.ApiVersion;
    int IClientListener.ListenerPriority => 0;
    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;
    int IEntityListener.ListenerPriority => 0;
    int IEventListener. ListenerVersion  => IEventListener.ApiVersion;
    int IEventListener. ListenerPriority => 0;
    int IGameListener.  ListenerVersion  => IGameListener.ApiVersion;
    int IGameListener.  ListenerPriority => 0;
}
