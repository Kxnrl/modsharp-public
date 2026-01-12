using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class InventoryCommands : ICommandCategory
{
    private readonly CommandContextFactory      _contextFactory;
    private readonly ILogger<InventoryCommands> _logger;

    public InventoryCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<InventoryCommands>();
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
