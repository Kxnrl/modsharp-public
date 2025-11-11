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
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;

namespace Sharp.Modules.EntityEnhancements.Modules;

internal sealed class GameUI : IEnhancement, IGameListener, IEntityListener
{
    private const string Class = "logic_case";

    private readonly record struct GameUIEntity(IPlayerPawn Owner, UserCommandButtons Buttons);

    private const uint SpawnFlagsFreezePlayer   = 32;
    private const uint SpawnFlagsHideWeapon     = 64;
    private const uint SpawnFlagsUseDeactivate  = 128;
    private const uint SpawnFlagsJumpDeactivate = 256;

    private readonly ILogger<GameUI> _logger;
    private readonly IModSharp       _modSharp;
    private readonly IEntityManager  _entityManager;

    private readonly Dictionary<IBaseEntity, GameUIEntity> _lastEntity;

    public GameUI(ISharedSystem sharedSystem)
    {
        _logger        = sharedSystem.GetLoggerFactory().CreateLogger<GameUI>();
        _modSharp      = sharedSystem.GetModSharp();
        _entityManager = sharedSystem.GetEntityManager();

        _lastEntity = [];
    }

    public void Init()
    {
        _entityManager.InstallEntityListener(this);
        _modSharp.InstallGameListener(this);

        _modSharp.PushTimer(OnRunThink, 0.05, GameTimerFlags.Repeatable);

        _entityManager.HookEntityInput(Class, "Activate");
        _entityManager.HookEntityInput(Class, "Deactivate");
    }

    public void Shutdown()
    {
        _entityManager.RemoveEntityListener(this);
        _modSharp.RemoveGameListener(this);
    }

    int IGameListener.ListenerPriority => 0;
    int IGameListener.ListenerVersion  => IGameListener.ApiVersion;

    public void OnRoundRestart()
    {
        _lastEntity.Clear();
    }

    int IEntityListener.ListenerPriority => 0;
    int IEntityListener.ListenerVersion  => IEntityListener.ApiVersion;

    public EHookAction OnEntityAcceptInput(IBaseEntity entity,
        string                                         input,
        in EntityVariant                               value,
        IBaseEntity?                                   activator,
        IBaseEntity?                                   caller)
    {
        if (!entity.Classname.Equals(Class, StringComparison.OrdinalIgnoreCase))
        {
            return EHookAction.Ignored;
        }

        if (input.Equals("Activate", StringComparison.OrdinalIgnoreCase))
        {
            return InputActivate(entity, activator);
        }

        if (input.Equals("Deactivate", StringComparison.OrdinalIgnoreCase))
        {
            return InputDeactivate(entity, activator);
        }

        return EHookAction.Ignored;
    }

    private EHookAction InputActivate(IBaseEntity entity, IBaseEntity? activator)
    {
        if (activator is null
            || !entity.IsGameUI()
            || activator.AsPlayerPawn() is not { } pawn
            || pawn.GetMovementService() is not { } movement)
        {
            return EHookAction.SkipCallReturnOverride;
        }

        if ((entity.SpawnFlags & SpawnFlagsFreezePlayer) != 0)
        {
            pawn.Flags |= EntityFlags.AtControls;
        }

        var buttons = movement.KeyButtons;
        buttons &= ~UserCommandButtons.Use;

        _lastEntity[entity] = new GameUIEntity(pawn, buttons);

        DelayInput(entity, pawn, "InValue", "PlayerOn");

        // _logger.LogInformation("Pawn {index} activate game_ui {i2}.{name}", pawn.Index, entity.Index, entity.Name);

        return EHookAction.SkipCallReturnOverride;
    }

    private EHookAction InputDeactivate(IBaseEntity entity, IBaseEntity? activator)
    {
        if (!entity.IsGameUI() || !_lastEntity.Remove(entity, out var info))
        {
            return EHookAction.SkipCallReturnOverride;
        }

        if (info.Owner.IsValid())
        {
            if ((entity.SpawnFlags & SpawnFlagsFreezePlayer) != 0)
            {
                info.Owner.Flags &= ~EntityFlags.AtControls;
            }

            DelayInput(entity, info.Owner, "InValue", "PlayerOff");
        }

        /*
        _logger.LogInformation("activator {index} deactivate game_ui {i2}.{name}",
                               info.Owner.Index,
                               entity.Index,
                               entity.Name);
        */

        return EHookAction.SkipCallReturnOverride;
    }

