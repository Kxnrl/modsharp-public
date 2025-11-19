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
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Types.CppProtobuf;

namespace Sharp.Core.Managers.Hooks;

internal class PlayerRunCommandHook : HookType<PlayerRunCommandHookParams, EmptyHookReturn, PlayerRunCommandHook>
{
    public PlayerRunCommandHook(ILoggerFactory factory) : base(factory)
    {
        Bridges.Forwards.Player.OnPlayerRunCommandPre  += CCSPlayerMovementService_RunCommandPre;
        Bridges.Forwards.Player.OnPlayerRunCommandPost += CCSPlayerMovementService_RunCommandPost;
    }

    private EHookAction CCSPlayerMovementService_RunCommandPre(nint ptrClient,
        nint                                                        ptrController,
        nint                                                        ptrPawn,
        nint                                                        ptrService,
        nint                                                        ptrUserCmd)
    {
        if (!IsHookInvokeRequired(false))
        {
            return EHookAction.Ignored;
        }

        var param  = new PlayerRunCommandHookParams(false, ptrClient, ptrController, ptrPawn, ptrService, ptrUserCmd);
        var result = InvokeHookPre(param);
        param.MarkAsDisposed();

        return result.Action;
    }

    private void CCSPlayerMovementService_RunCommandPost(nint ptrClient,
        nint                                                  ptrController,
        nint                                                  ptrPawn,
        nint                                                  ptrService,
        nint                                                  ptrUserCmd,
        EHookAction                                           action)
    {
        if (!IsHookInvokeRequired(true))
        {
            return;
        }

        var param = new PlayerRunCommandHookParams(true, ptrClient, ptrController, ptrPawn, ptrService, ptrUserCmd);
        InvokeHookPost(param, new HookReturnValue<EmptyHookReturn>(action));
        param.MarkAsDisposed();
    }

    protected override bool AllowPre  => true;
    protected override bool AllowPost => true;
}

internal sealed unsafe class PlayerRunCommandHookParams : PlayerFunctionParams, IPlayerRunCommandHookParams
{
    private readonly CUserCmd* _userCmd;
    private readonly nint      _ptrPawn;
    private readonly nint      _ptrService;

    public PlayerRunCommandHookParams(bool postHook,
        nint                               client,
        nint                               controller,
        nint                               pawn,
        nint                               ptrService,
        nint                               userCmd) : base(postHook,
                                                           client,
                                                           controller)
    {
        _userCmd    = (CUserCmd*) userCmd;
        _ptrPawn    = pawn;
        _ptrService = ptrService;
    }

    private IBasePlayerPawn? _pawn;

    public IBasePlayerPawn Pawn
    {
        get
        {
            if (_pawn is { } pawn)
            {
                return pawn;
            }

            CheckDisposed();
            _pawn = CreateBasePlayerPawn(_ptrPawn);

            return _pawn;
        }
    }

    private IMovementService? _service;

    public IMovementService Service
    {
        get
        {
            if (_service is { } service)
            {
                return service;
            }

            CheckDisposed();

            _service = Bridges.Natives.Player.PawnIsPlayer(_ptrPawn)
                ? CreatePlayerMovementService(_ptrService)
                : CreateBaseMovementService(_ptrService);

            return _service;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CUserCmd* GetUserCmd()
    {
        CheckDisposed();

        return _userCmd;
    }

    public CCSGOUserCmdPb* CSGOUserCmd => &GetUserCmd()->CSGOUserCmd;

    public CBaseUserCmdPb* BaseUserCmd => CSGOUserCmd->BaseUserCmd;

    public UserCommandButtons KeyButtons
    {
        get => GetUserCmd()->ButtonState.ButtonPressed;
        set => GetUserCmd()->ButtonState.ButtonPressed = value;
    }

    public UserCommandButtons ChangedButtons
    {
        get => GetUserCmd()->ButtonState.ButtonChanged;
        set => GetUserCmd()->ButtonState.ButtonChanged = value;
    }

    public UserCommandButtons ScrollButtons
    {
        get => GetUserCmd()->ButtonState.ButtonScroll;
        set => GetUserCmd()->ButtonState.ButtonScroll = value;
    }

    public int InputHistorySize => CSGOUserCmd->InputHistoryEntry.nCurrentSize;

    public CCSGOInputHistoryEntryPb* GetInputHistoryEntry(int index)
    {
        ref var inputHistoryEntry = ref CSGOUserCmd->InputHistoryEntry;

        if (index    < 0
            || index >= inputHistoryEntry.nCurrentSize
            || index >= inputHistoryEntry.nAllocatedSize)
        {
            return null;
        }

        return inputHistoryEntry[index];
    }

    public int SubtickMoveSize => BaseUserCmd->SubtickMoves.nCurrentSize;

    public CSubtickMoveStepPb* GetSubtickMove(int index)
    {
        ref var subtickMoves = ref BaseUserCmd->SubtickMoves;

        if (index    < 0
            || index >= subtickMoves.nCurrentSize
            || index >= subtickMoves.nAllocatedSize)
        {
            return null;
        }

        return subtickMoves[index];
    }
}
