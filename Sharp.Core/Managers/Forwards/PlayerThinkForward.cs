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
using Sharp.Shared.HookParams;

namespace Sharp.Core.Managers.Forwards;

internal class PlayerPreThinkForward : ForwardType<PlayerThinkForwardParams, PlayerPreThinkForward>
{
    public PlayerPreThinkForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerPreThink += CCSPlayerPawn_PreThink;

    private void CCSPlayerPawn_PreThink(nint ptrClient, nint ptrController, nint ptrPawn)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerThinkForwardParams(false, ptrClient, ptrController, ptrPawn);
        InvokeForward(param);
        param.MarkAsDisposed();
    }
}

internal class PlayerPostThinkForward : ForwardType<PlayerThinkForwardParams, PlayerPostThinkForward>
{
    public PlayerPostThinkForward(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerPostThink += CCSPlayerPawn_PostThink;

    private void CCSPlayerPawn_PostThink(nint ptrClient, nint ptrController, nint ptrPawn)
    {
        if (!IsForwardInvokeRequired())
        {
            return;
        }

        var param = new PlayerThinkForwardParams(true, ptrClient, ptrController, ptrPawn);
        InvokeForward(param);
        param.MarkAsDisposed();
    }
}

internal sealed class PlayerThinkForwardParams : PlayerPawnFunctionParams, IPlayerThinkForwardParams
{
    public PlayerThinkForwardParams(bool postHook, nint client, nint controller, nint pawn) : base(postHook,
        client,
        controller,
        pawn)
    {
    }
}
