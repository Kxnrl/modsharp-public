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

using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Units;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IGameClient : INativeObject
{
    /// <summary>
    ///     Print message to client console
    /// </summary>
    void ConsolePrint(string message);

    /// <summary>
    ///     Override player's in-game name
    /// </summary>
    void SetName(string name);

    /// <summary>
    ///     Send chat message as this client
    /// </summary>
    /// <exception cref="System.InvalidOperationException">The client has no player controller</exception>
    void SayChatMessage(bool teamOnly, string message);

    /// <summary>
    ///     Get client IP address.
    /// </summary>
    /// <param name="withPort"><see langword="true" /> to include the port; <see langword="false" /> for IP address only.</param>
    /// <returns>The IP address string, or <see langword="null" /> for bots/HLTV.</returns>
    string? GetAddress(bool withPort);

    /// <summary>
    ///     Get time since client connected (persists across map changes for human players; returns server uptime for bots/HLTV).
    /// </summary>
    float GetTimeConnected();

    /// <summary>
    ///     Send a console command to the client via <c>IVEngineServer2::ClientCommand</c> (subject to <c>FCVAR_SERVER_CAN_EXECUTE</c>).
    /// </summary>
    void Command(string command);

    /// <summary>
    ///     Execute a command on the server directly via <c>IServerGameClients::ClientCommand</c> (bypasses <c>ExecuteStringCommand</c>).
    /// </summary>
    void FakeCommand(string command);

    /// <summary>
    ///     Synthesize a <c>CNETMsg_StringCmd</c> and invoke <c>CServerSideClient::ExecuteStringCommand</c> (triggers ModSharp <c>ms_</c> commands and listeners).
    /// </summary>
    void ExecuteStringCommand(string command);

    /// <summary>
    ///     Force send full update to client
    /// </summary>
    void ForceFullUpdate();

    /// <summary>
    ///     Get client info ConVar value
    /// </summary>
    string? GetConVarValue(string cvarName);

    /// <summary>
    ///     Get client ConVars as a cached <see cref="IKeyValues" /> wrapper tied to this client's lifetime (cannot be deleted manually).
    /// </summary>
    IKeyValues? GetConVars();

    /// <summary>
    ///     Get <see cref="IPlayerController" />.
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="null" /> if <see cref="IsInGame" /> is <see langword="false" /> or the controller's
    ///     <see cref="IPlayerController.ConnectedState" /> is not <see cref="PlayerConnectedState.PlayerConnected" />.
    /// </remarks>
    IPlayerController? GetPlayerController();

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
    ///     Print a HUD message with limited HTML format support to this player.
    ///     Includes a flashing-fix workaround (skipped during warmup and Deathmatch). No-op for fake clients or disconnected clients.
    /// </summary>
    /// <param name="message">Message to print</param>
    /// <param name="duration">Display duration in seconds</param>
    void PrintCenterHtml(string message, int duration = 1);

    /// <summary>
    ///     Client sign-on state
    /// </summary>
    SignOnState SignOnState { get; }

    /// <summary>
    ///     Whether this is a fake client
    /// </summary>
    bool IsFakeClient { get; }

    /// <summary>
    ///     Whether this is HLTV
    /// </summary>
    bool IsHltv { get; }

    /// <summary>
    ///     User ID
    /// </summary>
    UserID UserId { get; }

    /// <summary>
    ///     Steam ID (64-bit); returns <c>0</c> for fake clients.
    /// </summary>
    SteamID SteamId { get; }

    /// <summary>
    ///     Client engine slot (PlayerSlot)
    /// </summary>
    PlayerSlot Slot { get; }

    /// <summary>
    ///     Controller entity index
    /// </summary>
    EntityIndex ControllerIndex { get; }

    /// <summary>
    ///     Player name
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Whether client is Perfect World user or has low violence mode enabled
    /// </summary>
    bool PerfectWorld { get; }

    /// <summary>
    ///     Whether Steam ID has been authenticated by Steam servers (reads native <c>m_FullyAuthenticated</c> directly; <see langword="false" /> for fake clients).
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Whether this client wrapper still references the same active client slot and has <see cref="SignOnState" /> &gt;= <see cref="SignOnState.Connected" />.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    ///     Whether <see cref="IsValid" /> is <see langword="true" /> and <see cref="SignOnState" /> is less than <see cref="SignOnState.ChangeLevel" />.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Whether <see cref="IsValid" /> is <see langword="true" /> and <see cref="SignOnState" /> is <see cref="SignOnState.Full" />.
    /// </summary>
    bool IsInGame { get; }

    /// <summary>
    ///     Time since client connected (in seconds).
    /// </summary>
    /// <remarks>
    ///     For human players: time since joining the server (persists across map changes).
    ///     For bots/HLTV: returns server uptime.
    /// </remarks>
    float TimeConnected { get; }

    /// <summary>
    ///     Client IP address without port (equivalent to <c>GetAddress(false)</c>).
    /// </summary>
    /// <remarks>Always <see langword="null" /> for bots/HLTV. Use <see cref="GetAddress(bool)" /> with <see langword="true" /> to include the port.</remarks>
    string? Address { get; }

    new int GetHashCode();

    new bool Equals(object? obj);
}
