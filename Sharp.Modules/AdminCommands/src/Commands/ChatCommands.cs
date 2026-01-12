using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class ChatCommands : ICommandCategory
{
    private readonly InterfaceBridge       _bridge;
    private readonly CommandContextFactory _contextFactory;
    private readonly ILogger<ChatCommands> _logger;

    public ChatCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _bridge         = bridge;
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<ChatCommands>();
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("say",  OnCommandSay,  ["admin:say"]);
        registry.RegisterAdminCommand("csay", OnCommandCsay, ["admin:csay"]);
        registry.RegisterAdminCommand("hsay", OnCommandHsay, ["admin:hsay"]);
        registry.RegisterAdminCommand("psay", OnCommandPsay, ["admin:psay"]);
    }

    private void OnCommandSay(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Say", "Usage: ms_say <message>"))
        {
            return;
        }

        var message = command.ArgString.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            ctx.ReplyKey("Admin.Usage.Say", "Usage: ms_say <message>");

            return;
        }

        var senderName = issuer is null ? "Console" : issuer.Name;
        var msg        = $" {ChatColor.Green}{senderName}{ChatColor.White}: {message}";

        _bridge.ModSharp.PrintToChatAll(msg);
    }

    private void OnCommandCsay(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Csay", "Usage: ms_csay <message>"))
        {
            return;
        }

        var message = command.ArgString.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            ctx.ReplyKey("Admin.Usage.Csay", "Usage: ms_csay <message>");

            return;
        }

        _bridge.ModSharp.PrintChannelAll(HudPrintChannel.Center, message);
    }

    private void OnCommandHsay(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Hsay", "Usage: ms_hsay <message>"))
        {
            return;
        }

        var message = command.ArgString.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            ctx.ReplyKey("Admin.Usage.Hsay", "Usage: ms_hsay <message>");

            return;
        }

        _bridge.ModSharp.PrintChannelAll(HudPrintChannel.Hint, message);
    }

    private void OnCommandPsay(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(2, "Admin.Usage.Psay", "Usage: ms_psay <target> <message>"))
        {
            return;
        }

        if (!ctx.TryGetSingleTarget(1, out var target))
        {
            return;
        }

        var message = CommandHelpers.GetRemainingArgs(command, 2);

        if (string.IsNullOrWhiteSpace(message))
        {
            ctx.ReplyKey("Admin.Usage.Psay", "Usage: ms_psay <target> <message>");

            return;
        }

        target.GetPlayerController()?.Print(HudPrintChannel.Chat, message);

        ctx.ReplyKey("Admin.Psay.Sent", "Sent private message to {0}.", target.Name);
    }
}
