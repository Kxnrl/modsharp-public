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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Forwards;

internal static class Player
{
    public delegate void DelegatePlayerSpawn(nint ptrClient, nint ptrController, nint ptrPawn);

    public delegate void DelegatePlayerThink(nint ptrClient, nint ptrController, nint ptrPawn);

    public delegate void DelegatePlayerKilled(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrResult);

    public delegate EHookAction DelegatePlayerRunCommandPre(nint ptrClient,
        nint                                                     ptrController,
        nint                                                     ptrPawn,
        nint                                                     ptrService,
        nint                                                     ptrUserCmd);

    public delegate void DelegatePlayerRunCommandPost(nint ptrClient,
        nint                                               ptrController,
        nint                                               ptrPawn,
        nint                                               ptrService,
        nint                                               ptrUserCmd,
        EHookAction                                        action);

    public delegate void DelegatePlayerProcessMove(nint ptrClient,
        nint                                            ptrController,
        nint                                            ptrPawn,
        nint                                            ptrService,
        nint                                            ptrMoveData);

    public delegate void DelegatePlayerWalkMove(nint ptrClient,
        nint                                         ptrController,
        nint                                         ptrPawn,
        nint                                         ptrService,
        nint                                         ptrMoveData,
        ref int                                      speed);

    public delegate void DelegateOnPlayerSwitchWeapon(nint ptrClient,
        nint                                               ptrController,
        nint                                               ptrPawn,
        nint                                               ptrService,
        nint                                               ptrWeapon);

    public delegate void DelegateOnPlayerEquipWeapon(nint ptrClient,
        nint                                              ptrController,
        nint                                              ptrPawn,
        nint                                              ptrService,
        nint                                              ptrWeapon,
        long                                              equipmentValue);

    public delegate void DelegateOnPlayerDropWeapon(nint ptrClient,
        nint                                             ptrController,
        nint                                             ptrPawn,
        nint                                             ptrService,
        nint                                             ptrWeapon);

    public delegate HookReturnValue<bool> DelegateOnPlayerWeaponCanUse(nint ptrClient,
        nint                                                                ptrController,
        nint                                                                ptrPawn,
        nint                                                                ptrWeapon);

    public delegate HookReturnValue<bool> DelegateOnPlayerWeaponCanSwitch(nint ptrClient,
        nint                                                                   ptrController,
        nint                                                                   ptrPawn,
        nint                                                                   ptrWeapon);

    public delegate HookReturnValue<bool> DelegateOnPlayerWeaponCanEquip(nint ptrClient,
        nint                                                                  ptrController,
        nint                                                                  ptrPawn,
        nint                                                                  ptrWeapon);

    public delegate HookReturnValue<float> DelegateOnPlayerGetMaxSpeed(nint ptrClient,
        nint                                                                ptrController,
        nint                                                                ptrPawn,
        float                                                               originalSpeed);

    public static event DelegatePlayerSpawn?             OnPlayerSpawnPre;
    public static event DelegatePlayerSpawn?             OnPlayerSpawnPost;
    public static event DelegatePlayerThink?             OnPlayerPreThink;
    public static event DelegatePlayerThink?             OnPlayerPostThink;
    public static event DelegatePlayerKilled?            OnPlayerKilledPre;
    public static event DelegatePlayerKilled?            OnPlayerKilledPost;
    public static event DelegatePlayerRunCommandPre?     OnPlayerRunCommandPre;
    public static event DelegatePlayerRunCommandPost?    OnPlayerRunCommandPost;
    public static event DelegatePlayerProcessMove?       OnPlayerProcessMovePre;
    public static event DelegatePlayerProcessMove?       OnPlayerProcessMovePost;
    public static event DelegatePlayerWalkMove?          OnPlayerWalkMove;
    public static event DelegateOnPlayerSwitchWeapon?    OnPlayerSwitchWeapon;
    public static event DelegateOnPlayerEquipWeapon?     OnPlayerEquipWeapon;
    public static event DelegateOnPlayerDropWeapon?      OnPlayerDropWeapon;
    public static event DelegateOnPlayerWeaponCanUse?    OnPlayerWeaponCanUse;
    public static event DelegateOnPlayerWeaponCanSwitch? OnPlayerWeaponCanSwitch;
    public static event DelegateOnPlayerWeaponCanEquip?  OnPlayerWeaponCanEquip;
    public static event DelegateOnPlayerGetMaxSpeed?     OnPlayerGetMaxSpeed;

    [UnmanagedCallersOnly]
    public static void OnPlayerSpawnPreExport(nint ptrClient, nint ptrController, nint ptrPawn)
        => OnPlayerSpawnPre?.Invoke(ptrClient, ptrController, ptrPawn);

    [UnmanagedCallersOnly]
    public static void OnPlayerSpawnPostExport(nint ptrClient, nint ptrController, nint ptrPawn)
        => OnPlayerSpawnPost?.Invoke(ptrClient, ptrController, ptrPawn);

    [UnmanagedCallersOnly]
    public static void OnPlayerPreThinkExport(nint ptrClient, nint ptrController, nint ptrPawn)
        => OnPlayerPreThink?.Invoke(ptrClient, ptrController, ptrPawn);

    [UnmanagedCallersOnly]
    public static void OnPlayerPostThinkExport(nint ptrClient, nint ptrController, nint ptrPawn)
        => OnPlayerPostThink?.Invoke(ptrClient, ptrController, ptrPawn);

