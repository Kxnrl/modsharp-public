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
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Shared;

public interface IClientPreference
{
    const string Identity = nameof(IClientPreference);

    /// <summary>
    ///     Subscribe to client cookie load completion. Dispose the returned handle on module unload or when unsubscribing.
    /// </summary>
    /// <remarks>
    ///     Invoked on the game main thread after <c>OnClientPostAdminCheck</c> and async storage loading completes.
    ///     Does not replay for clients whose cookies are already loaded.
    ///     Dirty cookies are persisted asynchronously on <c>OnClientDisconnected</c>.
    /// </remarks>
    /// <param name="callback">Callback invoked with the loaded client.</param>
    /// <returns>A disposable subscription token.</returns>
    IDisposable ListenOnLoad(Action<IGameClient> callback);

    /// <summary>
    ///     Checks whether cookies have finished loading for the specified client.
    /// </summary>
    /// <param name="identity">A valid user SteamID (<c>identity.IsValidUserId()</c> must be <see langword="true" />).</param>
    /// <returns><see langword="true" /> if the client's cookies are loaded; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    bool IsLoaded(SteamID identity);

    /// <summary>
    ///     Gets a cookie by key for the specified client, or <see langword="null" /> if the client is not loaded or the key does not exist.
    /// </summary>
    /// <param name="identity">A valid user SteamID.</param>
    /// <param name="key">Cookie key.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    ICookieItem? GetCookie(SteamID identity, string key);

    /// <summary>
    ///     Deletes a cookie by key for the specified client. Returns <see langword="false" /> if the client is not loaded or the key does not exist.
    /// </summary>
    /// <param name="identity">A valid user SteamID.</param>
    /// <param name="key">Cookie key.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    bool DeleteCookie(SteamID identity, string key);

    /// <summary>
    ///     Sets a boolean cookie (stored as <see cref="CookieValueType.Number" /> with value <c>1</c> or <c>0</c>; read back via <c>GetNumber() != 0</c>).
    /// </summary>
    /// <param name="identity">A valid user SteamID whose cookies are already loaded.</param>
    /// <param name="key">Cookie key.</param>
    /// <param name="value">Boolean value to store.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    /// <exception cref="InvalidOperationException">Cookies for <paramref name="identity" /> are not loaded yet.</exception>
    ICookieItem SetCookie(SteamID identity, string key, bool value);

    /// <summary>
    ///     Sets an integer cookie (<see cref="CookieValueType.Number" />).
    /// </summary>
    /// <param name="identity">A valid user SteamID whose cookies are already loaded.</param>
    /// <param name="key">Cookie key.</param>
    /// <param name="value">64-bit integer value to store.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    /// <exception cref="InvalidOperationException">Cookies for <paramref name="identity" /> are not loaded yet.</exception>
    ICookieItem SetCookie(SteamID identity, string key, long value);

    /// <summary>
    ///     Sets a floating-point cookie (<see cref="CookieValueType.Double" />). Non-finite values (<c>NaN</c>, <c>Infinity</c>) are stored as <c>0</c>.
    /// </summary>
    /// <param name="identity">A valid user SteamID whose cookies are already loaded.</param>
    /// <param name="key">Cookie key.</param>
    /// <param name="value">Double-precision floating-point value to store.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    /// <exception cref="InvalidOperationException">Cookies for <paramref name="identity" /> are not loaded yet.</exception>
    ICookieItem SetCookie(SteamID identity, string key, double value);

    /// <summary>
    ///     Sets a string cookie (<see cref="CookieValueType.String" />).
    /// </summary>
    /// <param name="identity">A valid user SteamID whose cookies are already loaded.</param>
    /// <param name="key">Cookie key.</param>
    /// <param name="value">Non-null string value to store.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Cookies for <paramref name="identity" /> are not loaded yet.</exception>
    ICookieItem SetCookie(SteamID identity, string key, string value);

    /// <summary>
    ///     Sets a serializable object cookie (stored as <see cref="CookieValueType.String" /> via <see cref="ISerializableCookieItem{T}.Serialize" />).
    /// </summary>
    /// <param name="identity">A valid user SteamID whose cookies are already loaded.</param>
    /// <param name="key">Cookie key.</param>
    /// <param name="value">Serializable value to store.</param>
    /// <exception cref="ArgumentException"><paramref name="identity" /> is not a valid user SteamID.</exception>
    /// <exception cref="InvalidOperationException">Cookies for <paramref name="identity" /> are not loaded yet.</exception>
    ICookieItem SetCookie<T>(SteamID identity, string key, T value) where T : ISerializableCookieItem<T>;
}
