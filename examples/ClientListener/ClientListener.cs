using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Listeners;

namespace ClientListener;

internal class ClientListener : IModSharpModule, IClientListener
{
    private readonly ISharedSystem _sharedSystem;
    public ClientListener(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfiguration, bool hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);

        _sharedSystem = sharedSystem;
    }

    public bool Init()
    {
        // Install listener, any class what inherits IClientListener can be a listener.
        _sharedSystem.GetClientManager().InstallClientListener(this);

        return true;
    }

    public void Shutdown()
    {
        // Must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetClientManager().RemoveClientListener(this);
    }

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

    public ECommandAction OnClientSayCommand(IGameClient client, bool teamOnly, bool isCommand, string commandName, string message)
    {
        _sharedSystem.GetModSharp().LogMessage($"[OnClientSayCommand] {client.Name} ({client.SteamId}), teamOnly: {teamOnly}, isCommand: {isCommand}, commandName: {commandName}, message: {message}");
        return ECommandAction.Skipped;
    }


    // 不用管，你就直接照着例子写就行
    int IClientListener.ListenerVersion => IClientListener.ApiVersion;

    // 优先级，数字越大优先级越高，绝大多数情况下你随便设这个数就行
    int IClientListener.ListenerPriority => 0;
}
