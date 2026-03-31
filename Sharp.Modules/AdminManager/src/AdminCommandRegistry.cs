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

using System.Collections.Immutable;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.CommandCenter.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using IAdmin = Sharp.Modules.AdminManager.Shared.IAdmin;

namespace Sharp.Modules.AdminManager;

internal class AdminCommandRegistry : IAdminCommandRegistry
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly AdminManager     _self;
    private readonly string           _moduleIdentity;

    public AdminCommandRegistry(ICommandRegistry commandRegistry, AdminManager self, string moduleIdentity)
    {
        _commandRegistry = commandRegistry;
        _self            = self;
        _moduleIdentity  = moduleIdentity;
    }

    public void RegisterAdminCommand(string command,
        Action<IGameClient?, StringCommand> call,
        ImmutableArray<string>              permissions)
    {
        _commandRegistry.RegisterGenericCommand(command,
                                                (client, stringCommand) =>
                                                {
                                                    OnExecutingAdminCommand(client, stringCommand, call, permissions);
                                                });
    }

    public void RegisterPermissions(ImmutableArray<string> permissions)
    {
        _self.RegisterModulePermissions(_moduleIdentity, permissions);
    }

    private void OnExecutingAdminCommand(IGameClient? client,
        StringCommand                                 command,
        Action<IGameClient?, StringCommand>           call,
        ImmutableArray<string>                        permissions)
    {
        if (client is null)
        {
            call(null, command);

            return;
        }

        if (client.GetPlayerController() is not { } controller)
        {
            return;
        }

        var admin = _self.GetAdmin(client.SteamId);

        if (admin is null || !HasPermission(admin, permissions))
        {
            PrintNoAccess(client, command, controller);

            return;
        }

        call(client, command);
    }

    private void PrintNoAccess(IGameClient client, StringCommand command, IPlayerController controller)
    {
        const string prefix   = "[MS] ";
        const string fallback = "You do not have access to do this command.";

        var msg = prefix + GetLocalizedString(client, "AdminManager.NoPermission", fallback);

        if (command.ChatTrigger)
        {
            controller.Print(HudPrintChannel.Chat, msg);
        }
        else
        {
            client.ConsolePrint(msg);
        }
    }

    private string GetLocalizedString(IGameClient client, string key, string fallback)
    {
        if (_self.GetLocalizerManager() is { } lm)
        {
            if (lm.For(client).TryText(key, out var value))
            {
                return value;
            }
        }

        return fallback;
    }

    private static bool HasPermission(IAdmin admin, ImmutableArray<string> permissions)
    {
        foreach (var permission in permissions)
        {
            if (admin.HasPermission(permission))
            {
                return true;
            }
        }

        return false;
    }
}
