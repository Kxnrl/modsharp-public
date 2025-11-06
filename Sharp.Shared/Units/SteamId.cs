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

using System.Runtime.CompilerServices;
using Sharp.Shared.Enums;
using UnitGenerator;

namespace Sharp.Shared.Units;

// ReSharper disable once InconsistentNaming
[UnitOf<ulong>(UnitGenerateOptions.ImplicitOperator
               | UnitGenerateOptions.ParseMethod
               | UnitGenerateOptions.Comparable
               | UnitGenerateOptions.JsonConverter
               | UnitGenerateOptions.JsonConverterDictionaryKeySupport)]
public partial struct SteamID
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidUserId()
        => AccountType is SteamAccountType.Individual && value >= 76561197960265728;

    public uint AccountId => (uint) unchecked(value & 0xFFFFFFFF);

    public uint AccountInstance => (uint) unchecked((value >> 32) & 0x000FFFFF);

    public SteamAccountType AccountType => (SteamAccountType) unchecked((value >> 52) & 0xF);

    public SteamUniverse Universe => (SteamUniverse) unchecked((value >> 56) & 0xFF);

    public string DestructureTransform()
        => $"{{ \"Id\": {value}, \"Type\": {AccountType}, \"Universe\": {Universe} }}";
}