    [UnmanagedCallersOnly]
    public static void OnPlayerKilledPreExport(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrResult)
        => OnPlayerKilledPre?.Invoke(ptrClient, ptrController, ptrPawn, ptrResult);

    [UnmanagedCallersOnly]
    public static void OnPlayerKilledPostExport(nint ptrClient, nint ptrController, nint ptrPawn, nint ptrResult)
        => OnPlayerKilledPost?.Invoke(ptrClient, ptrController, ptrPawn, ptrResult);

    [UnmanagedCallersOnly]
    public static EHookAction OnPlayerRunCommandPreExport(nint ptrClient,
        nint                                                   ptrController,
        nint                                                   ptrPawn,
        nint                                                   ptrService,
        nint                                                   ptrUserCmd)
        => OnPlayerRunCommandPre?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrUserCmd) ?? EHookAction.Ignored;

    [UnmanagedCallersOnly]
    public static void OnPlayerRunCommandPostExport(nint ptrClient,
        nint                                             ptrController,
        nint                                             ptrPawn,
        nint                                             ptrService,
        nint                                             ptrUserCmd,
        EHookAction                                      action)
        => OnPlayerRunCommandPost?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrUserCmd, action);

    [UnmanagedCallersOnly]
    public static void OnPlayerProcessMovePreExport(nint ptrClient,
        nint                                             ptrController,
        nint                                             ptrPawn,
        nint                                             ptrService,
        nint                                             ptrMoveData)
        => OnPlayerProcessMovePre?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrMoveData);

    [UnmanagedCallersOnly]
    public static void OnPlayerProcessMovePostExport(nint ptrClient,
        nint                                              ptrController,
        nint                                              ptrPawn,
        nint                                              ptrService,
        nint                                              ptrMoveData)
        => OnPlayerProcessMovePost?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrMoveData);

    [UnmanagedCallersOnly]
    public static unsafe void OnPlayerWalkMoveExport(nint ptrClient,
        nint                                              ptrController,
        nint                                              ptrPawn,
        nint                                              ptrService,
        nint                                              ptrMoveData,
        int*                                              pSpeed)
    {
        if (OnPlayerWalkMove is null)
        {
            return;
        }

        ref var speed = ref Unsafe.AsRef<int>(pSpeed);
        OnPlayerWalkMove.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrMoveData, ref speed);
    }

    [UnmanagedCallersOnly]
    public static void OnPlayerSwitchWeaponExport(nint ptrClient,
        nint                                           ptrController,
        nint                                           ptrPawn,
        nint                                           ptrService,
        nint                                           ptrWeapon)
        => OnPlayerSwitchWeapon?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrWeapon);

    [UnmanagedCallersOnly]
    public static void OnPlayerEquipWeaponExport(nint ptrClient,
        nint                                          ptrController,
        nint                                          ptrPawn,
        nint                                          ptrService,
        nint                                          ptrWeapon,
        long                                          equipmentValue)
        => OnPlayerEquipWeapon?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrWeapon, equipmentValue);

    [UnmanagedCallersOnly]
    public static void OnPlayerDropWeaponExport(nint ptrClient,
        nint                                         ptrController,
        nint                                         ptrPawn,
        nint                                         ptrService,
        nint                                         ptrWeapon)
        => OnPlayerDropWeapon?.Invoke(ptrClient, ptrController, ptrPawn, ptrService, ptrWeapon);

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnPlayerWeaponCanUseExport(nint ptrClient,
        nint                                                         ptrController,
        nint                                                         ptrPawn,
        nint                                                         ptrWeapon,
        bool*                                                        ptrReturn)
    {
        if (OnPlayerWeaponCanUse is null)
        {
            return EHookAction.Ignored;
        }

        var action = OnPlayerWeaponCanUse.Invoke(ptrClient, ptrController, ptrPawn, ptrWeapon);

        *ptrReturn = action.ReturnValue;

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnPlayerWeaponCanSwitchExport(nint ptrClient,
        nint                                                            ptrController,
        nint                                                            ptrPawn,
        nint                                                            ptrWeapon,
        bool*                                                           ptrReturn)
    {
        if (OnPlayerWeaponCanSwitch is null)
        {
            return EHookAction.Ignored;
        }

        var action = OnPlayerWeaponCanSwitch.Invoke(ptrClient, ptrController, ptrPawn, ptrWeapon);

        *ptrReturn = action.ReturnValue;

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnPlayerWeaponCanEquipExport(nint ptrClient,
        nint                                                           ptrController,
        nint                                                           ptrPawn,
        nint                                                           ptrWeapon,
        bool*                                                          ptrReturn)
    {
        if (OnPlayerWeaponCanEquip is null)
        {
            return EHookAction.Ignored;
        }

        var action = OnPlayerWeaponCanEquip.Invoke(ptrClient, ptrController, ptrPawn, ptrWeapon);

        *ptrReturn = action.ReturnValue;

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static unsafe EHookAction OnPlayerGetMaxSpeedExport(nint ptrClient,
        nint                                                        ptrController,
        nint                                                        ptrPawn,
        float                                                       originalSpeed,
        float*                                                      ptrReturn)
    {
        if (OnPlayerGetMaxSpeed is null)
        {
            return EHookAction.Ignored;
        }

        var action = OnPlayerGetMaxSpeed.Invoke(ptrClient, ptrController, ptrPawn, originalSpeed);

        *ptrReturn = action.ReturnValue;

        return action.Action;
    }
}
