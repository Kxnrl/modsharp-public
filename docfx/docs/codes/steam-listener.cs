using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Units;

namespace SteamListenerExample;

public sealed class SteamListener : IModSharpModule, ISteamListener
{
    private readonly ISharedSystem _sharedSystem;

    public SteamListener(ISharedSystem sharedSystem,
        string                         dllPath,
        string                         sharpPath,
        Version                        version,
        IConfiguration                 coreConfiguration,
        bool                           hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        // install listener, any class what inherits ISteamListener can be a listener.
        _sharedSystem.GetModSharp().InstallSteamListener(this);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        // otherwise you will get fucked after reloaded.
        _sharedSystem.GetModSharp().RemoveSteamListener(this);
    }

    public string DisplayName   => "SteamListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    // all callbacks are optional to implement, you can just implement what you need.

    // callback from RequestUserGroupStatus
    public void OnGroupStatusResult(SteamID steamId, SteamID groupId, bool isMember, bool isOfficer)
    {
        Console.WriteLine(
            $"[OnGroupStatusResult] SteamId={steamId} GroupId={groupId} IsMember={isMember} IsOfficer={isOfficer}");
    }

    // on GC connected
    public void OnSteamServersConnected()
    {
        Console.WriteLine("[OnSteamServersConnected]");
    }

    // on GC disconnected
    public void OnSteamServersDisconnected(SteamApiResult reason)
    {
        Console.WriteLine($"[OnSteamServersDisconnected] reason={reason}");
    }

    // on GC connect failure
    public void OnSteamServersConnectFailure(SteamApiResult reason, bool stillRetrying)
    {
        Console.WriteLine($"[OnSteamServersConnectFailure] reason={reason} stillRetrying={stillRetrying}");
    }

    // on UGC/workshop download result
    public void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
        Console.WriteLine($"[OnDownloadItemResult] sharedFileId={sharedFileId} result={result}");
    }

    // on UGC/workshop item installed
    public void OnItemInstalled(ulong publishedFileId)
    {
        Console.WriteLine($"[OnItemInstalled] publishedFileId={publishedFileId}");
    }

    // just write it according to the example.
    int ISteamListener.ListenerVersion => ISteamListener.ApiVersion;

    // the larger the number = the higher the priority; in most cases, 0 is used directly.
    int ISteamListener.ListenerPriority => 0;
}
