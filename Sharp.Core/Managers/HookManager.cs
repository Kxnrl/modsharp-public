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
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Interfaces;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.GameObjects;
using Sharp.Core.Managers.Forwards;
using Sharp.Core.Managers.Hooks;
using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.GameObjects;
using Sharp.Shared.HookParams;
using Sharp.Shared.Hooks;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace Sharp.Core.Managers;

// ReSharper disable ForCanBeConvertedToForeach
internal interface ICoreHookManager : IHookManager;

#region Hook Impl

internal interface ICoreHookType<in TParams, THookReturn> where TParams : class, IFunctionParams
{
    /// <summary>
    ///     泛播给其他的订户
    /// </summary>
    /// <typeparam name="TParams">Hook参数</typeparam>
    /// <typeparam name="THookReturn">Hook的返回值 (如果是void, 直接写int就好)</typeparam>
    /// <param name="params"></param>
    /// <returns>封装了Hook操作和Hook的返回值</returns>
    HookReturnValue<THookReturn> InvokeHookPre(TParams @params);

    void InvokeHookPost(TParams @params, HookReturnValue<THookReturn> @return);

    bool IsHookInvokeRequired(bool isPost);
}

internal abstract class HookType<TParams, THookReturn, TClass> : ICoreHookType<TParams, THookReturn>,
    IHookType<TParams, THookReturn>
    where TParams : class, IFunctionParams
    where TClass : HookType<TParams, THookReturn, TClass>
{
    private record PreHookInfo(
        Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> Callback,
        int                                                                       Priority);

    private record PostHookInfo(Action<TParams, HookReturnValue<THookReturn>> Callback, int Priority);

    private readonly List<PreHookInfo>  _preHooks;
    private readonly List<PostHookInfo> _postHooks;

    private readonly ILogger _logger;

    protected abstract bool AllowPre  { get; }
    protected abstract bool AllowPost { get; }

    protected HookType(ILoggerFactory factory)
    {
        _preHooks  = [];
        _postHooks = [];

        _logger = factory.CreateLogger<TClass>();
    }

    public void InstallHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre, int priority)
    {
        if (!AllowPre)
        {
            throw new InvalidOperationException($"Pre Hook of {GetType().Name} is not support");
        }

        if (_preHooks.Any(x => x.Callback.Equals(pre)))
        {
            return;
        }

        _preHooks.Add(new PreHookInfo(pre, priority));
        _preHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
    }

    public void InstallHookPost(Action<TParams, HookReturnValue<THookReturn>> post, int priority)
    {
        if (!AllowPost)
        {
            throw new InvalidOperationException($"Post Hook of {GetType().Name} is not support");
        }

        if (_postHooks.Any(x => x.Equals(post)))
        {
            return;
        }

        _postHooks.Add(new PostHookInfo(post, priority));
        _postHooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
    }

    public void RemoveHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre)
    {
        if (_preHooks.Find(x => x.Callback.Equals(pre)) is not { } hook)
        {
            throw new EntryPointNotFoundException("The forward does not hook");
        }

        _preHooks.Remove(hook);
    }

    public void RemoveHookPost(Action<TParams, HookReturnValue<THookReturn>> post)
    {
        if (_postHooks.Find(x => x.Callback.Equals(post)) is not { } hook)
        {
            throw new EntryPointNotFoundException("The forward does not hook");
        }

        _postHooks.Remove(hook);
    }

    public HookReturnValue<THookReturn> InvokeHookPre(TParams @params)
    {
        if (_preHooks.Count == 0)
        {
            return new HookReturnValue<THookReturn>(EHookAction.Ignored);
        }

        var          action = EHookAction.Ignored;
        THookReturn? result = default;

        for (var i = 0; i < _preHooks.Count; i++)
        {
            try
            {
                var hook = _preHooks[i];
                var call = hook.Callback(@params, new HookReturnValue<THookReturn>(action, result));

                if (call.Action > action)
                {
                    action = call.Action;
                    result = call.ReturnValue;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                                 "An error occurred while calling pre hook of {type}",
                                 GetType().Name);
            }
        }

        return new HookReturnValue<THookReturn>(action, result);
    }

    public void InvokeHookPost(TParams @params, HookReturnValue<THookReturn> @return)
    {
        if (_postHooks.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _postHooks.Count; i++)
        {
            try
            {
                _postHooks[i].Callback(@params, @return);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling post hook of {type}", GetType().Name);
            }
        }
    }

    public bool IsHookInvokeRequired(bool isPost)
        => !isPost ? _preHooks.Count != 0 : _postHooks.Count != 0;
}

