using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class IdentityCommands : ICommandCategory
{
    private readonly CommandContextFactory     _contextFactory;
    private readonly ILogger<IdentityCommands> _logger;

    public IdentityCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<IdentityCommands>();
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("rename", OnCommandRename, ["admin:rename"]);
        registry.RegisterAdminCommand("team",   OnCommandTeam,   ["admin:team"]);
        registry.RegisterAdminCommand("money",  OnCommandMoney,  ["admin:money"]);
    }

    private void OnCommandRename(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Rename", "Usage: ms_rename <target> <new name>"))
        {
            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        var newName = CommandHelpers.GetRemainingArgs(command, 2);

        if (string.IsNullOrWhiteSpace(newName))
        {
            ctx.ReplyKey("Admin.Usage.Rename", "Usage: ms_rename <target> <new name>");

            return;
        }

        var count = 0;

        foreach (var target in targets)
        {
            if (target.GetPlayerController() is not { } controller)
            {
                continue;
            }

            controller.PlayerName = newName;
            target.SetName(newName);
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Rename", "{0} Renamed {1} to {2}.", ctx.IssuerName, targetLabel, newName);
        }
    }

    private void OnCommandTeam(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Team", "Usage: ms_team <target> <ct|t|spec>"))
        {
            return;
        }

        if (!CommandHelpers.TryParseTeam(command.GetArg(2), out var team))
        {
            ctx.ReplyKey("Admin.InvalidTeam", "Team must be one of: ct, t, spec.");

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

            if (team <= CStrikeTeam.Spectator)
            {
                controller.ChangeTeam(team);
            }
            else
            {
                controller.SwitchTeam(team);
            }

            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Team", "{0} Moved {1} to {2}.", ctx.IssuerName, targetLabel, team);
        }
    }

    private void OnCommandMoney(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Money", "Usage: ms_money <target> <amount>"))
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out var amount))
        {
            ctx.ReplyKey("Admin.InvalidNumber", "Money must be a number.");

            return;
        }

        if (!ctx.TryGetTargets(1, out var targets, out var targetLabel))
        {
            return;
        }

        amount = Math.Clamp(amount, 0, 60000);

        var count = 0;

        foreach (var target in targets)
        {
            if (target.GetPlayerController() is not { } controller)
            {
                continue;
            }

            if (controller.GetInGameMoneyService() is not { } money)
            {
                continue;
            }

            money.Account = amount;
            count++;
        }

        if (count > 0)
        {
            ctx.ReplySuccessKey("Admin.Money", "{0} Set {1}'s money to {2}.", ctx.IssuerName, targetLabel, amount);
        }
    }
}
