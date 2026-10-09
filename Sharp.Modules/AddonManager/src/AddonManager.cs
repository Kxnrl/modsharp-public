/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 * ============================================================================
 * Acknowledgements
 *
 * Feature set and behavior follow MultiAddonManager (Source2ZE/MultiAddonManager)
 * by xen, licensed under GPL-3.0.
 *
 *   https://github.com/Source2ZE/MultiAddonManager
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AddonManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AddonManager;

public sealed class AddonManager : IModSharpModule, IAddonManager, IAddonListener, IEventListener, ISteamListener, IGameListener
{
    string IModSharpModule.DisplayName   => "Sharp.Modules.AddonManager";
    string IModSharpModule.DisplayAuthor => "ModSharp Dev Team";

    int IAddonListener.ListenerVersion  => IAddonListener.ApiVersion;
    int IAddonListener.ListenerPriority => 0;
    int IEventListener.ListenerVersion  => IEventListener.ApiVersion;
    int IEventListener.ListenerPriority => 0;
    int ISteamListener.ListenerVersion  => ISteamListener.ApiVersion;
    int ISteamListener.ListenerPriority => 0;
    int IGameListener.ListenerVersion   => IGameListener.ApiVersion;
    int IGameListener.ListenerPriority  => 0;

    private readonly ISharedSystem         _sharedSystem;
    private readonly IConfiguration        _configuration;
    private readonly ILogger<AddonManager> _logger;

    private readonly List<ulong>                      _addons             = [];
    private readonly List<ulong>                      _globalClientAddons = [];
    private readonly Dictionary<SteamID, List<ulong>> _clientAddons       = [];
    private readonly HashSet<ulong>                   _reloadOnDownload   = [];
    private          bool                             _reloadBatchSucceeded;

    private IConVar? _cvExtraAddons;
    private IConVar? _cvClientExtraAddons;
    private IConVar? _cvBlockDisconnectMessages;
    private IConVar? _cvMountDownload;
    private IConVar? _cvTimeout;
    private IConVar? _cvConnectionTimeout;
    private IConVar? _cvCacheClients;
    private IConVar? _cvCacheDuration;
    private IConVar? _cvDebug;
    private bool     _syncingConVar;

    public AddonManager(ISharedSystem sharedSystem,
        string                             dllPath,
        string                             sharpPath,
        Version                            version,
        IConfiguration                     coreConfiguration,
        bool                               hotReload)
    {
        _sharedSystem  = sharedSystem;
        _configuration = coreConfiguration;
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<AddonManager>();
    }

#region IModSharpModule

