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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Hooks;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Modules.EntityEnhancements.Modules;

internal class PointViewControl : IEnhancement, IGameListener, IEntityListener
{
    private const string Class              = "logic_relay";
    private const uint   InvalidFieldOfView = 0xFFFFFFFF;
    private const uint   ResetFieldOfView   = 0xFFFFFFFE;

    private static readonly CEntityHandle<IBaseEntity> InvalidEHandle = new (0xFFFFFFFFu);

    private class ViewControlEntity
    {
        public HashSet<IPlayerPawn> Players    { get; }
        public string               ViewTarget { get; }
        public string               Name       { get; }

        public ViewControlEntity(string viewTarget, string name)
        {
            Players    = new HashSet<IPlayerPawn>(64);
            ViewTarget = viewTarget;
            Name       = name;
        }
    }

    private readonly ILogger<PointViewControl> _logger;
    private readonly IModSharp                 _modSharp;
    private readonly IEntityManager            _entityManager;

    private readonly Dictionary<IBaseEntity, ViewControlEntity> _viewControlEntities;

    private static PointViewControl _sInstance = null!;

    private readonly IDetourHook _hookEyePosition;
    private readonly IDetourHook _hookEyeAngles;

    public PointViewControl(ISharedSystem sharedSystem)
    {
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<PointViewControl>();
        _modSharp      = sharedSystem.GetModSharp();
        _entityManager = sharedSystem.GetEntityManager();

        _viewControlEntities = [];

        _sInstance = this;

        var hooks = sharedSystem.GetHookManager();

        _hookEyePosition = hooks.CreateDetourHook();
        _hookEyeAngles   = hooks.CreateDetourHook();
    }

    public unsafe void Init()
    {
        _modSharp.InstallGameListener(this);
        _entityManager.InstallEntityListener(this);

        _modSharp.PushTimer(OnRunThink, 0.05, GameTimerFlags.Repeatable);

        _entityManager.HookEntityInput(Class, "EnableCamera");
        _entityManager.HookEntityInput(Class, "DisableCamera");
        _entityManager.HookEntityInput(Class, "EnableCameraAll");
        _entityManager.HookEntityInput(Class, "DisableCameraAll");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _hookEyePosition.Prepare("CBasePlayerPawn::GetEyePosition",
                                     (nint) (delegate* unmanaged<nint, Vector*, Vector*>) (&WindowsGetEyePosition));

            _hookEyeAngles.Prepare("CBasePlayerPawn::GetEyeAngles",
                                   (nint) (delegate* unmanaged<nint, Vector*, Vector*>) (&WindowsGetEyeAngles));
        }
        else
        {
            _hookEyePosition.Prepare("CBasePlayerPawn::GetEyePosition",
                                     (nint) (delegate* unmanaged<nint, Vector>) (&LinuxGetEyePosition));

            _hookEyeAngles.Prepare("CBasePlayerPawn::GetEyeAngles",
                                   (nint) (delegate* unmanaged<nint, Vector>) (&LinuxGetEyeAngles));
        }

