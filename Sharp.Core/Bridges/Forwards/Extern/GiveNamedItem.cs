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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace Sharp.Core.Bridges.Forwards.Extern;

public static class GiveNamedItem
{
    public delegate HookReturnValue<IBaseWeapon> DelegateGiveNamedItemPre(nint ptrClient,
        nint                                                                   ptrController,
        nint                                                                   ptrPawn,
        nint                                                                   ptrClassname,
        ref string?                                                            classnameOverride,
        ref bool                                                               ignoredCEconItemView,
        ref nint                                                               econItemView);

    public delegate void DelegateGiveNamedItemPost(nint ptrClient,
        nint                                            ptrController,
        nint                                            ptrPawn,
        nint                                            ptrClassname,
        bool                                            ignoredCEconItemView,
        EHookAction                                     hookAction,
        nint                                            returnValue,
        nint                                            econItemView);

    public delegate HookReturnValue<EAcquireResult> DelegateCanAcquirePre(nint ptrClient,
        nint                                                                   ptrController,
        nint                                                                   ptrPawn,
        ushort                                                                 itemIndex,
        EAcquireMethod                                                         acquireMethod,
        EAcquireResult                                                         defaultResult);

    public delegate void DelegateGiveGloveItemPost(nint ptrClient, nint ptrController, nint ptrPawn);

    public static event DelegateGiveNamedItemPre? OnGiveNamedItemPre;

    public static event DelegateGiveNamedItemPost? OnGiveNamedItemPost;

    public static event DelegateCanAcquirePre? OnCanAcquirePre;

    public static event DelegateGiveGloveItemPost? OnGiveGloveItemPost;

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnGiveNamedItemPreExport(nint ptrClient,
        nint                                                       ptrController,
        nint                                                       ptrPawn,
        nint                                                       ptrClassname,
        CUtlString*                                                ptrClassnameOverride,
        bool*                                                      classnameOverridden,
        bool*                                                      ignoredCEconItemView,
        nint*                                                      econItemView)
    {
        if (OnGiveNamedItemPre is null)
        {
            return EHookAction.Ignored;
        }

        string? classnameOverride = null;
        ref var ignore            = ref Unsafe.AsRef<bool>(ignoredCEconItemView);
        ref var itemView          = ref Unsafe.AsRef<nint>(econItemView);

        var action = OnGiveNamedItemPre.Invoke(ptrClient,
                                               ptrController,
                                               ptrPawn,
                                               ptrClassname,
                                               ref classnameOverride,
                                               ref ignore,
                                               ref itemView);

        if (action.Action != EHookAction.ChangeParamReturnDefault)
        {
            return EHookAction.Ignored;
        }

        if (classnameOverride is not null)
        {
            ptrClassnameOverride->SetString(classnameOverride);
            *classnameOverridden = true;
        }

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static void OnGiveNamedItemPostExport(nint ptrClient,
        nint                                          ptrController,
        nint                                          ptrPawn,
        nint                                          ptrClassname,
        byte                                          ignoredCEconItemView,
        EHookAction                                   hookAction,
        nint                                          returnValue,
        nint                                          econItemView)
        => OnGiveNamedItemPost?.Invoke(ptrClient,
                                       ptrController,
                                       ptrPawn,
                                       ptrClassname,
                                       ignoredCEconItemView != 0,
                                       hookAction,
                                       returnValue,
                                       econItemView);

    [UnmanagedCallersOnly]
    public static void OnGiveGloveItemPostExport(nint ptrClient, nint ptrController, nint ptrPawn)
        => OnGiveGloveItemPost?.Invoke(ptrClient, ptrController, ptrPawn);

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnCanAcquirePreExport(nint ptrClient,
        nint                                                    ptrController,
        nint                                                    ptrPawn,
        ushort                                                  itemIndex,
        EAcquireMethod                                          method,
        EAcquireResult*                                         acquireResult)
    {
        if (OnCanAcquirePre is null)
        {
            return EHookAction.Ignored;
        }

        var defaultCanAcquire = *acquireResult;

        var action = OnCanAcquirePre.Invoke(ptrClient, ptrController, ptrPawn, itemIndex, method, defaultCanAcquire);

        *acquireResult = action.ReturnValue;

        return action.Action;
    }
}
