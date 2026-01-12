using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal class KickCommands : ICommandCategory
{
    private readonly InterfaceBridge       _bridge;
    private readonly CommandContextFactory _contextFactory;
    private readonly ILogger<KickCommands> _logger;

    public KickCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _bridge         = bridge;
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<KickCommands>();
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
