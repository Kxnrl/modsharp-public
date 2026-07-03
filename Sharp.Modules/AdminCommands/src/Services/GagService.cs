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
    private readonly InterfaceBridge       _bridge;
    private readonly AdminOperationService _operations;
    private readonly AdminOperationEngine  _engine;
    private readonly CommandContextFactory _contextFactory;

    public GagService(ILogger<GagService>   logger,
                      InterfaceBridge       bridge,
                      AdminOperationService operations,
                      AdminOperationEngine  engine,
                      CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _bridge         = bridge;
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

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        if (!ctx.TryParseDuration(2, out var duration))
        {
            return;
        }

        var reason = ctx.GetReason(3);

        _ = ExecuteGagAsync(ctx, targets, targetLabel, duration, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process gag batch");
                                  ctx.Reply("Failed to process gag. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteGagAsync(CommandContext ctx,
        IReadOnlyList<IGameClient>                    targets,
        string                                        targetLabel,
        TimeSpan?                                     duration,
        string                                        reason,
        IGameClient?                                  issuer)
    {
        var candidates = targets.Where(t => !t.IsFakeClient)
                                .Select(t => (Client: t, t.SteamId, t.Name))
                                .ToList();

        var targetToApply = new List<(IGameClient Client, SteamID SteamId, string Name)>();

        foreach (var (client, steamId, name) in candidates)
        {
            if (await _operations.HasActiveAsync(steamId, AdminOperationType.Gag).ConfigureAwait(false))
            {
                _logger.LogDebug("Skip gag for {SteamId}: already gagged", steamId);

                continue;
            }

            targetToApply.Add((client, steamId, name));
        }

        if (targetToApply.Count == 0)
        {
            return;
        }

        await _bridge.ModSharp.InvokeFrameActionAsync(() =>
        {
            var count = 0;

            foreach (var (client, steamId, name) in targetToApply)
            {
                var target = client.IsValid ? client : _bridge.ClientManager.GetGameClient(steamId);

                if (target is not null)
                {
                    _engine.ApplyOnline(issuer, target, AdminOperationType.Gag, duration, reason);
                }
                else
                {
                    // Target disconnected mid-command: still persist so it re-applies on reconnect.
                    _engine.ApplyOffline(issuer, steamId, name, AdminOperationType.Gag, duration, reason);
                }

                count++;
            }

            if (count > 0)
            {
                ctx.ReplySuccessKey("Admin.Gagged", "{0} Gagged {1}.", ctx.IssuerName, targetLabel);
            }
        });
    }

    private void OnCommandUngag(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Ungag", "Usage: ms_ungag <target> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        var reason = ctx.GetReason(2);

        _ = ExecuteUngagAsync(ctx, targets, targetLabel, reason, issuer)
            .ContinueWith(t =>
                          {
                              if (t.Exception?.InnerException is { } ex)
                              {
                                  _logger.LogError(ex, "Failed to process ungag batch");
                                  ctx.Reply("Failed to process ungag. Check server logs.");
                              }
                          },
                          TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ExecuteUngagAsync(CommandContext ctx,
        IReadOnlyList<IGameClient>                      targets,
        string                                          targetLabel,
        string                                          reason,
        IGameClient?                                    issuer)
    {
        var candidates = targets.Select(t => (Client: t, t.SteamId, t.Name)).ToList();

        var targetToRemove = new List<(IGameClient Client, SteamID SteamId, string Name)>();

        foreach (var (client, steamId, name) in candidates)
        {
            if (!await _operations.HasActiveAsync(steamId, AdminOperationType.Gag).ConfigureAwait(false))
            {
                _logger.LogDebug("Skip ungag for {SteamId}: not gagged", steamId);

                continue;
            }

            targetToRemove.Add((client, steamId, name));
        }

        if (targetToRemove.Count == 0)
        {
            return;
        }

        await _bridge.ModSharp.InvokeFrameActionAsync(() =>
        {
            var count = 0;

            foreach (var (client, steamId, name) in targetToRemove)
            {
                var target = client.IsValid ? client : _bridge.ClientManager.GetGameClient(steamId);

                if (target is not null)
                {
                    _engine.RemoveOnline(issuer, target, AdminOperationType.Gag, reason);
                }
                else
                {
                    // Target disconnected mid-command: still remove the stored record.
                    _engine.RemoveOffline(issuer, steamId, name, AdminOperationType.Gag, reason);
                }

                count++;
            }

            if (count > 0)
            {
                ctx.ReplySuccessKey("Admin.Ungagged", "{0} Ungagged {1}.", ctx.IssuerName, targetLabel);
            }
        });
    }

    public void Gag(IGameClient? admin, IGameClient target, TimeSpan? duration, string reason)
        => _engine.ApplyOnline(admin, target, AdminOperationType.Gag, duration, reason);

    public void Gag(IGameClient? admin, SteamID steamId, TimeSpan? duration, string reason)
        => _engine.ApplyOffline(admin, steamId, "Offline Player", AdminOperationType.Gag, duration, reason);

    public void Ungag(IGameClient? admin, IGameClient target, string reason)
        => _engine.RemoveOnline(admin, target, AdminOperationType.Gag, reason);
}