    private void OnRunThink()
    {
        if (_lastEntity.Count == 0)
        {
            return;
        }

        var removeList = new List<IBaseEntity>();

        foreach (var (entity, info) in _lastEntity)
        {
            if (!entity.IsValid())
            {
                removeList.Add(entity);

                continue;
            }

            if (!info.Owner.IsValid())
            {
                DelayInput(entity, "Deactivate");

                continue;
            }

            if (!info.Owner.IsAlive)
            {
                DelayInput(entity, info.Owner, "Deactivate");

                continue;
            }

            HandleGameUI(entity, info.Owner, info.Buttons);
        }

        foreach (var entity in removeList)
        {
            _lastEntity.Remove(entity);
        }
    }

    private void HandleGameUI(IBaseEntity entity, IPlayerPawn pawn, UserCommandButtons lastButtons)
    {
        if (pawn.GetMovementService() is not { } movement)
        {
            return;
        }

        var buttons = movement.KeyButtons;

        if ((entity.SpawnFlags & SpawnFlagsJumpDeactivate) != 0
            && (buttons.HasFlag(UserCommandButtons.Jump) || movement.ScrollButtons.HasFlag(UserCommandButtons.Jump)))
        {
            DelayInput(entity, pawn, "Deactivate");

            return;
        }

        var nButtonsChanged = buttons ^ lastButtons;

        // W
        if (nButtonsChanged.HasFlag(UserCommandButtons.Forward))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Forward) ? "UnpressedForward" : "PressedForward");
        }

        // A
        if (nButtonsChanged.HasFlag(UserCommandButtons.MoveLeft))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.MoveLeft) ? "UnpressedMoveLeft" : "PressedMoveLeft");
        }

        // S
        if (nButtonsChanged.HasFlag(UserCommandButtons.Back))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Back) ? "UnpressedBack" : "PressedBack");
        }

        // D
        if (nButtonsChanged.HasFlag(UserCommandButtons.MoveRight))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.MoveRight) ? "UnpressedMoveRight" : "PressedMoveRight");
        }

        // Attack
        if (nButtonsChanged.HasFlag(UserCommandButtons.Attack))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Attack) ? "UnpressedAttack" : "PressedAttack");
        }

        // Attack2
        if (nButtonsChanged.HasFlag(UserCommandButtons.Attack2))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Attack2) ? "UnpressedAttack2" : "PressedAttack2");
        }

        // Speed
        if (nButtonsChanged.HasFlag(UserCommandButtons.Speed))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Speed) ? "UnpressedSpeed" : "PressedSpeed");
        }

        // Duck
        if (nButtonsChanged.HasFlag(UserCommandButtons.Duck))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Duck) ? "UnpressedDuck" : "PressedDuck");
        }

        // Inspect
        if (nButtonsChanged.HasFlag(UserCommandButtons.LookAtWeapon))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.LookAtWeapon) ? "UnpressedInspect" : "PressedInspect");
        }

        // Score
        if (nButtonsChanged.HasFlag(UserCommandButtons.Scoreboard))
        {
            entity.AcceptInput("InValue",
                               pawn,
                               entity,
                               lastButtons.HasFlag(UserCommandButtons.Scoreboard) ? "UnpressedScore" : "PressedScore");
        }

        _lastEntity[entity] = new GameUIEntity(pawn, buttons);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DelayInput(IBaseEntity entity, string input, string? param = null)
        => _modSharp.InvokeFrameAction(() =>
        {
            if (entity.IsValid())
            {
                entity.AcceptInput(input, null, entity, param);
            }
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DelayInput(IBaseEntity entity, IBaseEntity activator, string input, string? param = null)
        => _modSharp.InvokeFrameAction(() =>
        {
            if (entity.IsValid())
            {
                entity.AcceptInput(input, activator.IsValid() ? activator : null, entity, param);
            }
        });
}

file static class GameUIExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsGameUI(this IBaseEntity entity)
        => "game_ui".Equals(entity.PrivateVScripts, StringComparison.OrdinalIgnoreCase);
}
