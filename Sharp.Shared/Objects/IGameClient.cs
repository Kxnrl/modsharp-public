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
    void SayChatMessage(bool teamOnly, string message);

    /// <summary>
    ///     Get client IP address
    /// </summary>
    /// <returns></returns>
    string? GetAddress(bool withPort);

    /// <summary>
    ///     Get time since client connected
    /// </summary>
    float GetTimeConnected();

    /// <summary>
    ///     Execute command (via Client/NetChannel)
    /// </summary>
    void Command(string command);

    /// <summary>
    ///     Execute command (via GameClients)
    /// </summary>
    void FakeCommand(string command);

    /// <summary>
    ///     Execute command (via Engine)
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
    ///     Get client ConVars as KeyValues object
    /// </summary>
    IKeyValues? GetConVars();

    /// <summary>
    ///     Get <see cref="IPlayerController" /> <br />
    ///     <remarks>
    ///         Returns <c>null</c> if the player is not on the server, even if the controller entity exists
    ///     </remarks>
    /// </summary>
    IPlayerController? GetPlayerController();

    /// <summary>
    ///     Client sign-on state
    /// </summary>
    SignOnState SignOnState { get; }

    /// <summary>
    ///     Whether this is a fake client <br />
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
    ///     Steam ID (64-bit)
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
    ///     Player name <br />
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Whether client is Perfect World user or has low violence mode enabled
    /// </summary>
    bool PerfectWorld { get; }

    /// <summary>
    ///     Whether Steam ID has been authenticated by Steam servers
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Whether this client pointer is valid
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    ///     Whether this client is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Whether this client is in-game
    /// </summary>
    bool IsInGame { get; }

    /// <summary>
    ///     <remarks>For players: time since joining server (persists across map changes)</remarks>
    ///     <br />
    ///     <remarks>For bots/HLTV: always returns server uptime</remarks>
    /// </summary>
    float TimeConnected { get; }

    /// <summary>
    ///     Client IP address and port <br />
    ///     <remarks>Always null for bots/HLTV</remarks>
    /// </summary>
    string? Address { get; }

    new int GetHashCode();

    new bool Equals(object? obj);
}
