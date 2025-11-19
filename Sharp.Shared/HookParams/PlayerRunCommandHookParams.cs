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
using Sharp.Shared.GameObjects;
using Sharp.Shared.Types.CppProtobuf;

namespace Sharp.Shared.HookParams;

public unsafe interface IPlayerRunCommandHookParams : IFunctionParams, IPlayerFunctionParams
{
    IBasePlayerPawn  Pawn    { get; }
    IMovementService Service { get; }

    /// <summary>
    ///     获取CSGOUserCmd <br />
    ///     <remarks>获取成员struct时请使用ref</remarks>
    /// </summary>
    CCSGOUserCmdPb* CSGOUserCmd { get; }

    /// <summary>
    ///     获取BaseUserCmd <br />
    ///     <remarks>获取成员struct时请使用ref</remarks>
    /// </summary>
    CBaseUserCmdPb* BaseUserCmd { get; }

    UserCommandButtons KeyButtons     { get; set; }
    UserCommandButtons ChangedButtons { get; set; }
    UserCommandButtons ScrollButtons  { get; set; }

    int InputHistorySize { get; }

    /// <summary>
    ///     获取InputHistory
    /// </summary>
    /// <returns>如果超出范围则返回null</returns>
    CCSGOInputHistoryEntryPb* GetInputHistoryEntry(int index);

    int SubtickMoveSize { get; }

    /// <summary>
    ///     获取SubtickMove
    /// </summary>
    /// <returns>如果超出范围则返回null</returns>
    CSubtickMoveStepPb* GetSubtickMove(int index);
}
