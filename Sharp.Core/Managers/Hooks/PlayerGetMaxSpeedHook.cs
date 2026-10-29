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
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;

namespace Sharp.Core.Managers.Hooks;

internal class PlayerGetMaxSpeedHook : HookType<PlayerGetMaxSpeedHookParams, float, PlayerGetMaxSpeedHook>
{
    public PlayerGetMaxSpeedHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Player.OnPlayerGetMaxSpeed += CCSPlayerPawn_GetMaxSpeed;

    private HookReturnValue<float> CCSPlayerPawn_GetMaxSpeed(nint ptrClient,
        nint                                                      ptrController,
        nint                                                      ptrPawn,
        float                                                     originalSpeed)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<float>(EHookAction.Ignored, originalSpeed);
        }

        var param  = new PlayerGetMaxSpeedHookParams(false, ptrClient, ptrController, ptrPawn, originalSpeed);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result;
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;
}

internal sealed class PlayerGetMaxSpeedHookParams : PlayerPawnFunctionParams, IPlayerGetMaxSpeedHookParams
{
    public PlayerGetMaxSpeedHookParams(bool postHook, nint client, nint controller, nint pawn, float speed) : base(postHook,
        client,
        controller,
        pawn)
        => OriginalSpeed = speed;

    public float OriginalSpeed { get; }
}
