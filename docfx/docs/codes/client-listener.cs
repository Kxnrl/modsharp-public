using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace ClientListenerExample;

public sealed class ClientListener : IModSharpModule, IClientListener
{
    private readonly ISharedSystem _sharedSystem;

    public ClientListener(ISharedSystem sharedSystem,
        string                          dllPath,
        string                          sharpPath,
        Version                         version,
        IConfiguration                  coreConfiguration,
        bool                            hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // install listener, any class what inherits IClientListener can be a listener.
        _sharedSystem.GetClientManager().InstallClientListener(this);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetClientManager().RemoveClientListener(this);
    }

    public string DisplayName   => "Client Listener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // all callbacks are optional to implement, you can just implement what you need.

    public bool OnClientPreAdminCheck(IGameClient client)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientPreAdminCheck] {client.Name} ({client.SteamId})");

        return false;
    }

    public void OnClientConnected(IGameClient client)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientConnected] {client.Name} ({client.SteamId})");
    }

    public void OnClientPutInServer(IGameClient client)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientPutInServer] {client.Name} ({client.SteamId})");
    }

    public void OnClientPostAdminCheck(IGameClient client)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientPostAdminCheck] {client.Name} ({client.SteamId})");
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientDisconnecting] {client.Name} ({client.SteamId}), reason: {reason}");
    }

    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientDisconnected] {client.Name} ({client.SteamId}), reason: {reason}");
    }

    public void OnClientSettingChanged(IGameClient client)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientSettingChanged] {client.Name} ({client.SteamId})");
    }

    public ECommandAction OnClientSayCommand(IGameClient client,
        bool                                             teamOnly,
        bool                                             isCommand,
        string                                           commandName,
        string                                           message)
    {
        _sharedSystem.GetModSharp()
                     .LogMessage(
                         $"[OnClientSayCommand] {client.Name} ({client.SteamId}), teamOnly: {teamOnly}, isCommand: {isCommand}, commandName: {commandName}, message: {message}");

        return ECommandAction.Skipped;
    }

    // just write it according to the example.
    int IClientListener.ListenerVersion => IClientListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int IClientListener.ListenerPriority => 0;
}
