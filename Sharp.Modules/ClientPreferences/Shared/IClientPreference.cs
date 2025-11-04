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
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Sharp.Modules.ClientPreferences.Shared;

public interface IClientPreference
{
    const string Identity = nameof(IClientPreference);

    /// <summary>
    ///     监听加载事件, 需要你在Unload或取消监听时进行Dispose
    /// </summary>
    IDisposable ListenOnLoad(Action<IGameClient> callback);

    /// <summary>
    ///     检查是否已经加载数据
    /// </summary>
    bool IsLoaded(SteamID identity);

    /// <summary>
    ///     获取Cookie
    /// </summary>
    ICookieItem? GetCookie(SteamID identity, string key);

    /// <summary>
    ///     删除Cookie
    /// </summary>
    bool DeleteCookie(SteamID identity, string key);

    /// <summary>
    ///     设置Cookie
    /// </summary>
    ICookieItem SetCookie(SteamID identity, string key, bool value);

    /// <summary>
    ///     设置Cookie
    /// </summary>
    ICookieItem SetCookie(SteamID identity, string key, long value);

    /// <summary>
    ///     设置Cookie
    /// </summary>
    ICookieItem SetCookie(SteamID identity, string key, double value);

    /// <summary>
    ///     设置Cookie
    /// </summary>
    ICookieItem SetCookie(SteamID identity, string key, string value);

    /// <summary>
    ///     设置 Cookie
    /// </summary>
    ICookieItem SetCookie<T>(SteamID identity, string key, T value) where T : ISerializableCookieItem<T>;
}