#endregion

#region Forward Impl

internal interface ICoreForwardType<in TParams> where TParams : class, IFunctionParams
{
    void InvokeForward(TParams @params);

    bool IsForwardInvokeRequired();
}

internal abstract class ForwardType<TParams, TClass> : ICoreForwardType<TParams>, IForwardType<TParams>
    where TParams : class, IFunctionParams
    where TClass : ForwardType<TParams, TClass>
{
    private record HookInfo(Action<TParams> Callback, int Priority);

    private readonly List<HookInfo> _hooks;

    private readonly ILogger<TClass> _logger;

    protected ForwardType(ILoggerFactory factory)
    {
        _hooks  = [];
        _logger = factory.CreateLogger<TClass>();
    }

    public void InstallForward(Action<TParams> func, int priority)
    {
        if (_hooks.Any(x => x.Callback.Equals(func)))
        {
            return;
        }

        _hooks.Add(new HookInfo(func, priority));
        _hooks.Sort((x, y) => y.Priority.CompareTo(x.Priority));
    }

    public void RemoveForward(Action<TParams> func)
    {
        if (_hooks.Find(x => x.Callback.Equals(func)) is not { } hook)
        {
            throw new EntryPointNotFoundException("The forward does not hook");
        }

        _hooks.Remove(hook);
    }

    public void InvokeForward(TParams @params)
    {
        if (_hooks.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _hooks.Count; i++)
        {
            try
            {
                _hooks[i].Callback(@params);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while calling forward of {type}", GetType().Name);
            }
        }
    }

    public bool IsForwardInvokeRequired()
        => _hooks.Count > 0;
}

#endregion

internal class HookManager : ICoreHookManager
{
#region Hooks

#region Client Hook

    private readonly ConnectClientHook  _connectClient;
    private readonly ClientConnectHook  _clientConnect;
    private readonly ClientCanHearHook  _clientCanHear;
    private readonly ClientSpeakingHook _clientSpeaking;

#endregion

#region Player Weapons

    private readonly PlayerWeaponCanUseHook    _playerWeaponCanUse;
    private readonly PlayerWeaponCanSwitchHook _playerWeaponCanSwitch;
    private readonly PlayerWeaponCanEquipHook  _playerWeaponCanEquip;

#endregion

#region Player Misc

    private readonly PlayerCanAcquireHook      _playerCanAcquire;
    private readonly PlayerGetMaxSpeedHook     _playerGetMaxSpeed;
    private readonly GiveNamedItemHook         _giveNamedItemHookType;
    private readonly HandleCommandJoinTeamHook _handleCommandJoinTeam;

#endregion

#region Player Movement

    private readonly PlayerRunCommandHook _playerRunCommand;

#endregion

#region Sound

    private readonly EmitSoundHook  _emitSound;
    private readonly SoundEventHook _soundEvent;

#endregion

#region Damage

    private readonly EntityDispatchTraceAttackHook _entityDispatchTraceAttack;
    private readonly PlayerDispatchTraceAttackHook _playerDispatchTraceAttack;

#endregion

#region Others

    private readonly PointServerCommandHook _pointServerCommand;
    private readonly PostEventAbstractHook  _postEventAbstract;
    private readonly PrintStatusHook        _printStatus;
    private readonly TextMsgHook            _textMsg;
    private readonly TerminateRoundHook     _terminateRound;

#endregion

#endregion

#region Forwards

#region Sound

