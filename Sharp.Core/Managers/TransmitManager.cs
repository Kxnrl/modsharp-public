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
using Sharp.Core.Bridges.Natives;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;
using Sharp.Shared.Units;

namespace Sharp.Core.Managers;

internal interface ICoreTransmitManager : ITransmitManager;

internal class TransmitManager : ICoreTransmitManager
{
    private readonly ILogger<TransmitManager> _logger;

    public TransmitManager(ILogger<TransmitManager> logger)
        => _logger = logger;

    public bool AddEntityHooks(IBaseEntity entity, bool defaultTransmit)
        => Transmit.AddEntityHooks(entity.GetAbsPtr(), defaultTransmit);

    public bool RemoveEntityHooks(IBaseEntity entity)
        => Transmit.RemoveEntHooks(entity.GetAbsPtr());

    public bool IsEntityHooked(IBaseEntity entity)
        => Transmit.IsEntityHooked(entity.GetAbsPtr());

    public bool GetEntityState(EntityIndex entity, EntityIndex controllerIndex, int channel)
        => Transmit.GetEntityState(entity, controllerIndex, channel);

    public bool SetEntityState(EntityIndex entity, EntityIndex controllerIndex, bool transmit, int channel)
        => Transmit.SetEntityState(entity, controllerIndex, transmit, channel);

    public bool GetEntityBlock(EntityIndex entity)
        => Transmit.GetEntityBlock(entity);

    public bool SetEntityBlock(EntityIndex entity, bool state)
        => Transmit.SetEntityBlock(entity, state);

    public int GetEntityOwner(EntityIndex entity)
        => Transmit.GetEntityOwner(entity);

    public bool SetEntityOwner(EntityIndex entity, EntityIndex owner)
        => Transmit.SetEntityOwner(entity, owner);

    public NetworkReceiver GetEntityReceiver(EntityIndex entity)
        => Transmit.GetEntityReceiver(entity);

    public bool GetTempEntState(BlockTempEntType type, PlayerSlot slot)
        => Transmit.GetTempEntState(type, slot);

    public void SetTempEntState(BlockTempEntType type, PlayerSlot slot, bool state)
        => Transmit.SetTempEntState(type, slot, state);

    public void ClearReceiverState(EntityIndex receiverIndex)
        => Transmit.ClearReceiverState(receiverIndex);

    public TransmitFireBulletState GetWeaponFireBulletState(IBaseWeapon weapon)
        => Transmit.GetWeaponFireBulletState(weapon.GetAbsPtr());

    public void SetWeaponFireBulletState(IBaseWeapon weapon, TransmitFireBulletState state)
        => Transmit.SetWeaponFireBulletState(weapon.GetAbsPtr(), state);
}
