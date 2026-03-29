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
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared.CStrike;

public interface ISchemaObject : INativeObject
{
    /// <summary>
    ///     Gets the Schema DynamicBinding
    /// </summary>
    string GetSchemaClassname();

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    bool GetNetVar<T>(string fieldName, ushort extraOffset = 0, bool? _ = null)
        where T : IComparable<bool>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    byte GetNetVar<T>(string fieldName, ushort extraOffset = 0, byte? _ = null)
        where T : IComparable<byte>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    short GetNetVar<T>(string fieldName, ushort extraOffset = 0, short? _ = null)
        where T : IComparable<short>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ushort GetNetVar<T>(string fieldName, ushort extraOffset = 0, ushort? _ = null)
        where T : IComparable<ushort>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    int GetNetVar<T>(string fieldName, ushort extraOffset = 0, int? _ = null)
        where T : IComparable<int>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    uint GetNetVar<T>(string fieldName, ushort extraOffset = 0, uint? _ = null)
        where T : IComparable<uint>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    long GetNetVar<T>(string fieldName, ushort extraOffset = 0, long? _ = null)
        where T : IComparable<long>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ulong GetNetVar<T>(string fieldName, ushort extraOffset = 0, ulong? _ = null)
        where T : IComparable<ulong>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    float GetNetVar<T>(string fieldName, ushort extraOffset = 0, float? _ = null)
        where T : IComparable<float>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    nint GetNetVar<T>(string fieldName, ushort extraOffset = 0, nint? _ = null)
        where T : IComparable<nint>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    string GetNetVar<T>(string fieldName, ushort extraOffset = 0, string? _ = null)
        where T : IComparable<string>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    Vector GetNetVar<T>(string fieldName, ushort extraOffset = 0, Vector? _ = null)
        where T : IComparable<Vector>;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    string GetNetVarUtlSymbolLarge(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    string GetNetVarUtlString(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ref CUtlString GetNetVarUtlStringRef(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ISchemaArray<T> GetSchemaFixedArray<T>(string fieldName, ushort extraOffset = 0)
        where T : unmanaged;

    /// <summary>
    ///     Gets the value of a Schema member variable
    /// </summary>
    ISchemaList<T> GetSchemaList<T>(string fieldName, bool isStruct = false, ushort extraOffset = 0)
        where T : unmanaged;

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, bool value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, byte value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, short value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, ushort value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, int value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, uint value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, long value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, ulong value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, float value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, string value, int maxLen, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, Vector value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetworkStateChanged</b> will not be called automatically
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlSymbolLarge(string field, string value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetworkStateChanged" />
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlString(string field, string value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     Checks if a Schema member variable exists
    ///     <remarks>This is usually a good helper to determine what type of entity this is</remarks>
    /// </summary>
    bool FindNetVar(string field);

    /// <summary>
    ///     Gets the offset of a Schema member variable
    /// </summary>
    /// <exception cref="ArgumentException">Throws an exception if the member does not exist</exception>
    int GetNetVarOffset(string field);

    /// <summary>
    ///     Automatically calls <b>NetworkStateChanged</b> or <b>StateChanged</b>
    /// </summary>
    void NetworkStateChanged(string field, bool isStruct = false, ushort extraOffset = 0);
}
