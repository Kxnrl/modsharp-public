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

using Sharp.Shared.Enums;
using Sharp.Shared.Units;

namespace Sharp.Core.Bridges.Natives;

public static partial class Transmit
{
    public static partial bool AddEntityHooks(nint entity, bool defaultTransmit);

    public static partial bool RemoveEntHooks(nint entity);

    public static partial bool IsEntityHooked(nint entity);

    public static partial bool GetEntityState(EntityIndex entity, EntityIndex client, int channel);

    public static partial bool SetEntityState(EntityIndex entity, EntityIndex client, bool stats, int channel);

    public static partial bool GetEntityBlock(EntityIndex entity);

    public static partial bool SetEntityBlock(EntityIndex entity, bool state);

    public static partial int GetEntityOwner(EntityIndex entity);

    public static partial bool SetEntityOwner(EntityIndex entity, EntityIndex owner);

    public static partial NetworkReceiver GetEntityReceiver(EntityIndex entity);

    public static partial bool GetTempEntState(BlockTempEntType type, PlayerSlot slot);

    public static partial bool SetTempEntState(BlockTempEntType type, PlayerSlot slot, bool state);

    public static partial void ClearReceiverState(EntityIndex receiver);

    public static partial TransmitFireBulletState GetWeaponFireBulletState(nint weapon);

    public static partial void SetWeaponFireBulletState(nint weapon, TransmitFireBulletState state);
}
