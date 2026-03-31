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
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class InventoryCommands : ICommandCategory
{
    private readonly ILogger<InventoryCommands> _logger;
    private readonly CommandContextFactory      _contextFactory;

    public InventoryCommands(ILogger<InventoryCommands> logger, CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("give",  OnCommandGive,  ["admin:give"]);
        registry.RegisterAdminCommand("strip", OnCommandStrip, ["admin:strip"]);
    }

    private void OnCommandGive(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Give", "Usage: ms_give <target> <weapon>"))
        {
            return;
        }

        var itemName = command.GetArg(2);

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                continue;
            }

            if (pawn.GiveNamedItem(itemName) is not { } weapon)
            {
                ctx.ReplyKey("Admin.GiveFailed", "Failed to give {0} to {1}.", itemName, target.Name);

                continue;
            }

            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Give", "{0} Gave {1} to {2}.", ctx.IssuerName, itemName, targetLabel);
        }
    }

    private void OnCommandStrip(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Strip", "Usage: ms_strip <target>"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn))
            {
                continue;
            }

            if (!pawn.IsAlive)
            {
                continue;
            }

            pawn.RemoveAllItems(true);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Strip", "{0} Stripped all items from {1}.", ctx.IssuerName, targetLabel);
        }
    }
}
