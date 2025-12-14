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
using Sharp.Shared.CStrike;
using Sharp.Shared.GameEntities;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IGameEvent : INativeObject
{
    /// <summary>
    ///     Set event field to string value
    /// </summary>
    void SetString(string key, string value);

    /// <summary>
    ///     Set event field to float value
    /// </summary>
    void SetFloat(string key, float value);

    /// <summary>
    ///     Set event field to integer value
    /// </summary>
    void SetInt(string key, int value);

    /// <summary>
    ///     Set event field to 64-bit unsigned integer value
    /// </summary>
    void SetUInt64(string key, ulong value);

    /// <summary>
    ///     Set event field to player controller
    /// </summary>
    void SetPlayer(string key, IPlayerController controller);

    /// <summary>
    ///     Set event field to player pawn
    /// </summary>
    void SetPlayer(string key, IPlayerPawn pawn);

    /// <summary>
    ///     Set event field to player by slot
    /// </summary>
    void SetPlayer(string key, int slot);

    /// <summary>
    ///     Set event field to boolean value
    /// </summary>
    void SetBool(string key, bool value);

    /// <summary>
    ///     Get event field as boolean value
    /// </summary>
    bool GetBool(string key);

    /// <summary>
    ///     Get event field as string value
    /// </summary>
    string GetString(string key, string defaultValue = "");

    /// <summary>
    ///     Get event field as float value
    /// </summary>
    float GetFloat(string key, float defaultValue = 0.0f);

    /// <summary>
    ///     Get event field as integer value
    /// </summary>
    int GetInt(string key, int defaultValue = 0);

    /// <summary>
    ///     Get event field as 64-bit unsigned integer value
    /// </summary>
    ulong GetUInt64(string key, ulong defaultValue = 0);

    /// <summary>
    ///     Get event field as PlayerController
    /// </summary>
    IPlayerController? GetPlayerController(string key);

    /// <summary>
    ///     Get event field as PlayerPawn
    ///     <remarks>
    ///         Returns null if entity is an Observer
    ///     </remarks>
    /// </summary>
    IPlayerPawn? GetPlayerPawn(string key);

    /// <summary>
    ///     Get BasePlayerPawn, matches original game behavior
    /// </summary>
    IBasePlayerPawn? GetBasePlayerPawn(string key);

    /// <summary>
    ///     Get event name
    /// </summary>
    /// <returns></returns>
    string GetName();

    /// <summary>
    ///     Fire event <br />
    ///     <remarks>Throws exception if called on non-custom created event</remarks>
    /// </summary>
    /// <param name="serverOnly">Don't send to clients</param>
    void Fire(bool serverOnly);

    /// <summary>
    ///     Fire event to specific client <br />
    ///     <remarks>Throws exception if called on non-custom created event</remarks>
    /// </summary>
    /// <param name="slot">IGameClient index</param>
    void FireToClient(int slot);

    /// <summary>
    ///     Fire event to specific client <br />
    ///     <remarks>Throws exception if called on non-custom created event</remarks>
    /// </summary>
    /// <param name="client">IGameClient</param>
    void FireToClient(IGameClient client);

    /// <summary>
    ///     Fire event to all clients<br />
    ///     <remarks>Throws exception if called on non-custom created event</remarks>
    /// </summary>
    void FireToClients();

    /// <summary>
    ///     Dispose event <br />
    ///     <remarks>Throws exception if called on non-custom created event</remarks>
    ///     <br />
    ///     <remarks>Will crash immediately if called on already fired event</remarks>
    /// </summary>
    void Dispose();

    /// <summary>
    ///     Event name
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Whether event can be modified or fired
    /// </summary>
    bool Editable { get; }

    /// <summary>
    ///     Set value using indexer
    /// </summary>
    object this[string key] { set; }

    /// <summary>
    ///     Get event value and convert to enum
    /// </summary>
    T Get<T>(string key, int defaultValue = 0) where T : Enum;
}
