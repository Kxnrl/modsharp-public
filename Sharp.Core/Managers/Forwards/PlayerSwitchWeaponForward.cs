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
using Sharp.Core.GameEntities;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.HookParams;

namespace Sharp.Core.Managers.Forwards;

internal class PlayerSwitchWeaponForward : ForwardType<PlayerSwitchWeaponForwardParams, PlayerSwitchWeaponForward>
{
    public PlayerSwitchWeaponForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerSwitchWeapon += WeaponService_SwitchWeapon;

    private void WeaponService_SwitchWeapon(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrService, nint ptrWeapon)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerSwitchWeaponForwardParams(true, ptrClient, ptrController, ptrPawn, ptrWeapon, ptrService);
        InvokeForward(param);
        param.MarkAsDisposed();
    }
}

internal sealed class PlayerSwitchWeaponForwardParams : PlayerPawnFunctionParams, IPlayerSwitchWeaponForwardParams
{
    private readonly nint _ptrService;
    private readonly nint _ptrWeapon;

    public PlayerSwitchWeaponForwardParams(bool postHook, nint client, nint controller, nint pawn, nint weapon, nint service) :
        base(postHook,
             client,
             controller,
             pawn)
    {
        _ptrService = service;
        _ptrWeapon  = weapon;
    }

    private IWeaponService? _service;

    public IWeaponService Service
    {
        get
        {
            if (_service is { } service)
            {
                return service;
            }

            CheckDisposed();
            _service = CreateWeaponService(_ptrService);

            return _service;
        }
    }

    private bool         _checkWeapon;
    private IBaseWeapon? _weapon;

    public IBaseWeapon? Weapon
    {
        get
        {
            if (_checkWeapon)
            {
                return _weapon;
            }

            CheckDisposed();
            _checkWeapon = true;
            _weapon      = BaseWeapon.Create(_ptrWeapon);

            return _weapon;
        }
    }
}
