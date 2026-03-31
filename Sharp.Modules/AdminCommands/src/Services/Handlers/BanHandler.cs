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
 */

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal class BanHandler : IAdminOperationHandler, IAdminOperationHookRegistrar
{
    private readonly ILogger<BanHandler> _logger;
    private readonly InterfaceBridge     _bridge;

    private readonly Dictionary<SteamID, BanEntry> _bans;
    private readonly Dictionary<string, DateTime?> _ipBans;

    private bool _hooksRegistered;

    public BanHandler(ILogger<BanHandler> logger, InterfaceBridge bridge)
    {
        _logger = logger;
        _bridge = bridge;

        _bans   = [];
        _ipBans = [];
    }

    public AdminOperationType Type => AdminOperationType.Ban;

    public void OnApplied(AdminOperationRecord record, IGameClient? targetClient)
    {
        var metadata = record.GetMetadata<BanMetadata>();
        var type     = metadata?.BanType ?? BanType.SteamId;
        var ip       = metadata?.Ip;

        SetBanned(record.SteamId, true, type, ip, record.ExpiresAt);

        if (targetClient is not null)
        {
            _bridge.ClientManager.KickClient(targetClient, record.Reason, NetworkDisconnectionReason.SteamBanned);
        }
    }

    public void OnRemoved(SteamID targetId, IGameClient? targetClient)
    {
        SetBanned(targetId, false);
    }

    public (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText)
        => ("Admin.BanApplied", $"banned {target.Name} {durationText}");

    public (string Key, string Fallback) GetRemovedNotification(IGameClient target)
        => ("Admin.BanRemoved", $"unbanned {target.Name}");

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ConnectClient.InstallHookPre(OnConnectClientPre);
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ConnectClient.RemoveHookPre(OnConnectClientPre);

        _hooksRegistered = false;
    }

    private HookReturnValue<NetworkDisconnectionReason> OnConnectClientPre(IConnectClientHookParams @params,
        HookReturnValue<NetworkDisconnectionReason>                                                 arg2)
    {
        var steamId = @params.SteamId;

        // todo: use @params.Ip
        if (!IsBanned(steamId, @params.Ip))
        {
            return new HookReturnValue<NetworkDisconnectionReason>();
        }

        return new HookReturnValue<NetworkDisconnectionReason>(EHookAction.SkipCallReturnOverride,
                                                               NetworkDisconnectionReason.SteamBanned);
    }

    private bool IsBanned(SteamID steamId, string? ip)
    {
        if (!string.IsNullOrWhiteSpace(ip) && _ipBans.TryGetValue(ip, out var expiresAt))
        {
            if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
            {
                _ipBans.Remove(ip);
            }
            else
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var subnet = GetSubnet(ip);

            if (_ipBans.TryGetValue(subnet, out expiresAt))
            {
                if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
                {
                    _ipBans.Remove(subnet);
                }
                else
                {
                    return true;
                }
            }
        }

        if (!_bans.TryGetValue(steamId, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
        {
            SetBanned(steamId, false); // cleanup both cache

            return false;
        }

        return true;
    }

    private void SetBanned(SteamID steamId,
        bool                       banned,
        BanType                    type      = BanType.SteamId,
        string?                    ip        = null,
        DateTime?                  expiresAt = null)
    {
        if (banned)
        {
            _bans[steamId] = new BanEntry(expiresAt, type, ip);

            if (!string.IsNullOrWhiteSpace(ip))
            {
                if (type == BanType.Ip)
                {
                    _ipBans[ip] = expiresAt;
                }

                else if (type == BanType.IpRange)
                {
                    // Subnet ban: "1.1.1."
                    _ipBans[GetSubnet(ip)] = expiresAt;
                }
            }
        }
        else
        {
            if (_bans.Remove(steamId, out var entry))
            {
                if (entry.Type == BanType.Ip && !string.IsNullOrWhiteSpace(entry.Ip))
                {
                    _ipBans.Remove(entry.Ip);
                }
                else if (entry.Type == BanType.IpRange && !string.IsNullOrWhiteSpace(entry.Ip))
                {
                    _ipBans.Remove(GetSubnet(entry.Ip));
                }
            }
        }
    }

    private static string GetSubnet(string ip)
    {
        var lastDotIndex = ip.LastIndexOf('.');

        if (lastDotIndex == -1)
        {
            return ip;
        }

        return ip[..(lastDotIndex + 1)];
    }

    private record struct BanEntry(DateTime? ExpiresAt, BanType Type, string? Ip);

    private record BanMetadata
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("bantype")]
        public BanType BanType { get; set; }
    }
}
