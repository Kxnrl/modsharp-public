using System.Globalization;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class MovementCommands : ICommandCategory
{
    private readonly InterfaceBridge           _bridge;
    private readonly CommandContextFactory     _contextFactory;
    private readonly ILogger<MovementCommands> _logger;

    public MovementCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _bridge         = bridge;
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<MovementCommands>();
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("noclip",  OnCommandNoclip,   ["admin:noclip"]);
        registry.RegisterAdminCommand("speed",   OnCommandSpeed,    ["admin:speed"]);
        registry.RegisterAdminCommand("gravity", OnCommandGravity,  ["admin:gravity"]);
        registry.RegisterAdminCommand("tp",      OnCommandTeleport, ["admin:tp"]);
        registry.RegisterAdminCommand("bring",   OnCommandBring,    ["admin:bring"]);
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

        var current = false;

        foreach (var target in targets)
        {
            if (CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                current = pawn.MoveType == MoveType.NoClip;

                break;
            }
        }

        var enable = CommandHelpers.ShouldEnable(command, 2, current);

        var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(target, out var pawn, true))
            {
                continue;
            }

            pawn.SetMoveType(enable ? MoveType.NoClip : MoveType.Walk);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey(enable ? "Admin.Noclip.Enabled" : "Admin.Noclip.Disabled",
                                enable ? "{0} Enabled noclip for {1}." : "{0} Disabled noclip for {1}.",
                                ctx.IssuerName,
                                targetLabel);
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

        ctx.Reply("Not implemented!!!");

        /*var count = 0;

        foreach (var target in targets)
        {
            if (!CommandHelpers.TryGetPawn(ctx, target, out var pawn))
            {
                continue;
            }

            if (target.GetPlayerController() is { } controller)
            {
                controller.LaggedMovement *= speed;
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
        }*/
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

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        if (CommandHelpers.TryParseVector(command, 2, out var position))
        {
            var count = 0;

            foreach (var target in targets)
            {
                if (!CommandHelpers.TryGetPawn(target, out var pawn))
                {
                    continue;
                }

                pawn.Teleport(position);
                count++;
            }

            if (count > 0)
            {
                ctx.ReplySuccessKey("Admin.Teleport",
                                    "Teleported {0} to {1}.",
                                    targetLabel,
                                    CommandHelpers.FormatVector(position));
            }
        }
        else
        {
            if (!ctx.TryGetTargets(2, out var destinations, out var destLabel))
            {
                return;
            }

            var destination = destinations[0];

            if (!CommandHelpers.TryGetPawn(destination, out var destPawn))
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

                pawn.Teleport(destPawn.GetAbsOrigin(), destPawn.GetAbsAngles(), destPawn.GetAbsVelocity());
                count++;
            }

            if (count > 0)
            {
                ctx.ReplySuccessKey("Admin.Teleport", "{0} Teleported {1} to {2}.", ctx.IssuerName, targetLabel, destLabel);
            }
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
}
