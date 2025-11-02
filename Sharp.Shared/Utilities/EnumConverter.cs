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
using System.Linq.Expressions;

namespace Sharp.Shared.Utilities;

public static class EnumConverter<T>
{
    private static readonly Func<int, T> Converter32;

    private static readonly Func<long, T> Converter64;

    static EnumConverter()
    {
        var parameter32  = Expression.Parameter(typeof(int));
        var conversion32 = Expression.Convert(parameter32, typeof(T));
        Converter32 = Expression.Lambda<Func<int, T>>(conversion32, parameter32).Compile();

        var parameter64  = Expression.Parameter(typeof(long));
        var conversion64 = Expression.Convert(parameter64, typeof(T));
        Converter64 = Expression.Lambda<Func<long, T>>(conversion64, parameter64).Compile();
    }

    public static T Convert(int value)
        => Converter32(value);

    public static T Convert(long value)
        => Converter64(value);
}