        if (!_hookEyePosition.Install())
        {
            _logger.LogError("{n} init failed", "CBasePlayerPawn::GetEyePosition");
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _trWindowsEP = (delegate* unmanaged<nint, Vector*, Vector*>) _hookEyePosition.Trampoline;
            }
            else
            {
                _trLinuxEP = (delegate* unmanaged<nint, Vector>) _hookEyePosition.Trampoline;
            }
        }

        if (!_hookEyeAngles.Install())
        {
            _logger.LogError("{n} init failed", "CBasePlayerPawn::GetEyeAngles");
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _trWindowsEA = (delegate* unmanaged<nint, Vector*, Vector*>) _hookEyeAngles.Trampoline;
            }
            else
            {
                _trLinuxEA = (delegate* unmanaged<nint, Vector>) _hookEyeAngles.Trampoline;
            }
        }
    }

    public void Shutdown()
    {
        _modSharp.RemoveGameListener(this);
        _entityManager.RemoveEntityListener(this);

        _hookEyePosition.Uninstall();
        _hookEyeAngles.Uninstall();

        _hookEyePosition.Dispose();
        _hookEyeAngles.Dispose();
    }

    int IGameListener.ListenerPriority => 0;
    int IGameListener.ListenerVersion  => IGameListener.ApiVersion;

    public void OnRoundRestart()
        => _viewControlEntities.Clear();

    int IEntityListener.ListenerPriority => 0;
    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;

    public void OnEntitySpawned(IBaseEntity entity)
    {
        if (!entity.Classname.Equals(Class, StringComparison.OrdinalIgnoreCase) || !entity.IsPointViewControl())
        {
            return;
        }

        _modSharp.InvokeFrameAction(() =>
        {
            if (entity.IsValid())
            {
                var target = entity.GetTargetEntityName();

                if (string.IsNullOrWhiteSpace(target))
                {
                    _logger.LogWarning("Invalid point_viewcontrol<{index}.{name}> with null target!",
                                       entity.Index,
                                       entity.Name);

                    return;
                }

                _viewControlEntities[entity] = new ViewControlEntity(target, entity.Name);
            }
        });
    }

    public EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                         input,
        in EntityVariant                               value,
        IBaseEntity?                                   activator,
        IBaseEntity?                                   caller)
    {
        if (!_viewControlEntities.TryGetValue(entity, out var viewControlEntity))
        {
            return EHookAction.Ignored;
        }

        if (input.Equals("EnableCamera", StringComparison.OrdinalIgnoreCase))
        {
            return InputEnable(viewControlEntity, entity, activator, caller);
        }

        if (input.Equals("DisableCamera", StringComparison.OrdinalIgnoreCase))
        {
            return InputDisable(viewControlEntity, entity, activator, caller);
        }

        if (input.Equals("EnableCameraAll", StringComparison.OrdinalIgnoreCase))
        {
            return InputEnableAll(viewControlEntity, entity, activator, caller);
        }

        if (input.Equals("DisableCameraAll", StringComparison.OrdinalIgnoreCase))
        {
            return InputDisableAll(viewControlEntity, entity, activator, caller);
        }

        return EHookAction.Ignored;
    }

    private EHookAction InputEnable(ViewControlEntity viewControlEntity,
        IBaseEntity                                   entity,
        IBaseEntity?                                  activator,
        IBaseEntity?                                  caller)
    {
        if (activator?.AsPlayerPawn() is not { IsAlive: true } pawn
            || pawn.GetController() is not { } pc
            || pc.IsFakeClient
            || pc.IsHltv)
        {
            return EHookAction.SkipCallReturnOverride;
        }

        var freezePeriod = _modSharp.GetGameRules().IsFreezePeriod;

        foreach (var (vc, vp) in _viewControlEntities)
        {
            if (vp.Players.Contains(pawn))
            {
                if (entity.Equals(vc))
                {
                    _logger.LogInformation("PointViewControl {eh} was enabled twice in a row! player: {p}",
                                           entity.Name,
                                           pc.PlayerName);

                    continue;
                }

                vp.Players.Remove(pawn);
                UpdatePlayerState(pawn, InvalidEHandle, freezePeriod, ResetFieldOfView);
                _logger.LogInformation("Pawn {index} force quit point_viewcontrol {i2}.{name}", pawn.Index, vc.Index, vp.Name);

                break;
            }
        }

        viewControlEntity.Players.Add(pawn);

        _logger.LogInformation("Player {index} take point_viewcontrol {i2}.{name}", pc.PlayerName, entity.Index, entity.Name);

        return EHookAction.SkipCallReturnOverride;
    }

    private EHookAction InputDisable(ViewControlEntity viewControlEntity,
        IBaseEntity                                    entity,
        IBaseEntity?                                   activator,
        IBaseEntity?                                   caller)
    {
        if (activator?.AsPlayerPawn() is not { IsAlive: true } pawn
            || pawn.GetController() is not { } pc)
        {
            return EHookAction.SkipCallReturnOverride;
        }

        if (viewControlEntity.Players.Remove(pawn))
        {
            var freezePeriod = _modSharp.GetGameRules().IsFreezePeriod;

            UpdatePlayerState(pawn, InvalidEHandle, freezePeriod, ResetFieldOfView);

            _logger.LogInformation("Player {index} quit point_viewcontrol {i2}.{name}",
                                   pc.PlayerName,
                                   entity.Index,
                                   entity.Name);
        }

        return EHookAction.SkipCallReturnOverride;
    }

    private EHookAction InputEnableAll(ViewControlEntity viewControlEntity,
        IBaseEntity                                      entity,
        IBaseEntity?                                     activator,
        IBaseEntity?                                     caller)
    {
        var freezePeriod = _modSharp.GetGameRules().IsFreezePeriod;

        foreach (var pc in GetGameControllers())
        {
            if (pc.IsFakeClient || pc.IsHltv)
            {
                continue;
            }

            if (pc.GetPlayerPawn() is not { IsAlive: true } pawn)
            {
                continue;
            }

            foreach (var (vc, vp) in _viewControlEntities)
            {
                if (vp.Players.Contains(pawn))
                {
                    vp.Players.Remove(pawn);

                    if (entity.Equals(vc))
                    {
                        continue;
                    }

                    UpdatePlayerState(pawn, InvalidEHandle, freezePeriod, ResetFieldOfView);

                    _logger.LogInformation("Pawn {index} force quit point_viewcontrol {i2}.{name}",
                                           pawn.Index,
                                           vc.Index,
                                           vp.Name);
                }
            }

            viewControlEntity.Players.Add(pawn);
        }

        return EHookAction.SkipCallReturnOverride;
    }

    private EHookAction InputDisableAll(ViewControlEntity viewControlEntity,
        IBaseEntity                                       entity,
        IBaseEntity?                                      activator,
        IBaseEntity?                                      caller)
    {
        var freezePeriod = _modSharp.GetGameRules().IsFreezePeriod;

        foreach (var pawn in viewControlEntity.Players)
        {
            if (pawn.IsValid())
            {
                UpdatePlayerState(pawn, InvalidEHandle, freezePeriod, ResetFieldOfView);
            }
        }

        viewControlEntity.Players.Clear();

        return EHookAction.SkipCallReturnOverride;
    }

    private void OnRunThink()
    {
        if (_viewControlEntities.Count == 0)
        {
            return;
        }

        var freezePeriod = _modSharp.GetGameRules().IsFreezePeriod;

        var removeList = new List<IBaseEntity>();

        foreach (var (entity, controller) in _viewControlEntities)
        {
            if (!entity.IsValid())
            {
                removeList.Add(entity);

                foreach (var vp in controller.Players)
                {
                    if (vp.IsValid())
                    {
                        UpdatePlayerState(vp, InvalidEHandle, freezePeriod, ResetFieldOfView);
                    }
                }

                continue;
            }

            if (controller.Players.Count == 0)
            {
                continue;
            }

            var target = _entityManager.FindEntityByName(null, entity.GetTargetEntityName());

            if (target is null)
            {
                foreach (var vp in controller.Players)
                {
                    if (vp.IsValid())
                    {
                        UpdatePlayerState(vp, InvalidEHandle, freezePeriod, ResetFieldOfView);
                    }
                }

                controller.Players.Clear();

                continue;
            }

            var list = new List<IPlayerPawn>();

            foreach (var vp in controller.Players)
            {
                if (!vp.IsValid())
                {
                    list.Add(vp);

                    continue;
                }

                if (!vp.IsAlive)
                {
                    list.Add(vp);
                    UpdatePlayerState(vp, InvalidEHandle, freezePeriod, ResetFieldOfView);

                    continue;
                }

                UpdatePlayerState(vp,
                                  target.Handle,
                                  entity.ShouldFrozen(),
                                  entity.ShouldFieldOfView()
                                      ? entity.GetFieldOfView()
                                      : InvalidFieldOfView,
                                  entity.ShouldDisarm());
            }

            if (list.Count > 0)
            {
                list.ForEach(x => controller.Players.Remove(x));
            }
        }

        foreach (var entity in removeList)
        {
            _viewControlEntities.Remove(entity);
        }
    }

    private void UpdatePlayerState(IPlayerPawn pawn,
        in CEntityHandle<IBaseEntity>          target,
        bool                                   frozen,
        uint                                   fov    = InvalidFieldOfView,
        bool                                   disarm = false)
    {
        if (pawn.GetCameraService() is { } cameraService)
        {
            cameraService.ViewEntityHandle = target;

            if (fov != InvalidFieldOfView)
            {
                if (pawn.GetController() is { } controller)
                {
                    cameraService.FieldOfView = fov == ResetFieldOfView ? controller.DesiredFOV : fov;
                }
            }
        }

        if (disarm)
        {
            if (pawn.GetActiveWeapon() is { } weapon)
            {
                weapon.NextPrimaryAttackTick   = 999999999;
                weapon.NextSecondaryAttackTick = 999999999;
            }
        }

        var flags = pawn.Flags;

        if (!frozen && _modSharp.GetGameRules().IsFreezePeriod)
        {
            frozen = true;
        }

        if (frozen)
        {
            flags |= EntityFlags.Frozen;
        }
        else
        {
            flags &= ~EntityFlags.Frozen;
        }

        pawn.Flags = flags;
    }

    private IEnumerable<IPlayerController> GetGameControllers()
    {
        var max = _modSharp.GetGlobals().MaxClients;

        for (EntityIndex i = 1; i <= max; i++)
        {
            if (_entityManager.FindEntityByIndex(i) is not { } entity
                || entity.AsPlayerController() is not { ConnectedState: PlayerConnectedState.PlayerConnected } controller)
            {
                continue;
            }

            yield return controller;
        }
    }

    private bool IsViewControl(IPlayerPawn pawn)
    {
        if (_viewControlEntities.Count == 0)
        {
            return false;
        }

        foreach (var (_, e) in _viewControlEntities)
        {
            if (e.Players.Contains(pawn))
            {
                return true;
            }
        }

        return false;
    }

    private static unsafe delegate* unmanaged<nint, Vector*, Vector*>  _trWindowsEP;
    private static unsafe delegate * unmanaged<nint, Vector*, Vector*> _trWindowsEA;
    private static unsafe delegate* unmanaged<nint, Vector>            _trLinuxEP;
    private static unsafe delegate* unmanaged<nint, Vector>            _trLinuxEA;

    [UnmanagedCallersOnly]
    private static unsafe Vector* WindowsGetEyePosition(nint pEntity, Vector* pRet)
    {
        if (_sInstance._entityManager.MakeEntityFromPointer<IPlayerPawn>(pEntity) is { IsAlive: true } pawn
            && _sInstance.IsViewControl(pawn))
        {
            var position = pawn.GetAbsOrigin() + pawn.ViewOffset;
            pRet->X = position.X;
            pRet->Y = position.Y;
            pRet->Z = position.Z;

            return pRet;
        }

        return _trWindowsEP(pEntity, pRet);
    }

    [UnmanagedCallersOnly]
    private static unsafe Vector* WindowsGetEyeAngles(nint pEntity, Vector* pRet)
    {
        if (_sInstance._entityManager.MakeEntityFromPointer<IPlayerPawn>(pEntity) is { IsAlive: true } pawn
            && _sInstance.IsViewControl(pawn))
        {
            var angles = pawn.GetNetVar<Vector>("v_angle");
            pRet->X = angles.X;
            pRet->Y = angles.Y;
            pRet->Z = angles.Z;

            return pRet;
        }

        return _trWindowsEA(pEntity, pRet);
    }

    [UnmanagedCallersOnly]
    private static unsafe Vector LinuxGetEyePosition(nint pEntity)
    {
        if (_sInstance._entityManager.MakeEntityFromPointer<IPlayerPawn>(pEntity) is { IsAlive: true } pawn
            && _sInstance.IsViewControl(pawn))
        {
            return pawn.GetAbsOrigin() + pawn.ViewOffset;
        }

        return _trLinuxEP(pEntity);
    }

    [UnmanagedCallersOnly]
    private static unsafe Vector LinuxGetEyeAngles(nint pEntity)
    {
        if (_sInstance._entityManager.MakeEntityFromPointer<IPlayerPawn>(pEntity) is { IsAlive: true } pawn
            && _sInstance.IsViewControl(pawn))
        {
            return pawn.GetNetVar<Vector>("v_angle");
        }

        return _trLinuxEA(pEntity);
    }
}

file static class PointViewControlExtensions
{
    private const uint SpawnFlagsFrozen      = 1 << 5;
    private const uint SpawnFlagsFieldOfView = 1 << 6;
    private const uint SpawnFlagsDisarm      = 1 << 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPointViewControl(this IBaseEntity entity)
        => "Point_ViewControl".Equals(entity.PrivateVScripts, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ShouldFrozen(this IBaseEntity entity)
        => (entity.SpawnFlags & SpawnFlagsFrozen) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ShouldFieldOfView(this IBaseEntity entity)
        => (entity.SpawnFlags & SpawnFlagsFieldOfView) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ShouldDisarm(this IBaseEntity entity)
        => (entity.SpawnFlags & SpawnFlagsDisarm) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetFieldOfView(this IBaseEntity entity)
        => uint.Clamp((uint) entity.Health, 16, 179);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetTargetEntityName(this IBaseEntity entity)
        => entity.GetNetVarUtlSymbolLarge("m_target");
}
