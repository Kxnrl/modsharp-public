using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class ServerCommands : ICommandCategory
{
    private readonly InterfaceBridge         _bridge;
    private readonly CommandContextFactory   _contextFactory;
    private readonly ILogger<ServerCommands> _logger;

    public ServerCommands(InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _bridge         = bridge;
        _contextFactory = contextFactory;
        _logger         = bridge.LoggerFactory.CreateLogger<ServerCommands>();
    }

    public void Register(IAdminCommandRegistry registry)
    {
        registry.RegisterAdminCommand("map",  OnCommandMap,  ["admin:map"]);
        registry.RegisterAdminCommand("rcon", OnCommandRcon, ["admin:rcon"]);
        registry.RegisterAdminCommand("cvar", OnCommandCvar, ["admin:cvar"]);
    }

    private void OnCommandMap(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Map", "Usage: ms_map <mapname>"))
        {
            return;
        }

        var map = command.GetArg(1);

        if (!_bridge.ModSharp.IsMapValid(map))
        {
            ctx.ReplyKey("Admin.InvalidMap", "Map '{0}' is not available.", map);

            return;
        }

        // hack, need to have something like ModSharp.GetMapType(map)
        var isWorkshopMaps = _bridge.ModSharp.ListWorkshopMaps()
                                    .Any(i => i.Name.Equals(map, StringComparison.OrdinalIgnoreCase));

        try
        {
            ctx.ReplySuccessKey("Admin.MapChanged", "{0}: Changing map to {1}...", ctx.IssuerName, map);

            _bridge.ModSharp.PushTimer(() =>
                                       {
                                           if (isWorkshopMaps)
                                           {
                                               _bridge.ModSharp.ServerCommand($"ds_workshop_changelevel {map}");

                                               return;
                                           }

                                           _bridge.ModSharp.ChangeLevel(map);
                                       },
                                       3.0f,
                                       GameTimerFlags.StopOnMapEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change map to {Map}", map);
            ctx.ReplyKey("Admin.MapFailed", "Failed to change map to {0}.", map);
        }
    }

    private void OnCommandRcon(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Rcon", "Usage: ms_rcon <command>"))
        {
            return;
        }

        var rconCommand = command.ArgString.Trim();

        if (string.IsNullOrWhiteSpace(rconCommand))
        {
            ctx.ReplyKey("Admin.Usage.Rcon", "Usage: ms_rcon <command>");

            return;
        }

        _bridge.ModSharp.ServerCommand(rconCommand);
        ctx.ReplyKey("Admin.RconSent", "Executed server command: {0}", rconCommand);
    }

    private void OnCommandCvar(IGameClient? issuer, StringCommand command)
    {
        var ctx = _contextFactory.Create(issuer, command, _logger);

        if (!ctx.RequireArgs(1, "Admin.Usage.Cvar", "Usage: ms_cvar <name> [value]"))
        {
            return;
        }

        var name = command.GetArg(1);
        var cvar = _bridge.ConVarManager.FindConVar(name, true);

        if (cvar is null)
        {
            ctx.ReplyKey("Admin.CvarMissing", "ConVar '{0}' not found.", name);

            return;
        }

        if (command.ArgCount >= 2)
        {
            var value = command.GetArg(2);
            cvar.SetString(value);
            ctx.ReplySuccessKey("Admin.CvarSet", "{0} Set {1} to {2}.", ctx.IssuerName, name, value);
        }
        else
        {
            var value = cvar.GetString();
            ctx.ReplyKey("Admin.CvarGet", "{0} = {1}", name, value);
        }
    }
}
