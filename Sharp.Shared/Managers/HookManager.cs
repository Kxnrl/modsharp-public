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
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.HookParams;
using Sharp.Shared.Hooks;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface IHookManager
{
    /// <summary>
    ///     Create DetourHook
    /// </summary>
    IDetourHook CreateDetourHook();

    /// <summary>
    ///     Create VMThook
    /// </summary>
    IVirtualHook CreateVirtualHook();

    /// <summary>
    ///     Create MidFuncHook
    /// </summary>
    IMidFuncHook CreateMidFuncHook();

#region Hooks

#region Client Hook

    /// <summary>
    ///     CNetworkGameServer::ConnectClient
    /// </summary>
    IHookType<IConnectClientHookParams, NetworkDisconnectionReason> ConnectClient { get; }

    /// <summary>
    ///     IServerGameClients::ClientConnect
    /// </summary>
    IHookType<IClientConnectHookParams, bool> ClientConnect { get; }

    /// <summary>
    ///     CServerSideClient::ClientCanHear
    /// </summary>
    IHookType<IClientCanHearHookParams, bool> ClientCanHear { get; }

    /// <summary>
    ///     CServerSideClient:CLCMsg_VoiceData
    /// </summary>
    IHookType<IClientSpeakingHookParams, EmptyHookReturn> ClientSpeaking { get; }

#endregion

#region Player Weapons

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::WeaponCanUse
    /// </summary>
    IHookType<IPlayerWeaponCanUseHookParams, bool> PlayerWeaponCanUse { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::WeaponCanSwitch
    /// </summary>
    IHookType<IPlayerWeaponCanSwitchHookParams, bool> PlayerWeaponCanSwitch { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::CanEquip <br />
    ///     <remarks>
    ///         This actually hooks ValidateLineOfSight  <br />
    ///         CS2 doesn't just check LOS, it also checks VIS <br />
    ///         Forcing true still won't allow picking up weapons that fail VIS check <br />
    ///         This is just for convenience to hook CanEquip
    ///     </remarks>
    /// </summary>
    IHookType<IPlayerWeaponCanEquipHookParams, bool> PlayerWeaponCanEquip { get; }

#endregion

#region Player Misc

    /// <summary>
    ///     CCSPlayerPawn::GetPlayerMaxSpeed
    /// </summary>
    IHookType<IPlayerGetMaxSpeedHookParams, float> PlayerGetMaxSpeed { get; }

    /// <summary>
    ///     CCSPlayer_ItemServices::CanAcquire
    ///     <remarks>Used for checking if the player can acquire an item or weapon</remarks>
    /// </summary>
    IHookType<IPlayerCanAcquireHookParams, EAcquireResult> PlayerCanAcquire { get; }

    /// <summary>
    ///     CCSPlayerController::HandleCommandJoinTeam
    /// </summary>
    IHookType<IHandleCommandJoinTeamHookParams, bool> HandleCommandJoinTeam { get; }

    /// <summary>
    ///     CCSPlayerPawn->ItemService::GiveNamedItem
    /// </summary>
    IHookType<IGiveNamedItemHookParams, IBaseWeapon> GiveNamedItem { get; }

#endregion

#region Player Movement

    /// <summary>
    ///     CCSPlayerPawn->MovementService::RunCommand <br />
    ///     <remarks>Fields in MovementService is from the last frame. To get Buttons you can get it from <see cref="IPlayerRunCommandHookParams"/></remarks>
    /// </summary>
    IHookType<IPlayerRunCommandHookParams, EmptyHookReturn> PlayerRunCommand { get; }

#endregion

#region Sound

    /// <summary>
    ///     CSoundEmitterSystem::EmitSound
    /// </summary>
    IHookType<IEmitSoundHookParams, SoundOpEventGuid> EmitSound { get; }

    /// <summary>
    ///     SoundOpGameSystem::DoStartSoundEvent
    /// </summary>
    IHookType<ISoundEventHookParams, SoundOpEventGuid> SoundEvent { get; }

#endregion

#region Damage

    /// <summary>
    ///     CBaseEntity::DispatchTraceAttack (PlayerPawn Only)
    ///     <remarks>Return value is ignored if it returns SkipCall, the return value will be 0</remarks>
    /// </summary>
    IHookType<IPlayerDispatchTraceAttackHookParams, long> PlayerDispatchTraceAttack { get; }

    /// <summary>
    ///     CBaseEntity::DispatchTraceAttack (No PlayerPawn)
    ///     <remarks>Return value is ignored if it returns SkipCall, the return value will be 0</remarks>
    /// </summary>
    IHookType<IEntityDispatchTraceAttackHookParams, long> EntityDispatchTraceAttack { get; }

#endregion

#region Others

    /// <summary>
    ///     CPointServerCommand::InputCommand
    /// </summary>
    IHookType<IPointServerCommandHookParams, EmptyHookReturn> PointServerCommand { get; }

    /// <summary>
    ///     'status' Command <br />
    ///     <remarks>When client is null means it is sent by server</remarks>
    /// </summary>
    IHookType<IPrintStatusHookParams, EmptyHookReturn> PrintStatus { get; }

    /// <summary>
    ///     NetworkMessages::CUserMsgTextMSG
    /// </summary>
    /// <returns>The new Receiver Bits</returns>
    IHookType<ITextMsgHookParams, NetworkReceiver> TextMsg { get; }

    /// <summary>
    ///     IGameEventSystem::PostEventAbstract
    /// </summary>
    /// <returns>The new Receiver Bits</returns>
    IHookType<IPostEventAbstractHookParams, NetworkReceiver> PostEventAbstract { get; }

    /// <summary>
    ///     CCSGameRules::TerminateRound
    /// </summary>
    IHookType<ITerminateRoundHookParams, EmptyHookReturn> TerminateRound { get; }

#endregion

#endregion

#region Forwards

#region Sound

    /// <summary>
    ///     SoundOpGameSystem::DoStartSoundEvent <br />
    ///     <remarks>Only fires when the sound event is treated as music</remarks>
    /// </summary>
    IForwardType<IEmitMusicForwardParams> EmitMusic { get; }

#endregion

#region Player Base

    /// <summary>
    ///     CCSPlayerPawn::PlayerSpawn
    /// </summary>
    IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPre { get; }

    /// <summary>
    ///     CCSPlayerPawn::
    /// </summary>
    IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPost { get; }

    /// <summary>
    ///     CCSPlayerPawn::Killed
    /// </summary>
    IForwardType<IPlayerKilledForwardParams> PlayerKilledPre { get; }

    /// <summary>
    ///     CCSPlayerPawn::Killed
    /// </summary>
    IForwardType<IPlayerKilledForwardParams> PlayerKilledPost { get; }

    /// <summary>
    ///     CCSPlayerPawn::PreThink
    /// </summary>
    IForwardType<IPlayerThinkForwardParams> PlayerPreThink { get; }

    /// <summary>
    ///     CCSPlayerPawn::PostThink
    /// </summary>
    IForwardType<IPlayerThinkForwardParams> PlayerPostThink { get; }

#endregion

#region Player Misc

    /// <summary>
    ///     CCSPlayerPawn->ItemService::GiveGloveItem
    /// </summary>
    IForwardType<IGiveGloveItemForwardParams> GiveGloveItem { get; }

#endregion

#region Player Movement

    /// <summary>
    ///     CCSPlayerPawn->MovementService::WalkMove
    /// </summary>
    IForwardType<IPlayerWalkMoveForwardParams> PlayerWalkMove { get; }

    /// <summary>
    ///     CCSPlayerPawn->MovementService::ProcessMove
    /// </summary>
    IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePre { get; }

    /// <summary>
    ///     CCSPlayerPawn->MovementService::ProcessMove
    /// </summary>
    IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePost { get; }

#endregion

#region Player Weapon

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::SwitchWeapon
    /// </summary>
    IForwardType<IPlayerSwitchWeaponForwardParams> PlayerSwitchWeapon { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::EquipWeapon
    /// </summary>
    IForwardType<IPlayerEquipWeaponForwardParams> PlayerEquipWeapon { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::DropWeapon
    /// </summary>
    IForwardType<IPlayerDropWeaponForwardParams> PlayerDropWeapon { get; }

#endregion

#region Others

    /// <summary>
    ///     CCSGameRules::CreateEndMatchMapGroupVoteOptions
    /// </summary>
    IForwardType<IMapVoteCreatedForwardParams> MapVoteCreated { get; }

#endregion

#endregion
}

public interface IHookType<out TParams, THookReturn> where TParams : class, IFunctionParams
{
    /// <summary>
    ///     Listen to this Hook's Pre <br />
    ///     <remarks>Higher priority value means higher priority</remarks>
    /// </summary>
    void InstallHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre, int priority = 0);

    /// <summary>
    ///     Listen to this Hook's Post <br />
    ///     <remarks>Higher priority value means higher priority</remarks>
    /// </summary>
    void InstallHookPost(Action<TParams, HookReturnValue<THookReturn>> post, int priority = 0);

    /// <summary>
    ///     Stop listening to this Hook's Pre
    /// </summary>
    void RemoveHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre);

    /// <summary>
    ///     Stop listening to this Hook's Post
    /// </summary>
    void RemoveHookPost(Action<TParams, HookReturnValue<THookReturn>> post);
}

public interface IForwardType<out TParams> where TParams : class, IFunctionParams
{
    /// <summary>
    ///     Listen to this Forward call <br />
    ///     <remarks>Higher priority value means higher priority</remarks>
    /// </summary>
    void InstallForward(Action<TParams> func, int priority = 0);

    /// <summary>
    ///     Stop listening to this Forward call
    /// </summary>
    void RemoveForward(Action<TParams> func);
}
