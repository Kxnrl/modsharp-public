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
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Runtime;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Bridges.Forwards;

internal static unsafe class Game
{
    public delegate void DelegateOnStartupServerPre();

    public delegate void DelegateOnGameFramePre(bool simulating, bool firstTick, bool lastTick);

    public delegate void DelegateOnGameFramePost(bool simulating, bool firstTick, bool lastTick);

    public delegate void DelegateOnServerInit();

    public delegate void DelegateOnServerSpawn();

    public delegate void DelegateOnServerActivate();

    public delegate void DelegateGameSystemEvent();

    public delegate void DelegateOnResourcePrecache(nint pContext);

    public delegate void DelegateOnFrameUpdateEntityThink();

    public delegate void DelegateOnServerGamePostSimulate();

    public delegate EHookAction DelegateOnPrintStatus(nint pClient);

    public delegate void DelegateOnRoundRestart();

    public delegate void DelegateOnRoundRestarted();

    public delegate ECommandAction DelegateOnConsoleSay(string message);

    public delegate EHookAction DelegateOnEmitSound(int entity,
        ref string                                      sound,
        ref float                                       volume,
        ref ulong                                       receivers,
        ref bool                                        changeReceiver,
        ref bool                                        overrideSound);

    public delegate void DelegateOnEmitSoundPost(int entity,
        string                                       sound,
        float                                        volume,
        ulong                                        receivers,
        bool                                         changeReceiver,
        EHookAction                                  action,
        SoundOpEventGuid                             guid);

    public delegate void DelegateOnEmitMusicPost(int entity,
        string                                       sound,
        float                                        duration,
        ulong                                        receivers,
        SoundOpEventGuid                             guid);

    public delegate EHookAction DelegateOnSoundEvent(int entity,
        string                                           sound,
        float                                            duration,
        ref ulong                                        receivers,
        ref bool                                         changeReceiver);

    public delegate void DelegateOnSoundEventPost(int entity,
        string                                        sound,
        float                                         duration,
        ulong                                         receivers,
        bool                                          changeReceiver,
        EHookAction                                   action,
        SoundOpEventGuid                              guid);

    public delegate void DelegateOnMapVoteCreated(nint ptr);

    public delegate bool DelegateOnRuntimeTraceFilterCallback(nint ptr);

    public delegate HookReturnValue<EmptyHookReturn> DelegateOnTerminateRoundPre(nint gameRules,
        ref float                                                                     delay,
        ref RoundEndReason                                                            reason,
        NativeFixedSpan<TeamRewardInfo>                                               rewardInfo);

    public delegate void DelegateOnTerminateRoundPost(nint gameRules,
        float                                              delay,
        RoundEndReason                                     reason,
        NativeFixedSpan<TeamRewardInfo>                    rewardInfo,
        EHookAction                                        action);

    public static event DelegateOnStartupServerPre? OnStartupServerPre;

    public static event DelegateOnGameFramePre?               OnGameFramePre;
    public static event DelegateOnGameFramePost?              OnGameFramePost;
    public static event DelegateOnServerInit?                 OnServerInit;
    public static event DelegateOnServerSpawn?                OnServerSpawn;
    public static event DelegateOnServerActivate?             OnServerActivate;
    public static event DelegateGameSystemEvent?              OnGameInit;
    public static event DelegateGameSystemEvent?              OnGameShutdown;
    public static event DelegateGameSystemEvent?              OnGamePostInit;
    public static event DelegateGameSystemEvent?              OnGamePreShutdown;
    public static event DelegateGameSystemEvent?              OnGameActivate;
    public static event DelegateGameSystemEvent?              OnGameDeactivate;
    public static event DelegateOnResourcePrecache?           OnResourcePrecache;
    public static event DelegateOnFrameUpdateEntityThink?     OnFrameUpdatePreEntityThink;
    public static event DelegateOnFrameUpdateEntityThink?     OnFrameUpdatePostEntityThink;
    public static event DelegateOnServerGamePostSimulate?     OnServerGamePostSimulate;
    public static event DelegateOnPrintStatus?                OnPrintStatus;
    public static event DelegateOnRoundRestart?               OnRoundRestart;
    public static event DelegateOnRoundRestarted?             OnRoundRestarted;
    public static event DelegateOnConsoleSay?                 OnConsoleSay;
    public static event DelegateOnEmitSound?                  OnEmitSound;
    public static event DelegateOnEmitSoundPost?              OnEmitSoundPost;
    public static event DelegateOnEmitMusicPost?              OnEmitMusicPost;
    public static event DelegateOnSoundEvent?                 OnSoundEvent;
    public static event DelegateOnSoundEventPost?             OnSoundEventPost;
    public static event DelegateOnMapVoteCreated?             OnMapVoteCreated;
    public static event DelegateOnRuntimeTraceFilterCallback? OnRuntimeTraceFilterCallback;
    public static event DelegateOnTerminateRoundPre?          OnTerminateRoundPre;
    public static event DelegateOnTerminateRoundPost?         OnTerminateRoundPost;

