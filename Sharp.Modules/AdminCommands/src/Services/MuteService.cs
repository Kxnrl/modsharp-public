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

using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminCommands.Shared;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services;

internal class MuteService : ICommandCategory, IMuteService
{
    private readonly ILogger<MuteService>  _logger;
    private readonly AdminOperationService _operations;
    private readonly AdminOperationEngine  _engine;
    private readonly CommandContextFactory _contextFactory;

    public MuteService(ILogger<MuteService> logger,
        AdminOperationService               operations,
        AdminOperationEngine                engine,
        CommandContextFactory               contextFactory)
    {
        _logger         = logger;
        _operations     = operations;
        _engine         = engine;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("mute",   OnCommandMute,   ["admin:mute", "admin:silence"]);
        registry.RegisterAdminCommand("unmute", OnCommandUnmute, ["admin:mute", "admin:silence"]);
    }

    private void OnCommandMute(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Mute", "Usage: ms_mute <target> [duration] [reason]"))
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

        var reason = ctx.GetReason(3);

        _ = ExecuteMuteAsync(ctx, target, duration, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process mute for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process mute. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteMuteAsync(CommandContext ctx,
        IGameClient                                    target,
        TimeSpan?                                      duration,
        string                                         reason,
        IGameClient?                                   issuer)
    {
        if (await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Mute).ConfigureAwait(false))
        {
            ctx.ReplyKey("Admin.AlreadyMuted", "{0} is already muted.", target.Name);

            return;
        }

        _engine.ApplyOnline(issuer, target, AdminOperationType.Mute, duration, reason);
    }

    private void OnCommandUnmute(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Unmute", "Usage: ms_unmute <target> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        var reason = ctx.GetReason(2);

        _ = ExecuteUnmuteAsync(ctx, target, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process unmute for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process unmute. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteUnmuteAsync(CommandContext ctx, IGameClient target, string reason, IGameClient? issuer)
    {
        if (!await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Mute).ConfigureAwait(false))
        {
            ctx.ReplyKey("Admin.NotMuted", "{0} is not muted.", target.Name);

            return;
        }

        _engine.RemoveOnline(issuer, target, AdminOperationType.Mute, reason);
    }

    public void Mute(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
        => _engine.ApplyOnline(admin, target, AdminOperationType.Mute, duration, reason);

    public void Mute(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason)
        => _engine.ApplyOffline(admin, steamId, "Offline Player", AdminOperationType.Mute, duration, reason);

    public void Unmute(IGameClient? admin, IGameClient target, string reason)
        => _engine.RemoveOnline(admin, target, AdminOperationType.Mute, reason);
}
