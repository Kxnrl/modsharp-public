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

internal class GagService : ICommandCategory, IGagService
{
    private readonly ILogger<GagService>   _logger;
    private readonly AdminOperationService _operations;
    private readonly AdminOperationEngine  _engine;
    private readonly CommandContextFactory _contextFactory;

    public GagService(ILogger<GagService> logger,
        AdminOperationService             operations,
        AdminOperationEngine              engine,
        CommandContextFactory             contextFactory)
    {
        _logger         = logger;
        _operations     = operations;
        _engine         = engine;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("gag",   OnCommandGag,   ["admin:gag", "admin:silence"]);
        registry.RegisterAdminCommand("ungag", OnCommandUngag, ["admin:gag", "admin:silence"]);
    }

    private void OnCommandGag(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Gag", "Usage: ms_gag <target> [duration] [reason]"))
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

        _ = ExecuteGagAsync(ctx, target, duration, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process gag for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process gag. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteGagAsync(CommandContext ctx,
        IGameClient                                   target,
        TimeSpan?                                     duration,
        string                                        reason,
        IGameClient?                                  issuer)
    {
        if (await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Gag).ConfigureAwait(false))
        {
            ctx.ReplyKey("Admin.AlreadyGagged", "{0} is already gagged.", target.Name);

            return;
        }

        _engine.ApplyOnline(issuer, target, AdminOperationType.Gag, duration, reason);
    }

    private void OnCommandUngag(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Ungag", "Usage: ms_ungag <target> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        var reason = ctx.GetReason(2);

        _ = ExecuteUngagAsync(ctx, target, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process ungag for {SteamId}", target.SteamId);
                                  ctx.Reply("Failed to process ungag. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteUngagAsync(CommandContext ctx, IGameClient target, string reason, IGameClient? issuer)
    {
        if (!await _operations.HasActiveAsync(target.SteamId, AdminOperationType.Gag).ConfigureAwait(false))
        {
            ctx.ReplyKey("Admin.NotGagged", "{0} is not gagged.", target.Name);

            return;
        }

        _engine.RemoveOnline(issuer, target, AdminOperationType.Gag, reason);
    }

    public void Gag(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
        => _engine.ApplyOnline(admin, target, AdminOperationType.Gag, duration, reason);

    public void Gag(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason)
        => _engine.ApplyOffline(admin, steamId, "Offline Player", AdminOperationType.Gag, duration, reason);

    public void Ungag(IGameClient? admin, IGameClient target, string reason)
        => _engine.RemoveOnline(admin, target, AdminOperationType.Gag, reason);
}