    [UnmanagedCallersOnly]
    public static void OnStartupServerPreExport()
        => OnStartupServerPre?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGameFramePreExport(bool simulating, bool firstTick, bool lastTick)
        => OnGameFramePre?.Invoke(simulating, firstTick, lastTick);

    [UnmanagedCallersOnly]
    public static void OnGameFramePostExport(bool simulating, bool firstTick, bool lastTick)
        => OnGameFramePost?.Invoke(simulating, firstTick, lastTick);

    [UnmanagedCallersOnly]
    public static void OnServerInitExport()
        => OnServerInit?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnServerSpawnExport()
        => OnServerSpawn?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnServerActivateExport()
        => OnServerActivate?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGameInitExport()
        => OnGameInit?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGameShutdownExport()
        => OnGameShutdown?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGamePostInitExport()
        => OnGamePostInit?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGamePreShutdownExport()
        => OnGamePreShutdown?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGameActivateExport()
        => OnGameActivate?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnGameDeactivateExport()
        => OnGameDeactivate?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnResourcePrecacheExport(nint pContext)
        => OnResourcePrecache?.Invoke(pContext);

    [UnmanagedCallersOnly]
    public static void OnFrameUpdatePreEntityThinkExport()
        => OnFrameUpdatePreEntityThink?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnFrameUpdatePostEntityThinkExport()
        => OnFrameUpdatePostEntityThink?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnServerGamePostSimulateExport()
        => OnServerGamePostSimulate?.Invoke();

    [UnmanagedCallersOnly]
    public static EHookAction OnPrintStatusExport(nint pClient)
        => OnPrintStatus?.Invoke(pClient) ?? EHookAction.Ignored;

    [UnmanagedCallersOnly]
    public static void OnRoundRestartExport()
        => OnRoundRestart?.Invoke();

    [UnmanagedCallersOnly]
    public static void OnRoundRestartedExport()
        => OnRoundRestarted?.Invoke();

    [UnmanagedCallersOnly]
    public static ECommandAction OnConsoleSayExport(sbyte* pMessage)
    {
        if (OnConsoleSay is null)
        {
            return ECommandAction.Skipped;
        }

        var message = Utils.ReadString(pMessage);

        return string.IsNullOrEmpty(message) ? ECommandAction.Skipped : OnConsoleSay.Invoke(message);
    }

    [UnmanagedCallersOnly]
    public static EHookAction OnEmitSoundExport(int entity,
        sbyte*                                      pSound,
        float*                                      pVolume,
        ulong*                                      pReceivers,
        bool*                                       pChangeReceiver)
    {
        if (OnEmitSound is null)
        {
            return EHookAction.Ignored;
        }

        var sound          = Utils.ReadString(pSound);
        var volume         = *pVolume;
        var receivers      = *pReceivers;
        var changeReceiver = false;
        var overrideSound  = false;

        var ret = OnEmitSound.Invoke(entity,
                                     ref sound,
                                     ref volume,
                                     ref receivers,
                                     ref changeReceiver,
                                     ref overrideSound);

        if (ret is EHookAction.ChangeParamReturnDefault)
        {
            // performance friendly
            if (overrideSound)
            {
                Utils.WriteString(pSound, 128, sound);
            }

            *pVolume         = volume;
            *pChangeReceiver = changeReceiver;
            *pReceivers      = receivers;
        }

        return ret;
    }

