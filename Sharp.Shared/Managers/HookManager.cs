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
    ///     CNetworkGameServer::ConnectClient (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Only installed on dedicated servers (<c>-dedicated</c>). Post hooks are not supported (<see cref="IHookType{TParams, THookReturn}.InstallHookPost" /> throws <see cref="InvalidOperationException" />).
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IConnectClientHookParams, NetworkDisconnectionReason> ConnectClient { get; }

    /// <summary>
    ///     IServerGameClients::ClientConnect (Pre-only).
    /// </summary>
    [Obsolete("Use ConnectClient instead, will be removed in the future", true)]
    IHookType<IClientConnectHookParams, bool> ClientConnect { get; }

    /// <summary>
    ///     CServerSideClient::ClientCanHear (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IClientCanHearHookParams, bool> ClientCanHear { get; }

    /// <summary>
    ///     CServerSideClient::CLCMsg_VoiceData (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Post hooks still run when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IClientSpeakingHookParams, EmptyHookReturn> ClientSpeaking { get; }

#endregion

#region Player Weapons

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::WeaponCanUse (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.IgnoreParamReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerWeaponCanUseHookParams, bool> PlayerWeaponCanUse { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::WeaponCanSwitch (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerWeaponCanSwitchHookParams, bool> PlayerWeaponCanSwitch { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::CanEquip (Pre-only).
    /// </summary>
    /// <remarks>
    ///     This actually hooks <c>ValidateLineOfSight</c>. CS2 also checks VIS in addition to LOS, so forcing
    ///     <see langword="true" /> still will not allow picking up weapons that fail the VIS check. Post hooks are not supported.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerWeaponCanEquipHookParams, bool> PlayerWeaponCanEquip { get; }

#endregion

#region Player Misc

    /// <summary>
    ///     CCSPlayerPawn::GetPlayerMaxSpeed (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported. On Windows this uses a mid-function hook in <c>ProcessMovement</c>; on Linux the original
    ///     <c>GetPlayerMaxSpeed</c> runs first. Returning <see cref="EHookAction.SkipCallReturnOverride" /> overrides the base max speed,
    ///     which is still multiplied by the player's run speed modifier.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerGetMaxSpeedHookParams, float> PlayerGetMaxSpeed { get; }

    /// <summary>
    ///     CCSPlayer_ItemServices::CanAcquire (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Used for checking whether the player can acquire an item or weapon. Post hooks are not supported.
    ///     The engine's original <c>CanAcquire</c> runs before this hook, so <see cref="EHookAction.SkipCallReturnOverride" />
    ///     overrides the returned <see cref="EAcquireResult" /> rather than skipping the original call.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerCanAcquireHookParams, EAcquireResult> PlayerCanAcquire { get; }

    /// <summary>
    ///     CCSPlayerController::HandleCommandJoinTeam (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Post hooks are skipped when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />,
    ///         <see cref="EHookAction.ChangeParamReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IHandleCommandJoinTeamHookParams, bool> HandleCommandJoinTeam { get; }

    /// <summary>
    ///     CCSPlayerPawn->ItemService::GiveNamedItem (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<IGiveNamedItemHookParams, IBaseWeapon> GiveNamedItem { get; }

#endregion

#region Player Movement

    /// <summary>
    ///     CCSPlayerPawn->MovementService::RunCommand (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Fields on <c>MovementService</c> are from the previous frame; read current buttons from
    ///     <see cref="IPlayerRunCommandHookParams" />. Post hooks still run when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerRunCommandHookParams, EmptyHookReturn> PlayerRunCommand { get; }

#endregion

#region Sound

    /// <summary>
    ///     CSoundEmitterSystem::EmitSound (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Post hooks are skipped when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     Sounds triggered through <c>EmitSound</c> do not also fire <see cref="SoundEvent" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<IEmitSoundHookParams, SoundOpEventGuid> EmitSound { get; }

    /// <summary>
    ///     SoundOpGameSystem::DoStartSoundEvent (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Post hooks are skipped when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     Skipped for sounds emitted via <see cref="EmitSound" /> and for sounds classified as map music (which route to <see cref="EmitMusic" /> instead).
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<ISoundEventHookParams, SoundOpEventGuid> SoundEvent { get; }

#endregion

#region Damage

    /// <summary>
    ///     CBaseEntity::DispatchTraceAttack (PlayerPawn Only; Pre and Post).
    /// </summary>
    /// <remarks>
    ///     This hook is only fired when the entity that takes damage is a player pawn.
    ///     Post hooks still run when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         This hook should only return <see cref="EHookAction.Ignored" /> or
    ///         <see cref="EHookAction.SkipCallReturnOverride" />.
    ///         If <see cref="EHookAction.SkipCallReturnOverride" /> is used, the function returns 0 (the specific return value
    ///         provided by the hook is ignored).
    ///     </para>
    ///     <para>
    ///         To make a player take damage programmatically, use <see cref="IBaseEntity.DispatchTraceAttack(in TakeDamageInfo, bool)" /> on the player
    ///         pawn entity.
    ///     </para>
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPlayerDispatchTraceAttackHookParams, long> PlayerDispatchTraceAttack { get; }

    /// <summary>
    ///     CBaseEntity::DispatchTraceAttack (Non-PlayerPawn; Pre and Post).
    /// </summary>
    /// <remarks>
    ///     This hook is only fired when the entity that takes damage is <b>not</b> a player pawn.
    ///     Post hooks still run when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         This hook should only return <see cref="EHookAction.Ignored" /> or
    ///         <see cref="EHookAction.SkipCallReturnOverride" />.
    ///         If <see cref="EHookAction.SkipCallReturnOverride" /> is used, the function returns 0 (the specific return value
    ///         provided by the hook is ignored).
    ///     </para>
    ///     <para>
    ///         To make an entity take damage programmatically, use <see cref="IBaseEntity.DispatchTraceAttack(in TakeDamageInfo, bool)" /> on the
    ///         target entity.
    ///     </para>
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IEntityDispatchTraceAttackHookParams, long> EntityDispatchTraceAttack { get; }

#endregion

#region Others

    /// <summary>
    ///     CPointServerCommand::InputCommand and cs_script's Console.ServerCommand (Pre-only).
    ///     Also covers the deprecated <c>Instance.ServerCommand</c>; for cs_script calls, <c>Entity</c> is the calling <c>point_script</c>.
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported. Commands let through still run the engine's original function; on workshop maps they are
    ///     checked against Valve's command whitelist.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPointServerCommandHookParams, EmptyHookReturn> PointServerCommand { get; }

    /// <summary>
    ///     'status' Command (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported. When <c>Client</c> is <see langword="null" />, the command was issued by the server console.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />
    ///     </para>
    /// </remarks>
    IHookType<IPrintStatusHookParams, EmptyHookReturn> PrintStatus { get; }

    /// <summary>
    ///     NetworkMessages::CUserMsgTextMSG (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported. Skipped when the message has no parameters (<c>param_size &lt; 1</c>) or when sent through
    ///     <see cref="IModSharp.SendNetMessage{T}" /> / ModSharp chat helpers (which bypass network hooks).
    ///     Returns the updated receiver bitmask when using <see cref="EHookAction.ChangeParamReturnDefault" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<ITextMsgHookParams, NetworkReceiver> TextMsg { get; }

    /// <summary>
    ///     IGameEventSystem::PostEventAbstract (Pre-only).
    /// </summary>
    /// <remarks>
    ///     Post hooks are not supported. Requires registering the message ID via <see cref="IModSharp.HookNetMessage" />.
    ///     Never fires for messages handled earlier in the native switch (<c>UM_TextMsg</c>, <c>GE_FireBulletsId</c>,
    ///     <c>TE_MuzzleFlashId</c>, <c>TE_WorldDecalId</c>, <c>TE_BloodStreamId</c>, <c>TE_ArmorRicochetId</c>) or for messages sent via
    ///     <see cref="IModSharp.SendNetMessage{T}" /> (which sets the native bypass flag).
    ///     Returns the updated receiver bitmask when using <see cref="EHookAction.ChangeParamReturnDefault" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<IPostEventAbstractHookParams, NetworkReceiver> PostEventAbstract { get; }

    /// <summary>
    ///     CCSGameRules::TerminateRound (Pre and Post).
    /// </summary>
    /// <remarks>
    ///     Post hooks still run when a Pre hook returns <see cref="EHookAction.SkipCallReturnOverride" />.
    ///     <para>
    ///         Supported HookAction:
    ///         <see cref="EHookAction.Ignored" />,
    ///         <see cref="EHookAction.SkipCallReturnOverride" />,
    ///         <see cref="EHookAction.ChangeParamReturnDefault" />
    ///     </para>
    /// </remarks>
    IHookType<ITerminateRoundHookParams, EmptyHookReturn> TerminateRound { get; }

#endregion

#endregion

#region Forwards

#region Sound

    /// <summary>
    ///     SoundOpGameSystem::DoStartSoundEvent for sounds treated as map music.
    /// </summary>
    /// <remarks>Only fires when <c>ms_hook_map_music</c> is enabled and the sound event is treated as music.</remarks>
    IForwardType<IEmitMusicForwardParams> EmitMusic { get; }

#endregion

#region Player Base

    /// <summary>
    ///     CCSPlayerPawn::PlayerSpawn (Pre).
    /// </summary>
    IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPre { get; }

    /// <summary>
    ///     CCSPlayerPawn::PlayerSpawn (Post).
    /// </summary>
    IForwardType<IPlayerSpawnForwardParams> PlayerSpawnPost { get; }

    /// <summary>
    ///     CCSPlayerPawn::Event_Killed (Pre).
    /// </summary>
    /// <remarks>Only fires when the victim pawn has an associated client.</remarks>
    IForwardType<IPlayerKilledForwardParams> PlayerKilledPre { get; }

    /// <summary>
    ///     CCSPlayerPawn::Event_Killed (Post).
    /// </summary>
    /// <remarks>Only fires when the victim pawn has an associated client.</remarks>
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
    ///     CCSPlayerPawn->ItemService::GiveGloveItem.
    /// </summary>
    /// <remarks>Only fires for authenticated human (non-bot) clients.</remarks>
    IForwardType<IGiveGloveItemForwardParams> GiveGloveItem { get; }

#endregion

#region Player Movement

    /// <summary>
    ///     CPlayer_MovementService::WalkMove
    /// </summary>
    /// <remarks>
    ///     This forward is only called when the player is on the ground with (<see cref="MoveType.Walk" />) and before the
    ///     engine runs its own logic.
    ///     You can use <see cref="IPlayerWalkMoveForwardParams.SetSpeed" /> to override the maximum prestrafe speed.
    /// </remarks>
    IForwardType<IPlayerWalkMoveForwardParams> PlayerWalkMove { get; }

    /// <summary>
    ///     CPlayer_MovementService::ProcessMove
    /// </summary>
    /// <remarks>
    ///     This forward is called regardless of the player's <see cref="MoveType" />, executing before specific movement
    ///     functions (like WalkMove, AirMove).
    /// </remarks>
    IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePre { get; }

    /// <summary>
    ///     CPlayer_MovementService::ProcessMove
    /// </summary>
    /// <remarks>
    ///     This forward is called regardless of the player's <see cref="MoveType" />, executing after the game has processed
    ///     movement functions (like WalkMove, AirMove).
    /// </remarks>
    IForwardType<IPlayerProcessMoveForwardParams> PlayerProcessMovePost { get; }

#endregion

#region Player Weapon

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::SwitchWeapon (Post).
    /// </summary>
    IForwardType<IPlayerSwitchWeaponForwardParams> PlayerSwitchWeapon { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::EquipWeapon (Post).
    /// </summary>
    IForwardType<IPlayerEquipWeaponForwardParams> PlayerEquipWeapon { get; }

    /// <summary>
    ///     CCSPlayerPawn->WeaponService::DropWeapon (Post).
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
    ///     Listen to this Hook's Pre callback.
    /// </summary>
    /// <remarks>
    ///     Higher <paramref name="priority" /> values run first. All Pre hooks run; a subsequent hook's
    ///     <see cref="HookReturnValue{T}" /> only replaces the winning result when its <see cref="EHookAction" /> is
    ///     strictly greater than the current winning action (so on equal actions, the earlier/higher-priority hook wins).
    ///     Leaked hooks are automatically cleaned up with a warning on module unload.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="pre" /> is a multicast delegate.</exception>
    void InstallHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre, int priority = 0);

    /// <summary>
    ///     Listen to this Hook's Post callback.
    /// </summary>
    /// <remarks>
    ///     Higher <paramref name="priority" /> values run first. Not all hooks support Post callbacks.
    ///     Leaked hooks are automatically cleaned up with a warning on module unload.
    /// </remarks>
    /// <exception cref="InvalidOperationException">This hook is Pre-only and does not support Post callbacks.</exception>
    /// <exception cref="ArgumentException"><paramref name="post" /> is a multicast delegate.</exception>
    void InstallHookPost(Action<TParams, HookReturnValue<THookReturn>> post, int priority = 0);

    /// <summary>
    ///     Stop listening to this Hook's Pre callback.
    /// </summary>
    /// <exception cref="EntryPointNotFoundException"><paramref name="pre" /> is not currently registered.</exception>
    void RemoveHookPre(Func<TParams, HookReturnValue<THookReturn>, HookReturnValue<THookReturn>> pre);

    /// <summary>
    ///     Stop listening to this Hook's Post callback.
    /// </summary>
    /// <exception cref="EntryPointNotFoundException"><paramref name="post" /> is not currently registered.</exception>
    void RemoveHookPost(Action<TParams, HookReturnValue<THookReturn>> post);
}

public interface IForwardType<out TParams> where TParams : class, IFunctionParams
{
    /// <summary>
    ///     Listen to this Forward call.
    /// </summary>
    /// <remarks>
    ///     Higher <paramref name="priority" /> values run first.
    ///     Leaked forwards are automatically cleaned up with a warning on module unload.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="func" /> is a multicast delegate.</exception>
    void InstallForward(Action<TParams> func, int priority = 0);

    /// <summary>
    ///     Stop listening to this Forward call.
    /// </summary>
    /// <exception cref="EntryPointNotFoundException"><paramref name="func" /> is not currently registered.</exception>
    void RemoveForward(Action<TParams> func);
}
