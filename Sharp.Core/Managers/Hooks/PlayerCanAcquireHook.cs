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

internal class PlayerCanAcquireHook : HookType<PlayerCanAcquireFunctionParams, EAcquireResult, PlayerCanAcquireHook>
{
    public PlayerCanAcquireHook(ILoggerFactory factory) : base(factory)
        => Bridges.Forwards.Extern.GiveNamedItem.OnCanAcquirePre += CCSPlayerItemServices_CanAcquire;

    protected override bool AllowPre  => true;
    protected override bool AllowPost => false;

    protected override HookActionFlags SupportedActions => HookActionFlags.Ignored | HookActionFlags.SkipCallReturnOverride;

    private HookReturnValue<EAcquireResult> CCSPlayerItemServices_CanAcquire(nint ptrClient,
        nint                                                                      ptrController,
        nint                                                                      ptrPawn,
        ushort                                                                    itemIndex,
        EAcquireMethod                                                            method,
        EAcquireResult                                                            defaultResult)
    {
        if (!IsHookInvokeRequired(false))
        {
            return new HookReturnValue<EAcquireResult>(EHookAction.Ignored, EAcquireResult.Allowed);
        }

        var param = new PlayerCanAcquireFunctionParams(false,
                                                       ptrClient,
                                                       ptrController,
                                                       ptrPawn,
                                                       itemIndex,
                                                       method,
                                                       defaultResult);

        var result = InvokeHookPre(param);

        param.MarkAsDisposed();

        return result;
    }
}

internal sealed class PlayerCanAcquireFunctionParams : PlayerPawnFunctionParams, IPlayerCanAcquireHookParams
{
    public ushort         ItemDefinitionIndex { get; }
    public EAcquireMethod Method              { get; }
    public EAcquireResult DefaultResult       { get; }

    public PlayerCanAcquireFunctionParams(bool postHook,
        nint                                   client,
        nint                                   controller,
        nint                                   pawn,
        ushort                                 index,
        EAcquireMethod                         method,
        EAcquireResult                         defaultResult) :
        base(postHook,
             client,
             controller,
             pawn)
    {
        ItemDefinitionIndex = index;
        Method              = method;
        DefaultResult       = defaultResult;
    }
}
