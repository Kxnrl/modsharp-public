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

using System.Globalization;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Common;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class MovementCommands : ICommandCategory
{
    private readonly ILogger<MovementCommands> _logger;
    private readonly CommandContextFactory     _contextFactory;

    public MovementCommands(ILogger<MovementCommands> logger, CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("noclip",   OnCommandNoclip,   ["admin:noclip"]);
        registry.RegisterAdminCommand("speed",    OnCommandSpeed,    ["admin:speed"]);
        registry.RegisterAdminCommand("gravity",  OnCommandGravity,  ["admin:gravity"]);
        registry.RegisterAdminCommand("tp",       OnCommandTeleport, ["admin:tp"]);
        registry.RegisterAdminCommand("bring",    OnCommandBring,    ["admin:bring"]);
        registry.RegisterAdminCommand("freeze",   OnCommandFreeze,   ["admin:freeze"]);
        registry.RegisterAdminCommand("unfreeze", OnCommandUnfreeze, ["admin:unfreeze"]);
    }

    public void Unregister()
    {
    }

    private void OnCommandNoclip(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Noclip", "Usage: ms_noclip <target> [on|off]"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        // If index 2 is missing, forcedState is null (Toggle mode).
        // If index 2 is invalid, it auto-replies error and returns false.
        if (!ctx.TryGetState(2, out var forcedState))
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

            var isNoClip = pawn.MoveType == MoveType.NoClip;
            var newState = forcedState ?? !isNoClip;

            pawn.SetMoveType(newState ? MoveType.NoClip : MoveType.Walk);
            count++;
        }

        if (count > 0)
        {
            if (forcedState.HasValue)
            {
                if (forcedState.Value)
                {
                    ctx.ReplySuccessKey("Admin.Noclip.Enabled", "{0} enabled noclip for {1}.", ctx.IssuerName, targetLabel);
                }
                else
                {
                    ctx.ReplySuccessKey("Admin.Noclip.Disabled", "{0} disabled noclip for {1}.", ctx.IssuerName, targetLabel);
                }
            }
            else
            {
                ctx.ReplySuccessKey("Admin.Noclip.Toggled", "{0} toggled noclip for {1}.", ctx.IssuerName, targetLabel);
            }
        }
    }

    private void OnCommandSpeed(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Speed", "Usage: ms_speed <target> <amount>"))
        {
            return;
        }

        if (!float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var speed) || speed <= 0f)
        {
            ctx.ReplyKey("Admin.InvalidNumber", "Speed must be a positive number.");

            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn) || !pawn.IsAlive)
            {
                continue;
            }

            if (target.GetPlayerController() is { } controller)
            {
                controller.LaggedMovement = speed;
            }

            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Speed",
                                "{0} Set {1}'s speed to {2}.",
                                ctx.IssuerName,
                                targetLabel,
                                speed.ToString("0.##", CultureInfo.InvariantCulture));
        }
    }

    private void OnCommandGravity(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Gravity", "Usage: ms_gravity <target> <scale>"))
        {
            return;
        }

        if (!float.TryParse(command.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var scale) || scale <= 0f)
        {
            ctx.ReplyKey("Admin.InvalidNumber", "Gravity scale must be a positive number.");

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

            pawn.SetGravityScale(scale);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Gravity",
                                "{0} Set {1}'s gravity scale to {2}.",
                                ctx.IssuerName,
                                targetLabel,
                                scale.ToString("0.##", CultureInfo.InvariantCulture));
        }
    }

    private void OnCommandTeleport(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Tp", "Usage: ms_tp <target> <destination|x y z>"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var sources, out var sourceLabel))
        {
            return;
        }

        Vector destPos;
        string destLabel;

        if (CommandHelpers.TryParseVector(command, 2, out var position))
        {
            destPos   = position;
            destLabel = CommandHelpers.FormatVector(position);
        }
        else
        {
            if (!ctx.TryGetTargets(2, out var destTargets, out var destTargetLabel))
            {
                return;
            }

            // We can't teleport TO multiple people at once.
            if (destTargets.Count != 1)
            {
                ctx.ReplyKey("Admin.Teleport.Ambiguous", "Destination must be a single target.");

                return;
            }

            if (!CommandHelpers.TryGetPawn(destTargets[0], out var destPawn, true))
            {
                return;
            }

            destPos   = destPawn.GetAbsOrigin();
            destLabel = destTargetLabel;
        }

        var count = 0;

        var emptyVector = new Vector(0, 0, 0);

        foreach (var target in sources)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                continue;
            }

            pawn.Teleport(destPos, null, emptyVector);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Teleport", "{0} Teleported {1} to {2}.", ctx.IssuerName, sourceLabel, destLabel);
        }
    }

    private void OnCommandBring(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Bring", "Usage: ms_bring <target>"))
        {
            return;
        }

        if (issuer is null)
        {
            ctx.ReplyKey("Admin.BringFailed", "Only players can use bring.");

            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        if (!CommandHelpers.TryGetPawn(issuer, out var issuerPawn))
        {
            return;
        }

        if (!issuerPawn.IsAlive)
        {
            ctx.ReplyKey("Admin.AliveToUse", "You have to be alive to use this command.");

            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn) || !pawn.IsAlive)
            {
                continue;
            }

            pawn.Teleport(issuerPawn.GetAbsOrigin());
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Bring", "{0} Brought {1} to them.", ctx.IssuerName, targetLabel);
        }
    }

    private void OnCommandFreeze(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Freeze", "Usage: ms_freeze <target> [on/off]"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        if (!ctx.TryGetState(2, out var forcedState))
        {
            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn) || !pawn.IsAlive)
            {
                continue;
            }

            var isCurrentlyFrozen = pawn.ActualMoveType == MoveType.None;
            var shouldFreeze      = forcedState ?? !isCurrentlyFrozen;

            pawn.SetMoveType(shouldFreeze ? MoveType.None : MoveType.Walk);
            count++;
        }

        if (count > 0)
        {
            if (forcedState.HasValue)
            {
                var key = forcedState.Value ? "Admin.Freeze.Enabled" : "Admin.Freeze.Disabled";
                var def = forcedState.Value ? "{0} frozen {1}." : "{0} unfrozen {1}.";
                ctx.ReplySuccessKey(key, def, ctx.IssuerName, targetLabel);
            }
            else
            {
                ctx.ReplySuccessKey("Admin.Freeze.Toggled", "{0} toggled frozen on {1}.", ctx.IssuerName, targetLabel);
            }
        }
    }

    private void OnCommandUnfreeze(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Unfreeze", "Usage: ms_unfreeze <target>"))
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
            if (!CommandHelpers.TryGetPawn(target, out var pawn) || !pawn.IsAlive)
            {
                continue;
            }

            pawn.SetMoveType(MoveType.Walk);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Freeze.Disabled", "{0} unfrozen {1}.", ctx.IssuerName, targetLabel);
        }
    }
}
