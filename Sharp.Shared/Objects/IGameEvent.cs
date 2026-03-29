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
    ///     Fire the event.
    ///     <para>
    ///         If <paramref name="serverOnly"/> is <c>false</c>, the event will be broadcast to all clients.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The event object will be automatically disposed after firing the event.
    ///         Do not manually call <see cref="Dispose"/> if you use this method.
    ///     </para>
    ///     <para>
    ///         Throws an exception if called on a non-custom created event.
    ///     </para>
    /// </remarks>
    /// <param name="serverOnly">If set to <c>true</c>, the event is processed only on the server and not sent to clients.</param>
    void Fire(bool serverOnly);

    /// <summary>
    ///     Fire the event to a specific client identified by their slot index.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The event object will <b>NOT</b> be automatically disposed after calling this function.
    ///         You can cache the event object, modify its parameters, and re-fire it as needed.
    ///         Ensure you manually call <see cref="Dispose"/> when the event is no longer needed (e.g., on module unload).
    ///     </para>
    ///     <para>
    ///         Throws an exception if called on a non-custom created event.
    ///     </para>
    /// </remarks>
    /// <param name="slot">The client's slot index (0-based).</param>
    void FireToClient(int slot);

    /// <summary>
    ///     Fire the event to a specific client.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The event object will <b>NOT</b> be automatically disposed after calling this function.
    ///         You can cache the event object, modify its parameters, and re-fire it as needed.
    ///         Ensure you manually call <see cref="Dispose"/> when the event is no longer needed (e.g., on module unload).
    ///     </para>
    ///     <para>
    ///         Throws an exception if called on a non-custom created event.
    ///     </para>
    /// </remarks>
    /// <param name="client">The target <see cref="IGameClient"/>.</param>
    void FireToClient(IGameClient client);

    /// <summary>
    ///     Fire the event to all connected clients.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The event object will <b>NOT</b> be automatically disposed after calling this function.
    ///         You can cache the event object, modify its parameters, and re-fire it as needed.
    ///         Ensure you manually call <see cref="Dispose"/> when the event is no longer needed (e.g., on plugin unload).
    ///     </para>
    ///     <para>
    ///         Throws an exception if called on a non-custom created event.
    ///     </para>
    /// </remarks>
    void FireToClients();

    /// <summary>
    ///     Manually dispose of the event object.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This should only be called for custom-created events that were fired using
    ///         <see cref="FireToClient(int)"/>, <see cref="FireToClient(IGameClient)"/>, or <see cref="FireToClients"/>.
    ///     </para>
    ///     <para>
    ///         <b>WARNING:</b> Calling this method on an event that has already been disposed or fired via <see cref="Fire(bool)"/>
    ///         may cause the server to crash.
    ///     </para>
    ///     <para>
    ///         Throws an exception if called on a non-custom created event.
    ///     </para>
    /// </remarks>
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
