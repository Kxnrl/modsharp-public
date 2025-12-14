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
    ///     Parse string and set ConVar to corresponding type value <br />
    ///     <remarks>Sets to default value if parsing fails</remarks>
    /// </summary>
    /// <param name="value"></param>
    void SetString(string value);

    /// <summary>
    ///     Convert ConVar value to string representation <br />
    /// </summary>
    /// <returns></returns>
    string GetString();

    /// <summary>
    ///     Replicate value to client without modifying the ConVar
    /// </summary>
    void ReplicateToClient(IGameClient client, string value);
}
