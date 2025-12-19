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

using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;

namespace Sharp.Shared.GameObjects;

[NetClass("CPlayer_MovementServices")]
public interface IMovementService : IPlayerPawnComponent
{
    /// <summary>
    ///     MaxSpeed in MoveData
    /// </summary>
    float MaxSpeed { get; set; }

    /// <summary>
    ///     Pressed keyboard buttons
    /// </summary>
    UserCommandButtons KeyButtons { get; set; }

    /// <summary>
    ///     Changed keyboard buttons
    /// </summary>
    UserCommandButtons KeyChangedButtons { get; set; }

    /// <summary>
    ///     Buttons that are done by scrolling
    /// </summary>
    UserCommandButtons ScrollButtons { get; set; }

    /// <summary>
    ///     Cast to CCSPlayer_MovementServices
    /// </summary>
    /// <param name="reinterpret">
    ///     False: Returns null if current MovementService is not CCSPlayer_MovementServices<br />True:
    ///     Reinterpret pointer as CCSPlayer_MovementServices
    /// </param>
    IPlayerMovementService? AsPlayerMovementService(bool reinterpret = false);

    /// <summary>
    ///     Temporarily change MaxSpeed without sending network update
    /// </summary>
    void TransientChangeMaxSpeed(float speed);
}

[NetClass("CCSPlayer_MovementServices")]
public interface IPlayerMovementService : IMovementService
{
    /// <summary>
    ///     Duck speed, the lower it is the longer it takes to fully duck
    /// </summary>
    float DuckSpeed { get; set; }

    /// <summary>
    ///     Jump button held down
    /// </summary>
    bool OldJumpPressed { get; set; }

    /// <summary>
    ///     When does the player press their jump button
    /// </summary>
    float JumpPressedTime { get; set; }

    /// <summary>
    ///     Stamina value
    /// </summary>
    float Stamina { get; set; }

    /// <summary>
    ///     Force duck/crouch
    /// </summary>
    bool DuckOverride { get; set; }

    /// <summary>
    ///     Temporarily change stamina without sending network update
    /// </summary>
    void TransientChangeStamina(float stamina);
}
