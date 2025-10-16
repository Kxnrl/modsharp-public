﻿using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Units;

namespace SteamListener;

// ReSharper disable once UnusedMember.Global
internal class SteamListener : IModSharpModule, ISteamListener
{
    private readonly ISharedSystem _sharedSystem;

    public SteamListener(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        _sharedSystem.GetModSharp().InstallSteamListener(this);
        return true;
    }

    public void Shutdown()
    {
        _sharedSystem.GetModSharp().RemoveSteamListener(this);
    }

    public string DisplayName => "SteamListener Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    /// <summary>
    ///     调用 RequestUserGroupStatus 后触发
    /// </summary>
    public void OnGroupStatusResult(SteamID steamId, SteamID groupId, bool isMember, bool isOfficer)
    {
        Console.WriteLine($"[OnGroupStatusResult] SteamId={steamId} GroupId={groupId} IsMember={isMember} IsOfficer={isOfficer}");
    }

    /// <summary>
    ///     服务器连上Steam
    /// </summary>
    public void OnSteamServersConnected()
    {
        Console.WriteLine("[OnSteamServersConnected]");
    }

    /// <summary>
    ///     服务器断联Steam
    /// </summary>
    public void OnSteamServersDisconnected(SteamApiResult reason)
    {
        Console.WriteLine($"[OnSteamServersDisconnected] reason={reason}");
    }

    /// <summary>
    ///     服务器连不上Steam
    /// </summary>
    public void OnSteamServersConnectFailure(SteamApiResult reason, bool stillRetrying)
    {
        Console.WriteLine($"[OnSteamServersConnectFailure] reason={reason} stillRetrying={stillRetrying}");
    }

    /// <summary>
    ///     UGC下载
    /// </summary>
    public void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
        Console.WriteLine($"[OnDownloadItemResult] sharedFileId={sharedFileId} result={result}");
    }

    /// <summary>
    ///     UGC安装
    /// </summary>
    /// <param name="publishedFileId">创意工坊ID</param>
    public void OnItemInstalled(ulong publishedFileId)
    {
        Console.WriteLine($"[OnItemInstalled] publishedFileId={publishedFileId}");
    }

    int ISteamListener.ListenerVersion => ISteamListener.ApiVersion;
    int ISteamListener.ListenerPriority => 0;
}