    private readonly EmitMusicForward _emitMusic;

#endregion

#region Player Base

    private readonly PlayerSpawnPreForward   _playerSpawnPre;
    private readonly PlayerSpawnPostForward  _playerSpawnPost;
    private readonly PlayerKilledPreForward  _playerKilledPre;
    private readonly PlayerKilledPostForward _playerKilledPost;
    private readonly PlayerPreThinkForward   _playerPreThink;
    private readonly PlayerPostThinkForward  _playerPostThink;

#endregion

#region Player Misc

    private readonly GiveGloveItemForward _giveGloveItem;

#endregion

#region Player Movement

    private readonly PlayerWalkMoveForward               _playerWalkMove;
    private readonly PlayerProcessMovePreForward         _playerProcessMovePre;
    private readonly PlayerProcessMovePostForward        _playerProcessMovePost;

#endregion

#region Player Weapon

    private readonly PlayerSwitchWeaponForward _playerSwitchWeapon;
    private readonly PlayerEquipWeaponForward  _playerEquipWeapon;
    private readonly PlayerDropWeaponForward   _playerDropWeapon;

#endregion

#region Others

    private readonly MapVoteCreatedForward _mapVoteCreated;

#endregion

#endregion

    public HookManager(ILoggerFactory factory)
    {
        _clientCanHear             = new ClientCanHearHook(factory);
        _clientConnect             = new ClientConnectHook(factory);
        _connectClient             = new ConnectClientHook(factory);
        _clientSpeaking            = new ClientSpeakingHook(factory);
        _playerCanAcquire          = new PlayerCanAcquireHook(factory);
        _playerGetMaxSpeed         = new PlayerGetMaxSpeedHook(factory);
        _playerWeaponCanUse        = new PlayerWeaponCanUseHook(factory);
        _playerWeaponCanSwitch     = new PlayerWeaponCanSwitchHook(factory);
        _playerWeaponCanEquip      = new PlayerWeaponCanEquipHook(factory);
        _playerRunCommand          = new PlayerRunCommandHook(factory);
        _emitSound                 = new EmitSoundHook(factory);
        _soundEvent                = new SoundEventHook(factory);
        _entityDispatchTraceAttack = new EntityDispatchTraceAttackHook(factory);
        _playerDispatchTraceAttack = new PlayerDispatchTraceAttackHook(factory);
        _giveNamedItemHookType     = new GiveNamedItemHook(factory);
        _handleCommandJoinTeam     = new HandleCommandJoinTeamHook(factory);
        _pointServerCommand        = new PointServerCommandHook(factory);
        _postEventAbstract         = new PostEventAbstractHook(factory);
        _printStatus               = new PrintStatusHook(factory);
        _textMsg                   = new TextMsgHook(factory);
        _terminateRound            = new TerminateRoundHook(factory);

        _emitMusic             = new EmitMusicForward(factory);
        _playerKilledPre       = new PlayerKilledPreForward(factory);
        _playerKilledPost      = new PlayerKilledPostForward(factory);
        _giveGloveItem         = new GiveGloveItemForward(factory);
        _playerSpawnPre        = new PlayerSpawnPreForward(factory);
        _playerSpawnPost       = new PlayerSpawnPostForward(factory);
        _playerPreThink        = new PlayerPreThinkForward(factory);
        _playerPostThink       = new PlayerPostThinkForward(factory);
        _playerWalkMove        = new PlayerWalkMoveForward(factory);
        _playerProcessMovePre  = new PlayerProcessMovePreForward(factory);
        _playerProcessMovePost = new PlayerProcessMovePostForward(factory);
        _playerSwitchWeapon    = new PlayerSwitchWeaponForward(factory);
        _playerEquipWeapon     = new PlayerEquipWeaponForward(factory);
        _playerDropWeapon      = new PlayerDropWeaponForward(factory);
        _mapVoteCreated        = new MapVoteCreatedForward(factory);
    }