    public bool Init()
    {
        // core.json "AddonManager" is read before the first map, unlike cvars from server.cfg,
        // so it also works where launch parameters are not available
        // -addons on the command line overrides core.json "Addons", even when empty
        // after a hot reload the native list already holds the addons this module applied
        _addons.AddRange(_sharedSystem.GetAddonManager().GetAddons().Distinct());
        if (!_sharedSystem.GetModSharp().HasCommandLine("-addons"))
        {
            _addons.AddRange(ReadConfig("AddonManager:Addons").Where(x => !_addons.Contains(x)).ToArray());
        }

        _globalClientAddons.AddRange(ReadConfig("AddonManager:ClientAddons"));
        ApplyAddons(false, false);

        // being queried keeps the delivery active even without server addons
        _sharedSystem.GetAddonManager().InstallAddonListener(this);

        var conVars = _sharedSystem.GetConVarManager();

        _cvExtraAddons = conVars.CreateConVar("ms_extra_addons",
                                              string.Join(',', _addons),
                                              "Workshop IDs of extra server addons separated by commas, applied on the next map change");

        _cvClientExtraAddons = conVars.CreateConVar("ms_client_extra_addons",
                                                    string.Join(',', _globalClientAddons),
                                                    "Workshop IDs of extra addons applied to all clients (download-only), separated by commas");

        // after a hot reload the ConVar already exists and keeps its runtime value
        if (_cvClientExtraAddons is not null)
        {
            OnClientExtraAddonsChanged(_cvClientExtraAddons);
        }

        _cvBlockDisconnectMessages = conVars.CreateConVar("ms_block_disconnect_messages",
                                                          false,
                                                          "Whether to block \"loop shutdown\" disconnect messages while clients reconnect for addons");

        _cvMountDownload = conVars.CreateConVar("ms_addon_mount_download",
                                                false,
                                                "Whether to re-download (update) server addons on every map start even if installed");

        _cvTimeout = conVars.CreateConVar("ms_extra_addons_timeout",
                                          10f,
                                          "How long until clients are timed out in between connects for extra addons in seconds");

        _cvConnectionTimeout = conVars.CreateConVar("ms_addon_connection_timeout",
                                                    30f,
                                                    "How long until clients are timed out while downloading the first required addon (usually the current map), 0 disables");

        // off by default: a cached rejoin gets every addon at once in ReplyConnection, which can lock up a client
        // when one of them was updated in the meantime
        _cvCacheClients = conVars.CreateConVar("ms_cache_clients_with_addons",
                                               false,
                                               "Whether to cache clients addon download list, this will prevent reconnects on mapchange/rejoin");

        _cvCacheDuration = conVars.CreateConVar("ms_cache_clients_duration",
                                                0f,
                                                "How long to cache clients' downloaded addons list in seconds, pass 0 for forever");

        _cvDebug = conVars.CreateConVar("ms_addon_debug", false, "Whether to print some extra debug information");

        foreach (var conVar in new[] { _cvTimeout, _cvConnectionTimeout, _cvCacheClients, _cvCacheDuration, _cvDebug })
        {
            if (conVar is not null)
            {
                conVars.InstallChangeHook(conVar, OnOptionChanged);
            }
        }

        ApplyOptions();

        if (_cvExtraAddons is not null)
        {
            conVars.InstallChangeHook(_cvExtraAddons, OnExtraAddonsChanged);
        }

        if (_cvClientExtraAddons is not null)
        {
            conVars.InstallChangeHook(_cvClientExtraAddons, OnClientExtraAddonsChanged);
        }

        conVars.CreateServerCommand("ms_add_addon",           OnCommandAddAddon,          "Add a workshop ID to the extra addon list");
        conVars.CreateServerCommand("ms_remove_addon",        OnCommandRemoveAddon,       "Remove a workshop ID from the extra addon list");
        conVars.CreateServerCommand("ms_add_client_addon",    OnCommandAddClientAddon,    "Add a workshop ID to the global client-only addon list");
        conVars.CreateServerCommand("ms_remove_client_addon", OnCommandRemoveClientAddon, "Remove a workshop ID from the global client-only addon list");
        conVars.CreateServerCommand("ms_download_addon",      OnCommandDownloadAddon,     "Download an addon manually");
        conVars.CreateServerCommand("ms_reload_map",          OnCommandReloadMap,         "Reload the current map to apply addon changes");

        // players can re-fetch the addons when they failed to receive them: ms_addon_refresh
        _sharedSystem.GetClientManager().InstallCommandCallback("addon_refresh", OnClientCommandRefresh);

        var eventManager = _sharedSystem.GetEventManager();
        eventManager.HookEvent("player_disconnect");
        eventManager.InstallEventListener(this);

        _sharedSystem.GetModSharp().InstallSteamListener(this);
        _sharedSystem.GetModSharp().InstallGameListener(this);

        return true;
    }

    public void PostInit()
        => _sharedSystem.GetSharpModuleManager()
                        .RegisterSharpModuleInterface<IAddonManager>(this, IAddonManager.Identity, this);

    public void OnServerActivate()
    {
        if (_cvMountDownload?.GetBool() is not true)
        {
            return;
        }

        foreach (var addon in _addons)
        {
            _sharedSystem.GetAddonManager().UpdateAddon(addon);
        }
    }

