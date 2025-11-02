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
using Sharp.Modules.ClientPreferences.Shared;
using Sharp.Shared.Utilities;

namespace Sharp.Modules.ClientPreferences.Core;

internal class CookieItem : ICookieItem
{
    public CookieValueType Type { get; }

    private readonly long?   _numberValue;
    private readonly double? _doubleValue;
    private readonly string? _stringValue;

    public CookieItem(long value)
    {
        Type         = CookieValueType.Number;
        _numberValue = value;
    }

    public CookieItem(double value)
    {
        // IsFinite 可以判断非Nan和非Infinity
        if (!double.IsFinite(value))
        {
            value = 0;
        }

        Type         = CookieValueType.Double;
        _doubleValue = value;
    }

    public CookieItem(string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Type         = CookieValueType.String;
        _stringValue = value;
    }

    public long GetNumber()
    {
        if (Type is not CookieValueType.Number)
        {
            throw new InvalidOperationException("Cookie type mismatch");
        }

        return _numberValue.GetValueOrDefault();
    }

    public double GetDouble()
    {
        if (Type is not CookieValueType.Double)
        {
            throw new InvalidOperationException("Cookie type mismatch");
        }

        return _doubleValue.GetValueOrDefault();
    }

    public string GetString()
    {
        if (Type is not CookieValueType.String)
        {
            throw new InvalidOperationException("Cookie type mismatch");
        }

        return _stringValue ?? string.Empty;
    }

    public T Get<T>() where T : Enum
    {
        var value = GetNumber();

        return EnumConverter<T>.Convert(value);
    }

    public T GetObject<T>() where T : ISerializableCookieItem<T>
    {
        var value = GetString();

        return T.Deserialize(value);
    }
}
