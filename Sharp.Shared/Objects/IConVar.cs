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
    /// <exception cref="NotSupportedException">The type is not bool, integer, floating point, string or color</exception>
    /// <exception cref="InvalidOperationException">The ConVar has no default value</exception>
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

    /// <exception cref="InvalidOperationException">The type is String</exception>
    /// <exception cref="NotSupportedException">
    ///     The type is Color or a vector type (Vector2 / Vector3 / Vector4 / QAngle / VectorWS)
    /// </exception>
    void Set(int value);

    /// <exception cref="InvalidOperationException">The type is String</exception>
    /// <exception cref="NotSupportedException">
    ///     The type is Color or a vector type (Vector2 / Vector3 / Vector4 / QAngle / VectorWS)
    /// </exception>
    void Set(bool value);

    /// <summary>
    ///     long / uint / ulong arguments also bind to this overload; use <see cref="Set(int)" /> for integers within
    ///     the int range and <see cref="Set(string)" /> for integers outside it
    /// </summary>
    /// <exception cref="InvalidOperationException">The type is not Float32 / Float64</exception>
    void Set(float value);

    /// <exception cref="ArgumentException">The string cannot be parsed as the ConVar's type</exception>
    void Set(string value);

    /// <exception cref="InvalidOperationException">The type is String</exception>
    /// <exception cref="NotSupportedException">
    ///     The type is Float64 or a vector type (Vector2 / Vector3 / Vector4 / QAngle / VectorWS),
    ///     or the value cannot be written to this type
    /// </exception>
    void Set(ConVarVariantValue value);

    /// <summary>
    ///     Sets the minimum bound of this ConVar.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if updated; <see langword="false" /> if the ConVar does not already have a minimum bound or its type is not <see cref="ConVarType.Float32" /> or an integer type.
    /// </returns>
    bool SetMinBound(ConVarVariantValue value);

    /// <summary>
    ///     Sets the maximum bound of this ConVar.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if updated; <see langword="false" /> if the ConVar does not already have a maximum bound or its type is not <see cref="ConVarType.Float32" /> or an integer type.
    /// </returns>
    bool SetMaxBound(ConVarVariantValue value);

    /// <summary>
    ///     Universal method to set the ConVar value by parsing a string with permissive fallbacks.
    ///     <para>
    ///         Use this if you do not know the underlying type of the ConVar.
    ///         It attempts to parse the string and set the ConVar to the corresponding typed value.
    ///     </para>
    ///     <para>
    ///         Invalid numeric input falls back to zero. Invalid color or vector input uses the type's fallback:
    ///         color "0 0 0 255", or a zero vector with the appropriate number of components.
    ///         Boolean input is true only for "1" or "true" (case-insensitive); all other input is false.
    ///         This does not restore <see cref="DefaultValue" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     For <see cref="ConVarType.String" />, values longer than 255 UTF-8 bytes are truncated (use <see cref="Set(string)" /> for unrestricted length).
    ///     For <see cref="ConVarType.Float64" />, parsed values are narrowed to <see cref="float" />.
    /// </remarks>
    /// <param name="value">The string value to parse and set.</param>
    void SetString(string value);

    /// <summary>
    ///     Returns the ConVar value as a string.
    /// </summary>
    /// <returns>The string representation of the current value.</returns>
    /// <exception cref="NotSupportedException">The type is not bool, integer, floating point, string or color</exception>
    string GetString();

    /// <summary>
    ///     Sends the specified value to the client without changing the server-side ConVar.
    /// </summary>
    /// <param name="client">The target client.</param>
    /// <param name="value">The value to replicate.</param>
    void ReplicateToClient(IGameClient client, string value);
}