    public void Shutdown()
    {
        var conVars = _sharedSystem.GetConVarManager();

        if (_cvExtraAddons is not null)
        {
            conVars.RemoveChangeHook(_cvExtraAddons, OnExtraAddonsChanged);
        }

        if (_cvClientExtraAddons is not null)
        {
            conVars.RemoveChangeHook(_cvClientExtraAddons, OnClientExtraAddonsChanged);
        }

        foreach (var conVar in new[] { _cvTimeout, _cvConnectionTimeout, _cvCacheClients, _cvCacheDuration, _cvDebug })
        {
            if (conVar is not null)
            {
                conVars.RemoveChangeHook(conVar, OnOptionChanged);
            }
        }

        conVars.ReleaseServerCommandCallback("ms_add_addon",           OnCommandAddAddon);
        conVars.ReleaseServerCommandCallback("ms_remove_addon",        OnCommandRemoveAddon);
        conVars.ReleaseServerCommandCallback("ms_add_client_addon",    OnCommandAddClientAddon);
        conVars.ReleaseServerCommandCallback("ms_remove_client_addon", OnCommandRemoveClientAddon);
        conVars.ReleaseServerCommandCallback("ms_download_addon",      OnCommandDownloadAddon);
        conVars.ReleaseServerCommandCallback("ms_reload_map",          OnCommandReloadMap);

        _sharedSystem.GetClientManager().RemoveCommandCallback("addon_refresh", OnClientCommandRefresh);
        _sharedSystem.GetAddonManager().RemoveAddonListener(this);

        _sharedSystem.GetAddonManager().SetOptions(new AddonOptions());
        _sharedSystem.GetEventManager().RemoveEventListener(this);
        _sharedSystem.GetModSharp().RemoveSteamListener(this);
        _sharedSystem.GetModSharp().RemoveGameListener(this);
    }

#endregion

#region IAddonManager

    public IReadOnlyList<ulong> GetAddons()
        => _addons.ToArray();

    public bool AddAddon(ulong addon, bool reloadMap = false)
    {
        if (addon == 0 || _addons.Contains(addon))
        {
            return false;
        }

        _addons.Add(addon);
        ApplyAddons(reloadMap);

        return true;
    }

    public bool RemoveAddon(ulong addon, bool reloadMap = false)
    {
        if (!_addons.Remove(addon))
        {
            return false;
        }

        ApplyAddons(reloadMap);

        return true;
    }

    public void ClearAddons(bool reloadMap = false)
    {
        _addons.Clear();
        ApplyAddons(reloadMap);
    }

    public void ReloadMap()
        => _sharedSystem.GetAddonManager().ReloadMap();

    public IReadOnlyList<ulong> GetClientAddons(SteamID steamId = default)
    {
        if (steamId == default)
        {
            return _globalClientAddons.ToArray();
        }

        return _clientAddons.TryGetValue(steamId, out var list) ? list.ToArray() : [];
    }

    public bool AddClientAddon(ulong addon, SteamID steamId = default, bool refresh = false)
    {
        if (addon == 0)
        {
            return false;
        }

        var list = GetClientAddonList(steamId);

        if (list.Contains(addon))
        {
            return false;
        }

        list.Add(addon);

        if (steamId == default)
        {
            SyncConVar(_cvClientExtraAddons, _globalClientAddons);
        }

        if (refresh)
        {
            // like MAM, only sends what the clients do not have yet
            RefreshClients(steamId, false);
        }

        return true;
    }

    public bool RemoveClientAddon(ulong addon, SteamID steamId = default)
    {
        var removed = GetClientAddonList(steamId).Remove(addon);

        if (removed && steamId == default)
        {
            SyncConVar(_cvClientExtraAddons, _globalClientAddons);
        }

        return removed;
    }

    public void ClearClientAddons(SteamID steamId = default)
    {
        if (steamId != default)
        {
            _clientAddons.Remove(steamId);

            return;
        }

        _globalClientAddons.Clear();
        SyncConVar(_cvClientExtraAddons, _globalClientAddons);
    }

