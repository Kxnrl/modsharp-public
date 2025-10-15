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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;

namespace Sharp.Core.Bridges.Forwards.Extern;

internal static class DamageProcessor
{
    public delegate EHookAction DelegatePlayerDispatchTraceAttackPre(nint ptrClient,
        nint                                                              ptrController,
        nint                                                              ptrPawn,
        nint                                                              ptrInfo,
        nint                                                              ptrResult);

    public delegate void DelegatePlayerDispatchTraceAttackPost(nint ptrClient,
        nint                                                        ptrController,
        nint                                                        ptrPawn,
        nint                                                        ptrInfo,
        nint                                                        ptrResult,
        EHookAction                                                 action);

    public delegate EHookAction DelegateEntityDispatchTraceAttackPre(nint ptrEntity, nint ptrInfo, nint ptrResult);

    public delegate void DelegateEntityDispatchTraceAttackPost(nint ptrEntity,
        nint                                                        ptrInfo,
        nint                                                        ptrResult,
        EHookAction                                                 action);

    public static event DelegatePlayerDispatchTraceAttackPre?  OnPlayerDispatchTraceAttackPre;
    public static event DelegateEntityDispatchTraceAttackPre?  OnEntityDispatchTraceAttackPre;
    public static event DelegatePlayerDispatchTraceAttackPost? OnPlayerDispatchTraceAttackPost;
    public static event DelegateEntityDispatchTraceAttackPost? OnEntityDispatchTraceAttackPost;

    [UnmanagedCallersOnly]
    public static EHookAction OnPlayerDispatchTraceAttackPreExport(nint ptrClient,
        nint                                                            ptrController,
        nint                                                            ptrPawn,
        nint                                                            ptrInfo,
        nint                                                            ptrResult)
    {
        if (OnPlayerDispatchTraceAttackPre is null)
        {
            return EHookAction.Ignored;
        }

        return OnPlayerDispatchTraceAttackPre.Invoke(ptrClient, ptrController, ptrPawn, ptrInfo, ptrResult);
    }

    [UnmanagedCallersOnly]
    public static EHookAction OnEntityDispatchTraceAttackPreExport(nint ptrEntity, nint ptrInfo, nint ptrResult)
    {
        if (OnEntityDispatchTraceAttackPre is null)
        {
            return EHookAction.Ignored;
        }

        return OnEntityDispatchTraceAttackPre.Invoke(ptrEntity, ptrInfo, ptrResult);
    }

    [UnmanagedCallersOnly]
    public static void OnPlayerDispatchTraceAttackPostExport(nint ptrClient,
        nint                                                      ptrController,
        nint                                                      ptrPawn,
        nint                                                      ptrInfo,
        nint                                                      ptrResult,
        EHookAction                                               action)
        => OnPlayerDispatchTraceAttackPost?.Invoke(ptrClient, ptrController, ptrPawn, ptrInfo, ptrResult, action);

    [UnmanagedCallersOnly]
    public static void OnEntityDispatchTraceAttackPostExport(nint ptrEntity,
        nint                                                      ptrInfo,
        nint                                                      ptrResult,
        EHookAction                                               action)
        => OnEntityDispatchTraceAttackPost?.Invoke(ptrEntity, ptrInfo, ptrResult, action);
}
