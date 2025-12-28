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
using Sharp.Shared.Types;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Sharp.Shared.Objects;

public interface IConVar : INativeObject
{
    /// <summary>
    ///     ConVar name
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Default value
    /// </summary>
    string DefaultValue { get; }

    /// <summary>
    ///     Help description text
    /// </summary>
    string HelpString { get; }

    /// <summary>
    ///     ConVar flags
    /// </summary>
    ConVarFlags Flags { get; set; }

    /// <summary>
    ///     ConVar value type
    /// </summary>
    ConVarType Type { get; }

    bool GetBool();

    short GetInt16();

    ushort GetUInt16();

    int GetInt32();

    uint GetUInt32();

    long GetInt64();

    ulong GetUInt64();

    float GetFloat();

    double GetDouble();

    ref ConVarVariantValue Get();

    void Set(int value);

    void Set(bool value);

    void Set(float value);

    void Set(string value);

    void Set(ConVarVariantValue value);

    bool SetMinBound(ConVarVariantValue value);

    bool SetMaxBound(ConVarVariantValue value);

    /// <summary>
    ///     Universal method to set the ConVar value by parsing a string.
    ///     <para>
    ///         Use this if you do not know the underlying type of the ConVar.
    ///         It attempts to parse the string and set the ConVar to the corresponding typed value.
    ///     </para>
    ///     <para>
    ///         Falls back to the default value if parsing fails.
    ///     </para>
    /// </summary>
    /// <param name="value">The string value to parse and set.</param>
    void SetString(string value);

    /// <summary>
    ///     Returns the ConVar value as a string.
    /// </summary>
    /// <returns>The string representation of the current value.</returns>
    string GetString();

    /// <summary>
    ///     Sends the specified value to the client without changing the server-side ConVar.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="value">The value to replicate.</param>
    void ReplicateToClient(IGameClient client, string value);
}
