/*
 * ModSharp
 * Copyright (C) 2023-2025 Kxnrl. All Rights Reserved.
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
using Sharp.Modules.CommandManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using IAdmin = Sharp.Modules.AdminManager.Shared.IAdmin;

namespace Sharp.Modules.AdminManager;

internal class AdminCommandRegistry : IAdminCommandRegistry
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly AdminManager _self;
    private readonly ISharedSystem _shared;

    public AdminCommandRegistry(ICommandRegistry commandRegistry, AdminManager self, ISharedSystem shared)
    {
        _commandRegistry = commandRegistry;
        _self = self;
        _shared = shared;
    }

    public void RegisterAdminCommand(string command, Action<IGameClient?, StringCommand> call, ImmutableArray<string> permissions)
    {
        _commandRegistry.RegisterGenericCommand(command, (client, stringCommand) =>
        {
            OnExecutingAdminCommand(client, stringCommand, call, permissions);
        });
    }

    private void OnExecutingAdminCommand(IGameClient? client, StringCommand command, Action<IGameClient?, StringCommand> call, ImmutableArray<string> permissions)
    {
        if (client is null)
        {
            call(null, command);
            return;
        }

        var admin = _self.GetAdmin(client.SteamId);
        if (admin is null)
        {
            return;
        }

        if (HasPermission(admin, permissions))
        {
            call(client, command);
            return;
        }

        if (_shared.GetEntityManager().FindPlayerControllerBySlot(client.Slot) is not { } controller)
        {
            return;
        }

        if (!command.ChatTrigger)
        {
            client.ConsolePrint("[MS] You do not have access to do this command.");
        }

        controller.Print(HudPrintChannel.Chat, "[MS] You do not have access to do this command.");
    }

    private bool HasPermission(IAdmin admin, ImmutableArray<string> permissions)
    {
        return Enumerable.Any(permissions, admin.HasPermission);
    }
}