    public IDetourHook CreateDetourHook()
        => new DetourHook();

    public IMidFuncHook CreateMidFuncHook()
        => new MidFuncHook();

    public IVirtualHook CreateVirtualHook()
        => new VirtualHook();

    public IHookType<IConnectClientHookParams, NetworkDisconnectionReason> ConnectClient => _connectClient;
    public IHookType<IClientConnectHookParams, bool> ClientConnect => _clientConnect;
    public IHookType<IClientCanHearHookParams, bool> ClientCanHear => _clientCanHear;
    public IHookType<IClientSpeakingHookParams, EmptyHookReturn> ClientSpeaking => _clientSpeaking;
    public IHookType<IPlayerWeaponCanUseHookParams, bool> PlayerWeaponCanUse => _playerWeaponCanUse;
    public IHookType<IPlayerWeaponCanSwitchHookParams, bool> PlayerWeaponCanSwitch => _playerWeaponCanSwitch;
    public IHookType<IPlayerWeaponCanEquipHookParams, bool> PlayerWeaponCanEquip => _playerWeaponCanEquip;
    public IHookType<IPlayerGetMaxSpeedHookParams, float> PlayerGetMaxSpeed => _playerGetMaxSpeed;
    public IHookType<IPlayerRunCommandHookParams, EmptyHookReturn> PlayerRunCommand => _playerRunCommand;
    public IHookType<IPlayerCanAcquireHookParams, EAcquireResult> PlayerCanAcquire => _playerCanAcquire;
    public IHookType<IHandleCommandJoinTeamHookParams, bool> HandleCommandJoinTeam => _handleCommandJoinTeam;
    public IHookType<IGiveNamedItemHookParams, IBaseWeapon> GiveNamedItem => _giveNamedItemHookType;
    public IHookType<IEmitSoundHookParams, SoundOpEventGuid> EmitSound => _emitSound;
    public IHookType<ISoundEventHookParams, SoundOpEventGuid> SoundEvent => _soundEvent;
    public IHookType<IPlayerDispatchTraceAttackHookParams, long> PlayerDispatchTraceAttack => _playerDispatchTraceAttack;
    public IHookType<IEntityDispatchTraceAttackHookParams, long> EntityDispatchTraceAttack => _entityDispatchTraceAttack;
    public IHookType<IPointServerCommandHookParams, EmptyHookReturn> PointServerCommand => _pointServerCommand;
    public IHookType<IPrintStatusHookParams, EmptyHookReturn> PrintStatus => _printStatus;
    public IHookType<ITextMsgHookParams, NetworkReceiver> TextMsg => _textMsg;
    public IHookType<IPostEventAbstractHookParams, NetworkReceiver> PostEventAbstract => _postEventAbstract;
    public IHookType<ITerminateRoundHookParams, EmptyHookReturn> TerminateRound => _terminateRound;
    public IForwardType<IEmitMusicForwardParams> EmitMusic => _emitMusic;
    public IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPre => _playerSpawnPre;
    public IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPost => _playerSpawnPost;
    public IForwardType<IPlayerKilledForwardParams> PlayerKilledPre => _playerKilledPre;
    public IForwardType<IPlayerKilledForwardParams> PlayerKilledPost => _playerKilledPost;
    public IForwardType<IPlayerThinkForwardParams> PlayerPreThink => _playerPreThink;
    public IForwardType<IPlayerThinkForwardParams> PlayerPostThink => _playerPostThink;
    public IForwardType<IGiveGloveItemForwardParams> GiveGloveItem => _giveGloveItem;
    public IForwardType<IPlayerWalkMoveForwardParams> PlayerWalkMove => _playerWalkMove;
    public IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePre => _playerProcessMovePre;
    public IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePost => _playerProcessMovePost;
    public IForwardType<IPlayerSwitchWeaponForwardParams> PlayerSwitchWeapon => _playerSwitchWeapon;
    public IForwardType<IPlayerEquipWeaponForwardParams> PlayerEquipWeapon => _playerEquipWeapon;
    public IForwardType<IPlayerDropWeaponForwardParams> PlayerDropWeapon => _playerDropWeapon;
    public IForwardType<IMapVoteCreatedForwardParams> MapVoteCreated => _mapVoteCreated;
}

