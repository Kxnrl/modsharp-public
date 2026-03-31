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

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

internal class BanService : ICommandCategory, IBanService
{
    private readonly ILogger<BanService>   _logger;
    private readonly InterfaceBridge       _bridge;
    private readonly AdminOperationService _operations;
    private readonly AdminOperationEngine  _engine;
    private readonly ModuleContext         _moduleContext;
    private readonly CommandContextFactory _contextFactory;

    public BanService(ILogger<BanService> logger,
        InterfaceBridge                   bridge,
        AdminOperationService             operations,
        AdminOperationEngine              engine,
        CommandContextFactory             contextFactory,
        ModuleContext                     moduleContext)
    {
        _logger         = logger;
        _bridge         = bridge;
        _operations     = operations;
        _engine         = engine;
        _contextFactory = contextFactory;
        _moduleContext  = moduleContext;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("ban",       OnCommandBan,       ["admin:ban"]);
        registry.RegisterAdminCommand("banip",     OnCommandBanIp,     ["admin:ban"]);
        registry.RegisterAdminCommand("bansubnet", OnCommandBanSubnet, ["admin:ban"]);

        registry.RegisterAdminCommand("addban",  OnCommandAddBan, ["admin:ban"]);
        registry.RegisterAdminCommand("unban",   OnCommandUnban,  ["admin:ban", "admin:unban"]);
        registry.RegisterAdminCommand("unbanip", OnCommandUnban,  ["admin:ban", "admin:unban"]);
    }

