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
using Sharp.Shared.Attributes;
using Sharp.Shared.Enums;
using Sharp.Shared.GameObjects;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared.GameEntities;

[NetClass("CCSPlayerController")]
public interface IPlayerController : IBaseEntity
{
    /// <summary>
    ///     Gets the PlayerPawn for this Controller
    /// </summary>
    /// <returns>
    ///     <see cref="IBasePlayerPawn" />
    /// </returns>
    IBasePlayerPawn? GetPawn();

    /// <summary>
    ///     Gets the PlayerPawn for this Controller
    /// </summary>
    /// <returns>
    ///     <see cref="IPlayerPawn" />
    /// </returns>
    IPlayerPawn? GetPlayerPawn();

    /// <summary>
    ///     Gets the ObserverPawn for this Controller
    /// </summary>
    /// <returns>
    ///     <see cref="IObserverPawn" />
    /// </returns>
    IObserverPawn? GetObserverPawn();

    /// <summary>
    ///     Sets the PlayerPawn <br />
    ///     <remarks>You should know what you're doing before calling this</remarks>
    /// </summary>
    void SetPlayerPawn(IPlayerPawn pawn);

    /// <summary>
    ///     Gets the <see cref="IGameClient" /> <br />
    ///     <remarks>Returns <c>null</c> if the player is not in-game, even if the entity exists</remarks>
    /// </summary>
    IGameClient? GetGameClient();

    /// <summary>
    ///     Print message to this player
    /// </summary>
    void Print(HudPrintChannel channel,
        string                 message,
        string?                param1 = null,
        string?                param2 = null,
        string?                param3 = null,
        string?                param4 = null);

    /// <summary>
    ///     Change team without slaying
    /// </summary>
    void SwitchTeam(CStrikeTeam team);

    /// <summary>
    ///     Respawn player
    /// </summary>
    void Respawn();

    /// <summary>
    ///     Set clantag
    /// </summary>
    void SetClanTag(string tag);

    /// <summary>
    ///     Check awn
    /// </summary>
    void CheckPawn();

    /// <summary>
    ///     Set PlayerPawn
    /// </summary>
    void SetPawn(IBasePlayerPawn pawn, bool unknown1, bool unknown2, bool unknown3, bool unknown4);

    /// <summary>
    ///     Is the player connected
    /// </summary>
    bool IsConnected()
        => ConnectedState is PlayerConnectedState.PlayerConnected;

    /// <summary>
    ///     Is the player disconnecting
    /// </summary>
    bool IsDisconnecting()
        => ConnectedState is PlayerConnectedState.PlayerDisconnecting;

    /// <summary>
    ///     Returns a SteamID64 (7656119xxxxxxxxxx)
    /// </summary>
    SteamID SteamId { get; }

    /// <summary>
    ///     PlayerSlot
    /// </summary>
    PlayerSlot PlayerSlot { get; }

    /// <summary>
    ///     Clantag
    /// </summary>
    string ClanTag { get; }

    /// <summary>
    ///     PlayerName, setting the value does not call StateChanged
    /// </summary>
    string PlayerName { get; set; }

    /// <summary>
    ///     Pawn存活状态 <br />
    ///     <remarks>该功能是给计分板用的</remarks>
    /// </summary>
    [Obsolete("Use GetPawn().IsAlive, will be removed in 2.2")]
    bool IsPawnAlive { get; }

    /// <summary>
    ///     Pawn血量 <br />
    ///     <remarks>该功能是给计分板用的</remarks>
    /// </summary>
    [Obsolete("Use GetPawn().Health, will be removed in 2.2")]
    uint PawnHealth { get; }

    /// <summary>
    ///     Is controller in HLTV
    /// </summary>
    bool IsHltv { get; }

    /// <summary>
    ///     Is this controller for FakeClient(Bots)
    /// </summary>
    bool IsFakeClient => Flags.HasFlag(EntityFlags.FakeClient);

    /// <summary>
    ///     Score on scoreboard
    /// </summary>
    int Score { get; set; }

    /// <summary>
    ///     MVP on scoreboard
    /// </summary>
    int MvpCount { get; set; }

    /// <summary>
    ///     Scoreboard update count
    /// </summary>
    int UpdaterCount { get; set; }

    /// <summary>
    ///     m_flLaggedMovementValue in source1
    /// </summary>
    float LaggedMovement { get; set; }

    /// <summary>
    ///     m_iCompetitiveRanking
    /// </summary>
    int CompetitiveRanking { get; set; }

    /// <summary>
    ///     m_iCompetitiveRankType
    /// </summary>
    CompetitiveRankType CompetitiveRankType { get; set; }

    /// <summary>
    ///     m_iCompetitiveWins
    /// </summary>
    int CompetitiveWins { get; set; }

    /// <summary>
    ///     m_iDesiredFOV
    /// </summary>
    uint DesiredFOV { get; set; }

    /// <summary>
    ///     Connection state
    /// </summary>
    PlayerConnectedState ConnectedState { get; }

    /// <summary>
    ///     Will the player switch team when a new round starts
    /// </summary>
    bool SwitchTeamsOnNextRoundReset { get; set; }

    /// <summary>
    ///     m_bRemoveAllItemsOnNextRoundReset
    /// </summary>
    bool RemoveAllItemsOnNextRoundReset { get; set; }

    /// <summary>
    ///     Desired observer mode
    /// </summary>
    int DesiredObserverMode { get; set; }

    /// <summary>
    ///     m_bHasCommunicationAbuseMute
    /// </summary>
    bool HasCommunicationAbuseMute { get; set; }

    /// <summary>
    ///     m_iPendingTeamNum
    /// </summary>
    CStrikeTeam PendingTeamNum { get; set; }

    /// <summary>
    ///     m_bControllingBot
    /// </summary>
    bool ControllingBot { get; set; }

    /// <summary>
    ///     m_iRoundsWon
    /// </summary>
    int RoundsWon { get; set; }

    /// <summary>
    ///     DamageService
    /// </summary>
    IDamageService? GetDamageService();

    /// <summary>
    ///     MoneyService
    /// </summary>
    IInGameMoneyService? GetInGameMoneyService();

    /// <summary>
    ///     InventoryService
    /// </summary>
    IInventoryService? GetInventoryService();

    /// <summary>
    ///     ActionTrackingService
    /// </summary>
    IControllerActionTrackingService? GetActionTrackingService();

    /// <summary>
    ///     Play a soundevent only for this player
    /// </summary>
    /// <param name="sound">The sound event name to play (e.g., "Player.DamageKevlar")</param>
    /// <param name="volume">Volume. If null, uses default volume (1.0f)</param>
    SoundOpEventGuid EmitSoundClient(string sound, float? volume = null);

    /// <summary>
    ///     Gets the equipped inventory item from loadout
    /// </summary>
    IEconItemView? GetItemInLoadoutFromInventory(CStrikeTeam team, int slot);
}
