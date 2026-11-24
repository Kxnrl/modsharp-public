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

namespace Sharp.Modules.ClientPreferences.Shared;

public interface ICookieItem
{
    /// <summary>
    ///     Value type of this cookie.
    /// </summary>
    CookieValueType Type { get; }

    /// <summary>
    ///     Gets the stored 64-bit integer value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Cookie <see cref="Type" /> is not <see cref="CookieValueType.Number" />.</exception>
    long GetNumber();

    /// <summary>
    ///     Gets the stored double-precision floating-point value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Cookie <see cref="Type" /> is not <see cref="CookieValueType.Double" />.</exception>
    double GetDouble();

    /// <summary>
    ///     Gets the stored string value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Cookie <see cref="Type" /> is not <see cref="CookieValueType.String" />.</exception>
    string GetString();

    /// <summary>
    ///     Gets the stored integer value converted to enum <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">Target enum type.</typeparam>
    /// <exception cref="InvalidOperationException">Cookie <see cref="Type" /> is not <see cref="CookieValueType.Number" />.</exception>
    T Get<T>() where T : Enum;

    /// <summary>
    ///     Gets the stored string value and deserializes it into <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">Custom <see cref="ISerializableCookieItem{T}" /> type.</typeparam>
    /// <exception cref="InvalidOperationException">Cookie <see cref="Type" /> is not <see cref="CookieValueType.String" />.</exception>
    T GetObject<T>() where T : ISerializableCookieItem<T>;
}
