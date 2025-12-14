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
    ///     Get CSGOUserCmd <br />
    ///     <remarks>Please use ref when getting member struct</remarks>
    /// </summary>
    CCSGOUserCmdPb* CSGOUserCmd { get; }

    /// <summary>
    ///     Get BaseUserCmd <br />
    ///     <remarks>Please use ref when getting member struct</remarks>
    /// </summary>
    CBaseUserCmdPb* BaseUserCmd { get; }

    UserCommandButtons KeyButtons     { get; set; }
    UserCommandButtons ChangedButtons { get; set; }
    UserCommandButtons ScrollButtons  { get; set; }

    int InputHistorySize { get; }

    /// <summary>
    ///     Get InputHistory
    /// </summary>
    /// <returns>Returns null if out of range</returns>
    CCSGOInputHistoryEntryPb* GetInputHistoryEntry(int index);

    int SubtickMoveSize { get; }

    /// <summary>
    ///     Get SubtickMove
    /// </summary>
    /// <returns>Returns null if out of range</returns>
    CSubtickMoveStepPb* GetSubtickMove(int index);
}
