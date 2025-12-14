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
using Sharp.Shared.CStrike;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace Sharp.Shared.Managers;

/// <summary>
///     Low-level interface for interacting with Native Schema.<br />
///     Used for handling special Schema Classes not covered by the API.<br />
///     This interface directly accesses underlying data.<br />
///     Do not use these methods unless you know exactly what you're doing!
/// </summary>
public interface ISchemaManager
{
    /// <summary>
    ///     Gets a Schema field
    ///     <exception cref="ArgumentException">Thrown if <paramref name="classname" /> or <paramref name="field" /> does not exist</exception>
    /// </summary>
    SchemaField GetSchemaField(string classname, string field);

    /// <summary>
    ///     Gets the offset of a Schema member variable
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if <paramref name="classname" /> or <paramref name="field" /> does not exist</exception>
    int GetNetVarOffset(string classname, string field);

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    bool GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, bool? _ = null)
        where T : IComparable<bool>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    byte GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, byte? _ = null)
        where T : IComparable<byte>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    short GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, short? _ = null)
        where T : IComparable<short>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ushort GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, ushort? _ = null)
        where T : IComparable<ushort>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    int GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, int? _ = null)
        where T : IComparable<int>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    uint GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, uint? _ = null)
        where T : IComparable<uint>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    long GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, long? _ = null)
        where T : IComparable<long>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ulong GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, ulong? _ = null)
        where T : IComparable<ulong>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    float GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, float? _ = null)
        where T : IComparable<float>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    nint GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, nint? _ = null)
        where T : IComparable<nint>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    string GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, string? _ = null)
        where T : IComparable<string>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    Vector GetNetVar<T>(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0, Vector? _ = null)
        where T : IComparable<Vector>;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    string GetNetVarUtlSymbolLarge(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(INativeObject nativeObject,
        string                                                   classname,
        string                                                   field,
        ushort                                                   extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    string GetNetVarUtlString(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ref CUtlString GetNetVarUtlStringRef(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0);

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="pointer" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ISchemaArray<T> GetSchemaFixedArray<T>(nint pointer, string classname, string field, ISchemaObject chain)
        where T : unmanaged;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="pointer" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ISchemaArray<T> GetSchemaFixedArray<T>(nint pointer, SchemaField field, ISchemaObject chain)
        where T : unmanaged;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="pointer" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ISchemaList<T> GetSchemaList<T>(nint pointer, string classname, string field, ISchemaObject chain, bool isStruct)
        where T : unmanaged;

    /// <summary>
    ///     Gets the value of a Schema member variable <br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="pointer" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    ISchemaList<T> GetSchemaList<T>(nint pointer, SchemaField field, ISchemaObject chain, bool isStruct)
        where T : unmanaged;

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        bool                     value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        byte                     value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        short                    value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        ushort                   value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        int                      value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        uint                     value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        long                     value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        ulong                    value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        float                    value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        string                   value,
        int                      maxLen,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVar(INativeObject nativeObject,
        string                   classname,
        string                   field,
        Vector                   value,
        bool                     isStruct    = false,
        ushort                   extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlSymbolLarge(INativeObject nativeObject,
        string                                 classname,
        string                                 field,
        string                                 value,
        bool                                   isStruct    = false,
        ushort                                 extraOffset = 0);

    /// <summary>
    ///     Sets the value of a Schema member <br />
    ///     <remarks>
    ///         No need to explicitly call <seealso cref="NetVarStateChanged" /><br />
    ///         When <paramref name="isStruct" /> is <c>true</c>, <b>NetVarStateChanged</b> will not be called automatically<br /><br />
    ///         This method does not validate whether the provided <paramref name="ptr" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlString(INativeObject ptr,
        string                            classname,
        string                            field,
        string                            value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0);

    /// <summary>
    ///     Automatically calls <b>NetworkStateChanged</b> or <b>StateChanged</b><br />
    ///     <remarks>
    ///         This method does not validate whether the provided <paramref name="nativeObject" /> is a valid Schema instance<br />
    ///         Passing an invalid instance may cause crashes
    ///     </remarks>
    /// </summary>
    void NetVarStateChanged(
        INativeObject    nativeObject,
        SchemaClass      schemaClass,
        SchemaClassField schemaField,
        ushort           extraOffset = 0,
        bool             isStruct    = false
    );
}