    public void Ban(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
        => _engine.ApplyOnline(admin,
                               target,
                               AdminOperationType.Ban,
                               duration,
                               reason,
                               metadata: CreateBanMetadata(target, BanType.SteamId));

    public void Ban(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason)
        => _engine.ApplyOffline(admin,
                                steamId,
                                steamId.ToString(),
                                AdminOperationType.Ban,
                                duration,
                                reason,
                                JsonSerializer.Serialize(new { bantype = (int) BanType.SteamId }));

    public void Unban(IGameClient? admin, SteamID steamId, string reason)
        => _engine.RemoveOffline(admin, steamId, steamId.ToString(), AdminOperationType.Ban, reason);

    private void OnCommandBan(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(3, "Admin.Usage.Ban", "Usage: ms_ban <target> <duration> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target) || target.IsFakeClient)
        {
            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason        = ctx.GetReason(3);
        var targetSteamId = target.SteamId;
        var targetName    = target.Name;

        _ = ExecuteBanAsync(ctx, targetSteamId, targetName, duration, reason, issuer, BanType.SteamId)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process ban for {SteamId}", targetSteamId);
                                  ctx.Reply("Failed to process ban. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnCommandBanIp(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(3, "Admin.Usage.BanIp", "Usage: ms_banip <target> <duration> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target) || target.IsFakeClient)
        {
            return;
        }

        if (TryGetIpv4Address(target) is null)
        {
            ctx.ReplyKey("Admin.BanIp.NoIp", "Cannot ban {0}: No IPv4 address found.", target.Name);

            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason        = ctx.GetReason(3);
        var targetSteamId = target.SteamId;
        var targetName    = target.Name;

        _ = ExecuteBanAsync(ctx, targetSteamId, targetName, duration, reason, issuer, BanType.Ip)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process banip for {SteamId}", targetSteamId);
                                  ctx.Reply("Failed to process banip. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnCommandBanSubnet(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(3, "Admin.Usage.BanSubnet", "Usage: ms_bansubnet <target> <duration> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target) || target.IsFakeClient)
        {
            return;
        }

        if (TryGetIpv4Address(target) is null)
        {
            ctx.ReplyKey("Admin.BanIp.NoIp", "Cannot ban {0}: Can't get their ip.", target.Name);

            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason        = ctx.GetReason(3);
        var targetSteamId = target.SteamId;
        var targetName    = target.Name;

        _ = ExecuteBanAsync(ctx, targetSteamId, targetName, duration, reason, issuer, BanType.IpRange)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process bansubnet for {SteamId}", targetSteamId);
                                  ctx.Reply("Failed to process subnet ban. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnCommandAddBan(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(3, "Admin.Usage.AddBan", "Usage: ms_addban <steamid> <duration> [reason]"))
        {
            return;
        }

        if (!TryParseSteamId(command.GetArg(1), out var steamId))
        {
            ctx.ReplyKey("Admin.InvalidSteamId", "Invalid SteamID.");

            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason = ctx.GetReason(3);

        _ = ExecuteBanAsync(ctx, steamId, steamId.ToString(), duration, reason, issuer, BanType.SteamId)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process ban for {SteamId}", steamId);
                                  ctx.Reply("Failed to process ban. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnCommandUnban(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Unban", "Usage: ms_unban <steamid> [reason]"))
        {
            return;
        }

        if (!TryParseSteamId(command.GetArg(1), out var steamId))
        {
            ctx.ReplyKey("Admin.InvalidSteamId", "Invalid SteamID.");

            return;
        }

        var reason = ctx.GetReason(2);

        _ = ExecuteUnbanAsync(ctx, steamId, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process unban for {SteamId}", steamId);
                                  ctx.Reply("Failed to process unban. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteUnbanAsync(CommandContext ctx, SteamID steamId, string reason, IGameClient? issuer)
    {
        if (!await _operations.HasActiveAsync(steamId, AdminOperationType.Ban).ConfigureAwait(false))
        {
            ctx.ReplyKey("Admin.NotBanned", "{0} is not banned.", steamId);

            return;
        }

        _engine.RemoveOffline(issuer, steamId, steamId.ToString(), AdminOperationType.Ban, reason);

        ctx.ReplySuccessKey("Admin.Unbanned", "Unbanned {0}. Reason: {1}", steamId, reason);
    }

    private async Task ExecuteBanAsync(CommandContext ctx,
        SteamID                                       targetId,
        string                                        targetDisplayName,
        TimeSpan?                                     duration,
        string                                        reason,
        IGameClient?                                  issuer,
        BanType                                       type)
    {
        try
        {
            if (await _operations.HasActiveAsync(targetId, AdminOperationType.Ban).ConfigureAwait(false))
            {
                ctx.ReplyKey("Admin.AlreadyBanned", "{0} is already banned.", targetDisplayName);

                return;
            }

            if (_bridge.ClientManager.GetGameClient(targetId) is { } targetClient)
            {
                var metadata = CreateBanMetadata(targetClient, type);
                _engine.ApplyOnline(issuer, targetClient, AdminOperationType.Ban, duration, reason, metadata: metadata);
            }
            else
            {
                var metadata = JsonSerializer.Serialize(new { bantype = (int) type });
                _engine.ApplyOffline(issuer, targetId, targetDisplayName, AdminOperationType.Ban, duration, reason, metadata);
            }

            var durationStr = FormatDuration(duration);
            ctx.ReplySuccessKey("Admin.Banned", "Banned {0} {1}. Reason: {2}", targetDisplayName, durationStr, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ban for {SteamId}", targetId);
            ctx.Reply("Failed to process ban. Check server logs.");
        }
    }

    private static bool TryParseSteamId(string raw, out SteamID steamId)
    {
        var trimmed = raw.Trim();

        if (trimmed.StartsWith('@'))
        {
            trimmed = trimmed[1..];
        }

        steamId = default;

        return ulong.TryParse(trimmed, out var value) && (steamId = new SteamID(value)).IsValidUserId();
    }

    private LocalizedDuration FormatDuration(TimeSpan? duration)
        => new (duration, _moduleContext.LocalizerManager);

    private static string? CreateBanMetadata(IGameClient target, BanType type)
    {
        var ipv4 = TryGetIpv4Address(target);

        var data = new Dictionary<string, object> { ["bantype"] = type };

        if (ipv4 is not null)
        {
            data["ip"] = ipv4;
        }

        return JsonSerializer.Serialize(data);
    }

    private static string? TryGetIpv4Address(IGameClient target)
    {
        var address = target.GetAddress(false) ?? target.Address;

        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var trimmed = address.Trim();

        if (trimmed.Contains(':') && trimmed.Contains('.'))
        {
            var colonIndex = trimmed.IndexOf(':');

            if (colonIndex > 0)
            {
                trimmed = trimmed[..colonIndex];
            }
        }

        if (!IPAddress.TryParse(trimmed, out var ipAddress))
        {
            return null;
        }

        return ipAddress.AddressFamily == AddressFamily.InterNetwork ? trimmed : null;
    }
}
