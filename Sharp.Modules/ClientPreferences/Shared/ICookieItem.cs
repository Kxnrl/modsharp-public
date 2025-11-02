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

namespace Sharp.Modules.ClientPreferences.Shared;

public interface ICookieItem
{
    /// <summary>
    ///     类型
    /// </summary>
    CookieValueType Type { get; }

    /// <summary>
    ///     取整数
    /// </summary>
    /// <exception cref="InvalidOperationException">类型不匹配</exception>
    long GetNumber();

    /// <summary>
    ///     取浮点数
    /// </summary>
    /// <exception cref="InvalidOperationException">类型不匹配</exception>
    double GetDouble();

    /// <summary>
    ///     取字符串
    /// </summary>
    /// <exception cref="InvalidOperationException">类型不匹配</exception>
    string GetString();

    /// <summary>
    ///     取整数并转换类型为你定义的枚举
    /// </summary>
    /// <typeparam name="T">自定义Enum</typeparam>
    /// <exception cref="InvalidOperationException">类型不匹配</exception>
    T Get<T>() where T : Enum;

    /// <summary>
    ///     取字符串并反序列化为你定义的类型
    /// </summary>
    /// <typeparam name="T">自定义Object</typeparam>
    /// <exception cref="InvalidOperationException">类型不匹配</exception>
    T GetObject<T>() where T : ISerializableCookieItem<T>;
}