#region Function Params

internal abstract class FunctionParams : ContextObject, IFunctionParams
{
    public bool PostHook { get; }

    protected FunctionParams(bool postHook)
        => PostHook = postHook;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckEditable()
    {
        if (!PostHook)
        {
            return;
        }

        throw new InvalidOperationException("You could not edit this in post-hook!");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IGameClient CreateClient(nint ptr)
        => GameClient.Create(ptr) ?? throw new InvalidOperationException("Client Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IBaseEntity CreateEntity(nint ptr)
        => BaseEntity.Create(ptr) ?? throw new InvalidOperationException("Entity Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IBaseWeapon CreateWeapon(nint ptr)
        => BaseWeapon.Create(ptr) ?? throw new InvalidOperationException("Weapon Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IWeaponService CreateWeaponService(nint ptr)
        => WeaponService.Create(ptr) ?? throw new InvalidOperationException("WeaponService Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IPlayerMovementService CreatePlayerMovementService(nint ptr)
        => PlayerMovementService.Create(ptr) ?? throw new InvalidOperationException("MovementService Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IMovementService CreateBaseMovementService(nint ptr)
        => MovementService.Create(ptr) ?? throw new InvalidOperationException("MovementService Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IPlayerPawn CreatePlayerPawn(nint ptr)
        => PlayerPawn.Create(ptr) ?? throw new InvalidOperationException("PlayerPawn Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IBasePlayerPawn CreateBasePlayerPawn(nint ptr)
        => BasePlayerPawn.Create(ptr) ?? throw new InvalidOperationException("PlayerPawn Pointer is null");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IPlayerController CreatePlayerController(nint ptr)
        => PlayerController.Create(ptr) ?? throw new InvalidOperationException("PlayerController Pointer is null");
}

internal abstract class ClientFunctionParams : FunctionParams
{
    private readonly nint _ptrClient;

    protected ClientFunctionParams(bool postHook, nint client) : base(postHook)
        => _ptrClient = client;

    private IGameClient? _client;

    public IGameClient Client
    {
        get
        {
            if (_client is { } client)
            {
                return client;
            }

            CheckDisposed();
            _client = CreateClient(_ptrClient);

            return _client;
        }
    }
}

internal abstract class PlayerFunctionParams : ClientFunctionParams
{
    private readonly nint _ptrController;

    protected PlayerFunctionParams(bool postHook, nint client, nint controller) : base(postHook, client)
        => _ptrController = controller;

    private IPlayerController? _controller;

    public IPlayerController Controller
    {
        get
        {
            if (_controller is { } controller)
            {
                return controller;
            }

            CheckDisposed();
            _controller = CreatePlayerController(_ptrController);

            return _controller;
        }
    }
}

internal abstract class PlayerPawnFunctionParams : PlayerFunctionParams
{
    private readonly nint _ptrPawn;

    protected PlayerPawnFunctionParams(bool postHook, nint client, nint controller, nint pawn) : base(postHook,
        client,
        controller)
        => _ptrPawn = pawn;

    private IPlayerPawn? _pawn;

    public IPlayerPawn Pawn
    {
        get
        {
            if (_pawn is { } pawn)
            {
                return pawn;
            }

            CheckDisposed();
            _pawn = CreatePlayerPawn(_ptrPawn);

            return _pawn;
        }
    }
}

internal abstract class PlayerWeaponFunctionParams : PlayerPawnFunctionParams
{
    private readonly nint _ptrWeapon;

    protected PlayerWeaponFunctionParams(bool postHook, nint client, nint controller, nint pawn, nint weapon) : base(postHook,
        client,
        controller,
        pawn)
        => _ptrWeapon = weapon;

    private IBaseWeapon? _weapon;

    public IBaseWeapon Weapon
    {
        get
        {
            if (_weapon is { } weapon)
            {
                return weapon;
            }

            CheckDisposed();
            _weapon = CreateWeapon(_ptrWeapon);

            return _weapon;
        }
    }
}

internal abstract class PlayerWeaponWithServiceFunctionParams : PlayerWeaponFunctionParams
{
    private readonly nint _ptrService;

    protected PlayerWeaponWithServiceFunctionParams(bool postHook,
        nint                                             client,
        nint                                             controller,
        nint                                             pawn,
        nint                                             weapon,
        nint                                             ptrService) : base(postHook, client, controller, pawn, weapon)
        => _ptrService = ptrService;

    private IWeaponService? _service;

    public IWeaponService Service
    {
        get
        {
            if (_service is { } service)
            {
                return service;
            }

            CheckDisposed();
            _service = CreateWeaponService(_ptrService);

            return _service;
        }
    }
}

internal abstract class PlayerMovementFunctionParams : PlayerPawnFunctionParams
{
    private readonly nint _ptrService;

    protected PlayerMovementFunctionParams(bool postHook, nint client, nint controller, nint pawn, nint ptrService) : base(
        postHook,
        client,
        controller,
        pawn)
        => _ptrService = ptrService;

    private IPlayerMovementService? _service;

    public IPlayerMovementService Service
    {
        get
        {
            if (_service is { } service)
            {
                return service;
            }

            CheckDisposed();
            _service = CreatePlayerMovementService(_ptrService);

            return _service;
        }
    }
}

internal abstract unsafe class PlayerMovementWithDataFunctionParams : PlayerMovementFunctionParams
{
    private readonly MoveData* _ptrInfo;

    protected PlayerMovementWithDataFunctionParams(bool postHook,
        nint                                            client,
        nint                                            controller,
        nint                                            pawn,
        nint                                            ptrService,
        nint                                            ptrInfo) : base(postHook, client, controller, pawn, ptrService)
        => _ptrInfo = (MoveData*) ptrInfo;

    public MoveData* Info
    {
        get
        {
            CheckDisposed();

            return _ptrInfo;
        }
    }

    public Vector ViewAngles
    {
        get => Info->ViewAngles;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->ViewAngles = value;
        }
    }

    public Vector Velocity
    {
        get => Info->Velocity;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Velocity = value;
        }
    }

    public Vector Angles
    {
        get => Info->Angles;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->Angles = value;
        }
    }

    public Vector AbsOrigin
    {
        get => Info->AbsOrigin;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->AbsOrigin = value;
        }
    }

    public float MaxSpeed
    {
        get => Info->MaxSpeed;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->MaxSpeed = value;
        }
    }

    public float ClientMaxSpeed
    {
        get => Info->ClientMaxSpeed;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->ClientMaxSpeed = value;
        }
    }

    public Vector OutWishVel
    {
        get => Info->OutWishVel;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->OutWishVel = value;
        }
    }

    public bool InAir
    {
        get => Info->InAir;
        set
        {
            CheckDisposed();
            CheckEditable();
            _ptrInfo->InAir = value;
        }
    }
}

internal abstract class EntityFunctionParams : FunctionParams
{
    private readonly nint _ptrEntity;

    protected EntityFunctionParams(bool postHook, nint entity) : base(postHook)
        => _ptrEntity = entity;

    private IBaseEntity? _entity;

    public IBaseEntity Entity
    {
        get
        {
            if (_entity is { } entity)
            {
                return entity;
            }

            CheckDisposed();
            _entity = CreateEntity(_ptrEntity);

            return _entity;
        }
    }
}

#endregion
