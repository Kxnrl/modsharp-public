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
using Sharp.Shared.HookParams;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.AdminCommands.Services.Handlers;

internal class MuteHandler : IAdminOperationHandler, IAdminOperationHookRegistrar
{
    private readonly InterfaceBridge _bridge;

    private readonly Dictionary<SteamID, DateTime?> _mutes;

    private bool _hooksRegistered;

    public MuteHandler(InterfaceBridge bridge)
    {
        _bridge = bridge;

        _mutes = [];
    }

    public AdminOperationType Type => AdminOperationType.Mute;

    public void OnApplied(AdminOperationRecord record, IGameClient? targetClient)
        => SetMuted(record.SteamId, true, record.ExpiresAt);

    public void OnRemoved(SteamID targetId, IGameClient? targetClient)
        => SetMuted(targetId, false);

    public (string Key, string Fallback) GetAppliedNotification(IGameClient target, string durationText)
        => ("Admin.MuteApplied", $"muted {target.Name} {durationText}");

    public (string Key, string Fallback) GetRemovedNotification(IGameClient target)
        => ("Admin.MuteRemoved", $"unmuted {target.Name}");

    public void RegisterHooks()
    {
        if (_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ClientCanHear.InstallHookPre(OnClientCanHearPre);
        _hooksRegistered = true;
    }

    public void UnregisterHooks()
    {
        if (!_hooksRegistered)
        {
            return;
        }

        _bridge.HookManager.ClientCanHear.RemoveHookPre(OnClientCanHearPre);
        _hooksRegistered = false;
    }

    private HookReturnValue<bool> OnClientCanHearPre(IClientCanHearHookParams @params, HookReturnValue<bool> ret)
        => IsMuted(@params.Speaker.SteamId)
            ? new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride, false)
            : new HookReturnValue<bool>(EHookAction.Ignored);

    private bool IsMuted(SteamID steamId)
    {
        if (!_mutes.TryGetValue(steamId, out var expiresAt))
        {
            return false;
        }

        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            _mutes.Remove(steamId);

            return false;
        }

        return true;
    }

    private void SetMuted(SteamID steamId, bool muted, DateTime? expiresAt = null)
    {
        if (muted)
        {
            _mutes[steamId] = expiresAt;
        }
        else
        {
            _mutes.Remove(steamId);
        }
    }
}
