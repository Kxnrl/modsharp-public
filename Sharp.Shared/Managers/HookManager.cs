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
    /// </summary>
    /// <remarks>
    ///     This hook is only fired when the entity that takes damage is a player pawn.
    ///     <para>
    ///     This hook should only return <see cref="EHookAction.Ignored"/> or <see cref="EHookAction.SkipCallReturnOverride"/>.
    ///     If <see cref="EHookAction.SkipCallReturnOverride"/> is used, the function returns 0 (the specific return value provided by the hook is ignored).
    ///     </para>
    ///     <para>
    ///     To make a player take damage programmatically, use <see cref="IBaseEntity.DispatchTraceAttack"/> on the player pawn entity.
    ///     </para>
    /// </remarks>
    IHookType<IPlayerDispatchTraceAttackHookParams, long> PlayerDispatchTraceAttack { get; }

    /// <summary>
    ///     CBaseEntity::DispatchTraceAttack (No PlayerPawn)
    /// </summary>
    /// <remarks>
    ///     This hook is only fired when the entity that takes damage is <b>not</b> a player pawn.
    ///     <para>
    ///     This hook should only return <see cref="EHookAction.Ignored"/> or <see cref="EHookAction.SkipCallReturnOverride"/>.
    ///     If <see cref="EHookAction.SkipCallReturnOverride"/> is used, the function returns 0 (the specific return value provided by the hook is ignored).
    ///     </para>
    ///     <para>
    ///     To make an entity take damage programmatically, use <see cref="IBaseEntity.DispatchTraceAttack"/> on the target entity.
    ///     </para>
    /// </remarks>
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
    /// <remarks>
    ///     Invoked only for living players. Executes after internal pre-processing logic is complete, 
    ///     such as handling mp_autokick, domination updates, and healthshot recovery.
    /// </remarks>
    IForwardType<IPlayerThinkForwardParams> PlayerPreThink { get; }

    /// <summary>
    ///     CCSPlayerPawn::PostThink
    /// </summary>
    /// <remarks>
    ///     Invoked only for living players. Runs after the game has done processing movement, attack
    ///     animation update for the player.
    /// </remarks>
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
    ///     CPlayer_MovementService::WalkMove
    /// </summary>
    /// <remarks>
    ///     This forward is only called when the player is on the ground with (<see cref="MoveType.Walk"/>) and before the engine runs its own logic.
    ///     You can use <see cref="IPlayerWalkMoveForwardParams.SetSpeed"/> to override the maximum prestrafe speed.
    /// </remarks>
    IForwardType<IPlayerWalkMoveForwardParams> PlayerWalkMove { get; }

    /// <summary>
    ///     CPlayer_MovementService::ProcessMove
    /// </summary>
    /// <remarks>
    ///     This forward is called regardless of the player's <see cref="MoveType"/>, executing before specific movement functions (like WalkMove, AirMove).
    /// </remarks>
    IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePre { get; }

    /// <summary>
    ///     CPlayer_MovementService::ProcessMove
    /// </summary>
    /// <remarks>
    ///     This forward is called regardless of the player's <see cref="MoveType"/>, executing after the game has processed movement functions (like WalkMove, AirMove).
    /// </remarks>
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
