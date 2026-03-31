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

using Sharp.Modules.AdminCommands.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal class GagHandler : IAdminOperationHandler, IAdminOperationHookRegistrar, IClientListener
{
    private readonly InterfaceBridge _bridge;

    private readonly Dictionary<SteamID, DateTime?> _gags;

    private bool _hooksRegistered;

    public GagHandler(InterfaceBridge bridge)
    {
        _bridge = bridge;

        _gags = [];
    }

    public int ListenerVersion  => IClientListener.ApiVersion;
    public int ListenerPriority => byte.MaxValue;

    public AdminOperationType Type => AdminOperationType.Gag;

    public void OnApplied(AdminOperationRecord record, IGameClient? targetClient)
        => SetGagged(record.SteamId, true, record.ExpiresAt);

    public void OnRemoved(SteamID targetId, IGameClient? targetClient)
        => SetGagged(targetId, false);

    public (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText)
        => ("Admin.GagApplied", $"gagged {target.Name} {durationText}");

    public (string Key, string Fallback) GetRemovedNotification(IGameClient target)
        => ("Admin.GagRemoved", $"ungagged {target.Name}");

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _bridge.ClientManager.InstallClientListener(this);
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        _bridge.ClientManager.RemoveClientListener(this);
        _hooksRegistered = false;
    }

    public ECommandAction OnClientSayCommand(IGameClient client,
        bool                                             teamOnly,
        bool                                             isCommand,
        string                                           commandName,
        string                                           message)
        => IsGagged(client.SteamId) && !isCommand ? ECommandAction.Stopped : ECommandAction.Skipped;

    private bool IsGagged(SteamID steamId)
    {
        if (!_gags.TryGetValue(steamId, out var expiresAt))
        {
            return false;
        }

        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            _gags.Remove(steamId);

            return false;
        }

        return true;
    }

    private void SetGagged(SteamID steamId, bool gagged, DateTime? expiresAt = null)
    {
        if (gagged)
        {
            _gags[steamId] = expiresAt;
        }
        else
        {
            _gags.Remove(steamId);
        }
    }
}
