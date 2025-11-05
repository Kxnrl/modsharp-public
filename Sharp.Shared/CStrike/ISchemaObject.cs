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
    ///     获取Schema DynamicBinding
    /// </summary>
    string GetSchemaClassname();

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    bool GetNetVar<T>(string fieldName, ushort extraOffset = 0, bool? _ = null)
        where T : IComparable<bool>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    byte GetNetVar<T>(string fieldName, ushort extraOffset = 0, byte? _ = null)
        where T : IComparable<byte>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    short GetNetVar<T>(string fieldName, ushort extraOffset = 0, short? _ = null)
        where T : IComparable<short>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    ushort GetNetVar<T>(string fieldName, ushort extraOffset = 0, ushort? _ = null)
        where T : IComparable<ushort>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    int GetNetVar<T>(string fieldName, ushort extraOffset = 0, int? _ = null)
        where T : IComparable<int>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    uint GetNetVar<T>(string fieldName, ushort extraOffset = 0, uint? _ = null)
        where T : IComparable<uint>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    long GetNetVar<T>(string fieldName, ushort extraOffset = 0, long? _ = null)
        where T : IComparable<long>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    ulong GetNetVar<T>(string fieldName, ushort extraOffset = 0, ulong? _ = null)
        where T : IComparable<ulong>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    float GetNetVar<T>(string fieldName, ushort extraOffset = 0, float? _ = null)
        where T : IComparable<float>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    nint GetNetVar<T>(string fieldName, ushort extraOffset = 0, nint? _ = null)
        where T : IComparable<nint>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    string GetNetVar<T>(string fieldName, ushort extraOffset = 0, string? _ = null)
        where T : IComparable<string>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    Vector GetNetVar<T>(string fieldName, ushort extraOffset = 0, Vector? _ = null)
        where T : IComparable<Vector>;

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    string GetNetVarUtlSymbolLarge(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     获取Schema成员变量的值
    /// </summary>
    string GetNetVarUtlString(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     获取Schema成员变量的值 <br />
    /// </summary>
    ref CUtlString GetNetVarUtlStringRef(string fieldName, ushort extraOffset = 0);

    /// <summary>
    ///     获取Schema成员变量的值 <br />
    /// </summary>
    ISchemaArray<T> GetSchemaFixedArray<T>(string fieldName, ushort extraOffset = 0)
        where T : unmanaged;

    /// <summary>
    ///     获取Schema成员变量的值 <br />
    /// </summary>
    ISchemaList<T> GetSchemaList<T>(string fieldName, bool isStruct = false, ushort extraOffset = 0)
        where T : unmanaged;

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, bool value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, byte value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, short value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, ushort value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, int value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, uint value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, long value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, ulong value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, float value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, string value, int maxLen, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVar(string field, Vector value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///         <paramref name="isStruct" />为<c>true</c>时不会主动调用<b>NetworkStateChanged</b>
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlSymbolLarge(string field, string value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     设置Schema成员的值 <br />
    ///     <remarks>
    ///         不需要在显式调用<seealso cref="NetworkStateChanged" /><br />
    ///     </remarks>
    /// </summary>
    void SetNetVarUtlString(string field, string value, bool isStruct = false, ushort extraOffset = 0);

    /// <summary>
    ///     检查Schema成员变量是否存在<br />
    ///     <remarks>这通常是帮助你确定这个实体是什么东西的好帮手</remarks>
    /// </summary>
    bool FindNetVar(string field);

    /// <summary>
    ///     获取Schema成员变量的偏移
    /// </summary>
    /// <exception cref="ArgumentException">如果不存在成员则抛出异常</exception>
    int GetNetVarOffset(string field);

    /// <summary>
    ///     自动调用<b>NetworkStateChanged</b>或<b>StateChanged</b>
    /// </summary>
    void NetworkStateChanged(string field, bool isStruct = false, ushort extraOffset = 0);
}
