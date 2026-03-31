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
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class KickCommands : ICommandCategory
{
    private readonly ILogger<KickCommands> _logger;
    private readonly InterfaceBridge       _bridge;
    private readonly CommandContextFactory _contextFactory;

    public KickCommands(ILogger<KickCommands> logger, InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _bridge         = bridge;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("kick", OnCommandKick, ["admin:kick"]);
    }

    private void OnCommandKick(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Kick", "Usage: ms_kick <target> [reason]"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        var reason = ctx.GetReason(2);

        var adminName     = issuer?.Name ?? "Console";
        var targetName    = target.Name;
        var targetSteamId = target.SteamId;

        _bridge.ClientManager.KickClient(target, reason, NetworkDisconnectionReason.Kicked);

        ctx.ReplySuccessKey("Admin.Kicked", "{0} Kicked {1}.", adminName, targetName);

        _logger.LogInformation("Kick issued by {Admin}: {Target} ({SteamId}). Reason: {Reason}",
                               adminName,
                               targetName,
                               targetSteamId,
                               reason);
    }
}
