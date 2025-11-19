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
    ///     MV中最大移动速度
    /// </summary>
    float MaxSpeed { get; set; }

    /// <summary>
    ///     按键
    /// </summary>
    UserCommandButtons KeyButtons { get; set; }

    /// <summary>
    ///     变化的按键
    /// </summary>
    UserCommandButtons KeyChangedButtons { get; set; }

    /// <summary>
    ///     滚轮的按键
    /// </summary>
    UserCommandButtons ScrollButtons { get; set; }

    /// <summary>
    ///     转换为CCSPlayer_MovementServices
    /// </summary>
    /// <param name="reinterpret">
    ///     False: 如果当前MovementService不是CCSPlayer_MovementServices则返回null<br />True:
    ///     使用指针重新解析为CCSPlayer_MovementServices
    /// </param>
    IPlayerMovementService? AsPlayerMovementService(bool reinterpret = false);

    /// <summary>
    ///     临时修改MaxSpeed, 不发送
    /// </summary>
    void TransientChangeMaxSpeed(float speed);
}

[NetClass("CCSPlayer_MovementServices")]
public interface IPlayerMovementService : IMovementService
{
    /// <summary>
    ///     下蹲疲劳速度
    /// </summary>
    float DuckSpeed { get; set; }

    /// <summary>
    ///     按住Jump
    /// </summary>
    bool OldJumpPressed { get; set; }

    /// <summary>
    ///     上次按下Jump的时间
    /// </summary>
    float JumpPressedTime { get; set; }

    /// <summary>
    ///     耐力值
    /// </summary>
    float Stamina { get; set; }

    /// <summary>
    ///     强制下蹲
    /// </summary>
    bool DuckOverride { get; set; }

    /// <summary>
    ///     临时修改耐力值, 不发送
    /// </summary>
    void TransientChangeStamina(float stamina);
}
