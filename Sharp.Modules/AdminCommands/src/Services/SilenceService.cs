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

namespace Sharp.Modules.AdminCommands.Services;

internal class SilenceService : ICommandCategory, ISilenceService
{
    private readonly ILogger<SilenceService> _logger;
    private readonly AdminOperationService   _operations;
    private readonly AdminOperationEngine    _engine;
    private readonly CommandContextFactory   _contextFactory;

    public SilenceService(ILogger<SilenceService> logger,
        InterfaceBridge                           bridge,
        AdminOperationService                     operations,
        AdminOperationEngine                      engine,
        CommandContextFactory                     contextFactory)
    {
        _logger         = logger;
        _operations     = operations;
        _engine         = engine;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("silence",   OnCommandSilence,   ["admin:silence"]);
        registry.RegisterAdminCommand("unsilence", OnCommandUnSilence, ["admin:silence"]);
    }

    private void OnCommandSilence(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Silence", "Usage: ms_silence <target> [duration] [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason = ctx.GetReason(3);

        _ = ExecuteSilenceAsync(ctx, target, duration, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process silence for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process silence. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteSilenceAsync(CommandContext ctx,
        IGameClient                                       target,
        TimeSpan?                                         duration,
        string                                            reason,
        IGameClient?                                      issuer)
    {
        var isMuted = await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Mute).ConfigureAwait(false);
        var isGag   = await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Gag).ConfigureAwait(false);

        if (isMuted && isGag)
        {
            ctx.ReplyKey("Admin.AlreadySilenced", "{0} is already silenced.", target.Name);

            return;
        }

        _engine.ApplyOnline(issuer, target, AdminOperationType.Mute, duration, reason, true);
        _engine.ApplyOnline(issuer, target, AdminOperationType.Gag,  duration, reason, true);

        _engine.NotifySilenceApplied(issuer, target, duration, reason);
    }

    private void OnCommandUnSilence(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Unsilence", "Usage: ms_unsilence <target> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        var reason = ctx.GetReason(2);

        _ = ExecuteUnsilenceAsync(ctx, target, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process unsilence for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process unsilence. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteUnsilenceAsync(CommandContext ctx, IGameClient target, string reason, IGameClient? issuer)
    {
        var isMuted = await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Mute).ConfigureAwait(false);
        var isGag   = await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Gag).ConfigureAwait(false);

        if (!isMuted && !isGag)
        {
            ctx.ReplyKey("Admin.NotSilenced", "{0} is not silenced.", target.Name);

            return;
        }

        _engine.RemoveOnline(issuer, target, AdminOperationType.Mute, reason, true);
        _engine.RemoveOnline(issuer, target, AdminOperationType.Gag,  reason, true);

        _engine.NotifySilenceRemoved(issuer, target, reason);
    }

    public void Silence(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
    {
        _engine.ApplyOnline(admin, target, AdminOperationType.Mute, duration, reason, true);
        _engine.ApplyOnline(admin, target, AdminOperationType.Gag,  duration, reason, true);
        _engine.NotifySilenceApplied(admin, target, duration, reason);
    }

    public void Unsilence(IGameClient? admin, IGameClient target, string reason)
    {
        _engine.RemoveOnline(admin, target, AdminOperationType.Mute, reason, true);
        _engine.RemoveOnline(admin, target, AdminOperationType.Gag,  reason, true);
        _engine.NotifySilenceRemoved(admin, target, reason);
    }
}
