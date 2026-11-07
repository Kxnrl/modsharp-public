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

using System;
using Microsoft.Extensions.Logging;
using Sharp.Shared.HookParams;

namespace Sharp.Core.Managers.Forwards;

internal class PlayerWalkMoveForward : ForwardType<PlayerWalkMoveForwardParams, PlayerWalkMoveForward>
{
    public PlayerWalkMoveForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerWalkMove += CCSPlayerMovementService_PlayerWorkMove;

    private void CCSPlayerMovementService_PlayerWorkMove(nint ptrClient,
        nint                                                  ptrController,
        nint                                                  ptrPawn,
        nint                                                  ptrService,
        nint                                                  ptrMoveData,
        ref int                                               speed)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerWalkMoveForwardParams(false, ptrClient, ptrController, ptrPawn, ptrService, ptrMoveData, speed);
        InvokeForward(param);
        speed = param.Speed;
        param.MarkAsDisposed();
    }
}

internal sealed class PlayerWalkMoveForwardParams : PlayerMovementWithDataFunctionParams, IPlayerWalkMoveForwardParams
{
    public PlayerWalkMoveForwardParams(bool postHook,
        IntPtr                              client,
        IntPtr                              controller,
        IntPtr                              pawn,
        IntPtr                              ptrService,
        IntPtr                              ptrInfo,
        int                                 speed) : base(postHook, client, controller, pawn, ptrService, ptrInfo)
        => Speed = speed;

    public int Speed { get; private set; }

    public void SetSpeed(int speed)
    {
        CheckDisposed();
        CheckEditable();

        Speed = speed;
    }
}
