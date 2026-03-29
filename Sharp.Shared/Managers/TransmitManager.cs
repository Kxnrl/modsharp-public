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

using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface ITransmitManager
{
    /// <summary>
    ///     Hook an entity
    /// </summary>
    /// <param name="entity">Entity instance</param>
    /// <param name="defaultTransmit">Default state for all channels</param>
    bool AddEntityHooks(IBaseEntity entity, bool defaultTransmit);

    /// <summary>
    ///     Manually remove hooks
    ///     <remarks>Hooks are automatically destroyed when the entity is deleted</remarks>
    /// </summary>
    bool RemoveEntityHooks(IBaseEntity entity);

    /// <summary>
    ///     Check if entity is already hooked
    /// </summary>
    /// <param name="entity">Entity instance</param>
    bool IsEntityHooked(IBaseEntity entity);

    /// <summary>
    ///     Get entity visibility state for a controller
    /// </summary>
    /// <param name="entity">Entity index</param>
    /// <param name="controllerIndex">Controller index</param>
    /// <param name="channel">Channel, -1 to read global state</param>
    bool GetEntityState(EntityIndex entity, EntityIndex controllerIndex, int channel = -1);

    /// <summary>
    ///     Set entity visibility state for a controller
    /// </summary>
    /// <param name="entity">Entity index</param>
    /// <param name="controllerIndex">Controller index</param>
    /// <param name="transmit">Whether visible</param>
    /// <param name="channel">Channel</param>
    bool SetEntityState(EntityIndex entity, EntityIndex controllerIndex, bool transmit, int channel);

    /// <summary>
    ///     Get whether entity is blocked
    /// </summary>
    bool GetEntityBlock(EntityIndex entity);

    /// <summary>
    ///     Set entity block state
    /// </summary>
    bool SetEntityBlock(EntityIndex entity, bool state);

    /// <summary>
    ///     Get entity owner from hook
    /// </summary>
    /// <returns>-2 = NoHook | -1 = Null | other = Entity Index</returns>
    int GetEntityOwner(EntityIndex entity);

    /// <summary>
    ///     Set entity owner in hook
    /// </summary>
    /// <param name="entity">Entity index</param>
    /// <param name="owner">Owner entity index</param>
    bool SetEntityOwner(EntityIndex entity, EntityIndex owner);

    /// <summary>
    ///     Checks if a specific temporary entity type is currently blocked for a player.
    /// </summary>
    /// <param name="type">Temporary entity type</param>
    /// <param name="slot">IGameClient slot</param>
    /// <returns><see langword="true"/> if the entity type is blocked; otherwise, <see langword="false"/>.</returns>
    bool GetTempEntState(BlockTempEntType type, PlayerSlot slot);

    /// <summary>
    ///     Configures whether a specific temporary entity type is blocked for a player.
    /// </summary>
    /// <param name="type">Temporary entity type</param>
    /// <param name="slot">IGameClient slot</param>
    /// <param name="state">If <see langword="true"/>, the entity type will be blocked (not sent to the client). If <see langword="false"/>, it will be allowed.</param>
    void SetTempEntState(BlockTempEntType type, PlayerSlot slot, bool state);

    /// <summary>
    ///     Reset all entity states for a receiver
    /// </summary>
    /// <param name="receiverIndex">Receiver controller index</param>
    void ClearReceiverState(EntityIndex receiverIndex);

    /// <summary>
    ///     Gets the blocking state for fire bullet effects (e.g., sound, visual effects).
    /// </summary>
    /// <param name="weapon">The weapon instance to check.</param>
    /// <returns>The current transmission or blocking state.</returns>
    TransmitFireBulletState GetWeaponFireBulletState(IBaseWeapon weapon);

    /// <summary>
    ///     Sets the blocking state for fire bullet effects (e.g., sound, visual effects).
    /// </summary>
    void SetWeaponFireBulletState(IBaseWeapon weapon, TransmitFireBulletState state);
}
