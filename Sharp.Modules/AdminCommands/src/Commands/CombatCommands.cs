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
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class CombatCommands : ICommandCategory
{
    private readonly ILogger<CombatCommands> _logger;
    private readonly CommandContextFactory   _contextFactory;

    public CombatCommands(ILogger<CombatCommands> logger, CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _contextFactory = contextFactory;
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("slay",    OnCommandSlay,    ["admin:slay"]);
        registry.RegisterAdminCommand("slap",    OnCommandSlap,    ["admin:slap"]);
        registry.RegisterAdminCommand("hp",      OnCommandHp,      ["admin:hp"]);
        registry.RegisterAdminCommand("respawn", OnCommandRespawn, ["admin:respawn"]);
        registry.RegisterAdminCommand("god",     OnCommandGod,     ["admin:god"]);
    }

    private void OnCommandGod(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.God", "Usage: ms_god <target> [on/off]"))
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
            if (!CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                continue;
            }

            var isGodModeNow = forcedState ?? pawn.AllowTakesDamage;

            pawn.AllowTakesDamage = !isGodModeNow;
            count++;
        }

        if (count > 0)
        {
            if (forcedState.HasValue)
            {
                var key = forcedState.Value ? "Admin.God.Enabled" : "Admin.God.Disabled";
                var def = forcedState.Value ? "{0} enabled godmode on {1}." : "{0} disabled godmode on {1}.";
                ctx.ReplySuccessKey(key, def, ctx.IssuerName, targetLabel);
            }
            else
            {
                ctx.ReplySuccessKey("Admin.God.Toggled", "{0} toggled godmode on {1}.", ctx.IssuerName, targetLabel);
            }
        }
    }

    private void OnCommandSlay(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Slay", "Usage: ms_slay <target>"))
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
            if (!CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                continue;
            }

            pawn.AllowTakesDamage = true;
            pawn.Slay();
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Slay", "{0} Slain {1}.", ctx.IssuerName, targetLabel);
        }
    }

    private void OnCommandSlap(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Slap", "Usage: ms_slap <target> [damage]"))
        {
            return;
        }

        var damage = 0;

        if (command.ArgCount >= 2 && int.TryParse(command.GetArg(2), out var parsed) && parsed >= 0)
        {
            damage = parsed;
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

            ApplySlap(pawn, damage);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Slap", "{0} Slapped {1} for {2} damage.", ctx.IssuerName, targetLabel, damage);
        }
    }

    private void OnCommandHp(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Hp", "Usage: ms_hp <target> <amount>"))
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out var hp) || hp <= 0)
        {
            ctx.ReplyKey("Admin.InvalidNumber", "Health must be a positive number.");

            return;
        }

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

            if (hp > pawn.MaxHealth)
            {
                pawn.MaxHealth = hp;
            }

            pawn.Health = hp;
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Hp", "{0} Set {1}'s health to {2}.", ctx.IssuerName, targetLabel, hp);
        }
    }

    private void OnCommandRespawn(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Respawn", "Usage: ms_respawn <target>"))
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
            if (target.GetPlayerController() is not { } controller)
            {
                continue;
            }

            controller.Respawn();
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Respawn", "{0} Respawned {1}.", ctx.IssuerName, targetLabel);
        }
    }

    private static void ApplySlap(IPlayerPawn pawn, int damage)
    {
        try
        {
            var rnd = Random.Shared;

            var horizontal = rnd.NextSingle() * 200f;
            var direction  = rnd.NextSingle() > 0.5f ? 1f : -1f;
            var velocity   = new Vector(horizontal * direction, horizontal * -direction, 250f);

            pawn.ApplyAbsVelocityImpulse(velocity);
        }
        catch
        {
            // ignored
        }

        if (damage <= 0)
        {
            return;
        }

        var newHealth = pawn.Health - damage;

        if (newHealth <= 0)
        {
            pawn.Slay();
        }
        else
        {
            pawn.Health = newHealth;
        }
    }
}
