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
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers.Hooks;

internal class PlayerWeaponCanEquipHook : HookType<PlayerWeaponCanEquipHookParams, bool, PlayerWeaponCanEquipHook>
{
    public PlayerWeaponCanEquipHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerWeaponCanEquip += WeaponService_WeaponCanEquip;

    private HookReturnValue<bool> WeaponService_WeaponCanEquip(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrWeapon)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<bool>(EHookAction.Ignored, true);
        }

        var param  = new PlayerWeaponCanEquipHookParams(false, ptrClient, ptrController, ptrPawn, ptrWeapon);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class PlayerWeaponCanEquipHookParams : PlayerWeaponFunctionParams, IPlayerWeaponCanEquipHookParams
{
    public PlayerWeaponCanEquipHookParams(bool postHook, nint client, nint controller, nint pawn, nint weapon) : base(postHook,
        client,
        controller,
        pawn,
        weapon)
    {
    }
}