    [UnmanagedCallersOnly]
    public static void OnEmitSoundPostExport(int entity,
        sbyte*                                   pSound,
        float                                    volume,
        ulong                                    receivers,
        bool                                     changeReceiver,
        EHookAction                              action,
        SoundOpEventGuid                         guid)
    {
        if (OnEmitSoundPost is null)
        {
            return;
        }

        var sound = Utils.ReadString(pSound);

        OnEmitSoundPost.Invoke(entity, sound, volume, receivers, changeReceiver, action, guid);
    }

    [UnmanagedCallersOnly]
    public static void OnEmitMusicPostExport(int entity,
        sbyte*                                   pSound,
        float                                    duration,
        ulong                                    receivers,
        SoundOpEventGuid                         guid)
    {
        if (OnEmitMusicPost is null)
        {
            return;
        }

        var sound = Utils.ReadString(pSound);

        OnEmitMusicPost.Invoke(entity, sound, duration, receivers, guid);
    }

    [UnmanagedCallersOnly]
    public static EHookAction OnSoundEventExport(int entity,
        sbyte*                                       pSound,
        float                                        duration,
        ulong*                                       pReceivers,
        bool*                                        pChangeReceiver)
    {
        if (OnSoundEvent is null)
        {
            return EHookAction.Ignored;
        }

        var sound          = Utils.ReadString(pSound);
        var receivers      = *pReceivers;
        var changeReceiver = false;

        var ret = OnSoundEvent.Invoke(entity, sound, duration, ref receivers, ref changeReceiver);

        if (ret is EHookAction.ChangeParamReturnDefault)
        {
            *pReceivers      = receivers;
            *pChangeReceiver = changeReceiver;
        }

        return ret;
    }

    [UnmanagedCallersOnly]
    public static void OnSoundEventPostExport(int entity,
        sbyte*                                    pSound,
        float                                     duration,
        ulong                                     receivers,
        bool                                      changeReceiver,
        EHookAction                               action,
        SoundOpEventGuid                          guid)
    {
        if (OnSoundEventPost is null)
        {
            return;
        }

        var sound = Utils.ReadString(pSound);

        OnSoundEventPost.Invoke(entity, sound, duration, receivers, changeReceiver, action, guid);
    }

    [UnmanagedCallersOnly]
    public static void OnMapVoteCreatedExport(nint ptr)
        => OnMapVoteCreated?.Invoke(ptr);

    [UnmanagedCallersOnly]
    public static bool OnRuntimeTraceFilterCallbackExport(nint ptr)
        => OnRuntimeTraceFilterCallback?.Invoke(ptr) ?? true;

    [UnmanagedCallersOnly]
    public static EHookAction OnTerminateRoundPreExport(nint gameRules,
        float*                                               pDelay,
        RoundEndReason*                                      pReason,
        NativeFixedSpan<TeamRewardInfo>*                     pInfo)
    {
        if (OnTerminateRoundPre is null)
        {
            return EHookAction.Ignored;
        }

        ref var delay  = ref Unsafe.AsRef<float>(pDelay);
        ref var reason = ref Unsafe.AsRef<RoundEndReason>(pReason);
        var     info   = *pInfo;

        var action = OnTerminateRoundPre.Invoke(gameRules, ref delay, ref reason, info);

        return action.Action;
    }

    [UnmanagedCallersOnly]
    public static void OnTerminateRoundPostExport(nint gameRules,
        float                                          delay,
        RoundEndReason                                 reason,
        NativeFixedSpan<TeamRewardInfo>*               pInfo,
        EHookAction                                    action)
    {
        if (OnTerminateRoundPost is null)
        {
            return;
        }

        var info = *pInfo;
        OnTerminateRoundPost.Invoke(gameRules, delay, reason, info, action);
    }

    //    Server ⬇
    //
    //    ServerInit
    //    LevelChange
    //    LevelInit
    //    ServerSpawn
    //    ServerActivate
}
