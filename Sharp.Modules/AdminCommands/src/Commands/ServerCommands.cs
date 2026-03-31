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
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Sharp.Modules.AdminCommands.Commands;

internal sealed class ServerCommands : ICommandCategory
{
    private readonly InterfaceBridge         _bridge;
    private readonly CommandContextFactory   _contextFactory;
    private readonly ILogger<ServerCommands> _logger;

    public ServerCommands(ILogger<ServerCommands> logger, InterfaceBridge bridge, CommandContextFactory contextFactory)
    {
        _logger         = logger;
        _bridge         = bridge;
        _contextFactory = contextFactory;
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