    public bool RefreshClient(SteamID steamId)
        => _sharedSystem.GetAddonManager().RefreshClient(steamId, true);

    public bool DownloadAddon(ulong addon, bool reloadMap = false, bool force = true)
    {
        var steam = _sharedSystem.GetModSharp().GetSteamGameServer();

        if (!steam.IsAvailable())
        {
            _logger.LogWarning("Cannot download addon {Addon}, Steam API is not available", addon);

            return false;
        }

        if (!force && steam.GetItemState(addon).HasFlag(WorkshopItemState.ItemStateInstalled))
        {
            return true;
        }

        // goes through the core so a mounted addon is unmounted while updating (Windows file lock)
        if (!_sharedSystem.GetAddonManager().UpdateAddon(addon))
        {
            _logger.LogWarning("Failed to start download for addon {Addon}", addon);

            return false;
        }

        if (reloadMap)
        {
            _reloadOnDownload.Add(addon);
        }

        _logger.LogInformation("Download started for addon {Addon}", addon);

        return true;
    }

    public bool HasUGCConnection()
        => _sharedSystem.GetModSharp().GetSteamGameServer().IsAvailable();

#endregion

#region Listeners

    public void OnClientQueryAddons(SteamID steamId, List<ulong> addons)
    {
        addons.AddRange(_globalClientAddons);

        if (_clientAddons.TryGetValue(steamId, out var list))
        {
            addons.AddRange(list.Where(x => !addons.Contains(x)));
        }
    }

    public bool HookFireEvent(IGameEvent @event, ref bool serverOnly)
    {
        // clients reconnecting for addons would spam "loop shutdown" in chat
        if (_cvBlockDisconnectMessages?.GetBool() is true
            && @event.GetName() == "player_disconnect"
            && @event.GetInt("reason") == (int) NetworkDisconnectionReason.LoopShutdown)
        {
            serverOnly = true;
        }

        return true;
    }

    public void FireGameEvent(IGameEvent @event)
    {
    }

    public void OnDownloadItemResult(ulong sharedFileId, SteamApiResult result)
    {
        if (!_reloadOnDownload.Remove(sharedFileId))
        {
            return;
        }

        if (result == SteamApiResult.Success)
        {
            _reloadBatchSucceeded = true;
        }
        else
        {
            _logger.LogWarning("Addon {Addon} download failed ({Result})", sharedFileId, result);
        }

        if (_reloadOnDownload.Count > 0)
        {
            return;
        }

        // nothing new to apply when every download failed, and reloading would request them again
        if (!_reloadBatchSucceeded)
        {
            _logger.LogWarning("All downloads failed, skipping map reload");

            return;
        }

        _reloadBatchSucceeded = false;
        _logger.LogInformation("All downloads complete, reloading map");
        ReloadMap();
    }

#endregion

#region Commands

    private ECommandAction OnCommandAddAddon(StringCommand command)
    {
        if (ParseAddonArg(command) is { } addon)
        {
            LogResult(AddAddon(addon), "added", addon);
        }

        return ECommandAction.Handled;
    }

    private ECommandAction OnCommandRemoveAddon(StringCommand command)
    {
        if (ParseAddonArg(command) is { } addon)
        {
            LogResult(RemoveAddon(addon), "removed", addon);
        }

        return ECommandAction.Handled;
    }

    private ECommandAction OnCommandAddClientAddon(StringCommand command)
    {
        if (ParseAddonArg(command) is { } addon)
        {
            LogResult(AddClientAddon(addon), "added to client addons", addon);
        }

        return ECommandAction.Handled;
    }

    private ECommandAction OnCommandRemoveClientAddon(StringCommand command)
    {
        if (ParseAddonArg(command) is { } addon)
        {
            LogResult(RemoveClientAddon(addon), "removed from client addons", addon);
        }

        return ECommandAction.Handled;
    }

