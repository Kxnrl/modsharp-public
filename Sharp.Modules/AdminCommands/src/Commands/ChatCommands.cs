using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminCommands.Services.Internal;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class ChatCommands : ICommandCategory, IClientListener
{
    private const string SayPermission = "admin:say";

    private readonly InterfaceBridge       _bridge;
    private readonly CommandContextFactory _contextFactory;
    private readonly ModuleContext         _moduleContext;
    private readonly ILogger<ChatCommands> _logger;

    public ChatCommands(InterfaceBridge bridge, CommandContextFactory contextFactory, ModuleContext moduleContext)
    {
        _bridge         = bridge;
        _contextFactory = contextFactory;
        _moduleContext  = moduleContext;
        _logger         = bridge.LoggerFactory.CreateLogger<ChatCommands>();
    }

    public void Register(IAdminCommandRegistry registry)
    {
        _bridge.ClientManager.InstallClientListener(this);

        registry.RegisterAdminCommand("say",  OnCommandSay,  [SayPermission]);
        registry.RegisterAdminCommand("csay", OnCommandCsay, ["admin:csay"]);
        registry.RegisterAdminCommand("hsay", OnCommandHsay, ["admin:hsay"]);
        registry.RegisterAdminCommand("psay", OnCommandPsay, ["admin:psay"]);
    }

    public void Unregister()
    {
        _bridge.ClientManager.RemoveClientListener(this);
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

        BroadcastAdminMessage(ctx.IssuerName, message, HudPrintChannel.Chat);
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

        BroadcastAdminMessage(ctx.IssuerName, message, HudPrintChannel.Center);
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

        BroadcastAdminMessage(ctx.IssuerName, message, HudPrintChannel.Hint);
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

        if (_moduleContext.LocalizerManager is { } lm)
        {
            lm.For(target)
              .Message()
              .Prefix(null)
              .Literal($" ({ChatColor.Red}")
              .TextOrFallback("Admin.Tag.PM", "PM")
              .Literal($"{ChatColor.White}) {ChatColor.Green}{ctx.IssuerName}{ChatColor.White}: {message}")
              .Print();
        }
        else
        {
            var msg = $" {ChatColor.Red}(PM) {ChatColor.Green}{ctx.IssuerName}{ChatColor.White}: {message}";
            target.GetPlayerController()?.Print(HudPrintChannel.Chat, msg);
        }

        ctx.ReplyKey("Admin.Psay.Sent", "Sent private message to {0}.", target.Name);
    }

    public ECommandAction OnClientSayCommand(IGameClient client,
                                             bool        teamOnly,
                                             bool        isCommand,
                                             string      commandName,
                                             string      message)
    {
        if (!message.StartsWith('@') || _moduleContext.AdminManager is not { } adminManager)
        {
            return ECommandAction.Skipped;
        }

        if (adminManager.GetAdmin(client.SteamId) is { } admin && admin.HasPermission(SayPermission))
        {
            var actualMessage = message[1..].Trim();

            if (string.IsNullOrWhiteSpace(actualMessage))
            {
                return ECommandAction.Skipped;
            }

            BroadcastAdminMessage(client.Name, actualMessage, HudPrintChannel.Chat);

            return ECommandAction.Stopped;
        }

        return ECommandAction.Skipped;
    }

    private void BroadcastAdminMessage(string sender, string message, HudPrintChannel channel)
    {
        var useColors = channel == HudPrintChannel.Chat;

        if (_moduleContext.LocalizerManager is { } lm)
        {
            lm.ForMany(_bridge.ClientManager.GetGameClients(true))
              .Message()
              .Prefix(null)
              .Literal(useColors ? $" ({ChatColor.Red}" : "(")
              .TextOrFallback("Admin.Tag", "ADMIN")
              .Literal(useColors
                           ? $"{ChatColor.White}) {ChatColor.Green}{sender}{ChatColor.White}: {message}"
                           : $") {sender}: {message}")
              .Print(channel);
        }
        else
        {
            var msg = useColors
                ? $" {ChatColor.Red}(ADMIN) {ChatColor.Green}{sender}{ChatColor.White}: {message}"
                : $" (ADMIN) {sender}: {message}";

            if (channel == HudPrintChannel.Chat)
            {
                _bridge.ModSharp.PrintToChatAll(msg);
            }
            else
            {
                _bridge.ModSharp.PrintChannelAll(channel, msg);
            }
        }
    }

    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => 0;
}