    private ECommandAction OnCommandDownloadAddon(StringCommand command)
    {
        if (ParseAddonArg(command) is { } addon)
        {
            DownloadAddon(addon);
        }

        return ECommandAction.Handled;
    }

    private ECommandAction OnCommandReloadMap(StringCommand command)
    {
        ReloadMap();

        return ECommandAction.Handled;
    }

    private ECommandAction OnClientCommandRefresh(IGameClient client, StringCommand command)
    {
        RefreshClient(client.SteamId);

        return ECommandAction.Handled;
    }

#endregion

    private void OnExtraAddonsChanged(IConVar conVar)
    {
        if (_syncingConVar)
        {
            return;
        }

        _addons.Clear();
        _addons.AddRange(ParseAddons(conVar.GetString()));
        ApplyAddons(false, false);
    }

    private void OnClientExtraAddonsChanged(IConVar conVar)
    {
        if (_syncingConVar)
        {
            return;
        }

        _globalClientAddons.Clear();
        _globalClientAddons.AddRange(ParseAddons(conVar.GetString()));
    }

    private void ApplyAddons(bool reloadMap, bool syncConVar = true)
    {
        _sharedSystem.GetAddonManager().SetAddons(_addons);

        if (syncConVar)
        {
            SyncConVar(_cvExtraAddons, _addons);
        }

        _logger.LogInformation("Server addons: [{Addons}]", string.Join(", ", _addons));

        if (reloadMap)
        {
            ReloadMap();
        }
    }

    private void SyncConVar(IConVar? conVar, List<ulong> addons)
    {
        if (conVar is null)
        {
            return;
        }

        _syncingConVar = true;

        try
        {
            conVar.Set(string.Join(',', addons));
        }
        finally
        {
            _syncingConVar = false;
        }
    }

    private void RefreshClients(SteamID steamId, bool resetCache)
    {
        var addonManager = _sharedSystem.GetAddonManager();

        if (steamId != default)
        {
            addonManager.RefreshClient(steamId, resetCache);

            return;
        }

        foreach (var client in _sharedSystem.GetClientManager().GetGameClients(true))
        {
            if (!client.IsFakeClient)
            {
                addonManager.RefreshClient(client.SteamId, resetCache);
            }
        }
    }

    private void OnOptionChanged(IConVar conVar)
        => ApplyOptions();

    private void ApplyOptions()
    {
        var cacheDuration = _cvCacheClients?.GetBool() is true ? _cvCacheDuration?.GetFloat() ?? 0 : -1;

        _sharedSystem.GetAddonManager()
                     .SetOptions(new AddonOptions(_cvTimeout?.GetFloat() ?? 10,
                                                  _cvConnectionTimeout?.GetFloat() ?? 30,
                                                  cacheDuration,
                                                  _cvDebug?.GetBool() ?? false));
    }

    private IEnumerable<ulong> ReadConfig(string key)
        => _configuration.GetSection(key).Get<ulong[]>()?.Where(x => x > 0).Distinct() ?? [];

    private List<ulong> GetClientAddonList(SteamID steamId)
    {
        if (steamId == default)
        {
            return _globalClientAddons;
        }

        if (!_clientAddons.TryGetValue(steamId, out var list))
        {
            _clientAddons[steamId] = list = [];
        }

        return list;
    }

    private ulong? ParseAddonArg(StringCommand command)
    {
        if (command.ArgCount >= 1 && ulong.TryParse(command.GetArg(1), out var addon) && addon > 0)
        {
            return addon;
        }

        _logger.LogInformation("Usage: {Command} <workshop id>", command.CommandName);

        return null;
    }

    private void LogResult(bool success, string action, ulong addon)
    {
        if (success)
        {
            _logger.LogInformation("Addon {Addon} {Action}", addon, action);
        }
        else
        {
            _logger.LogWarning("Addon {Addon} was not {Action}", addon, action);
        }
    }

    private static IEnumerable<ulong> ParseAddons(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => ulong.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct();
}
