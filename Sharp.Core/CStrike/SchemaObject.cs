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
using Sharp.Core.Helpers;
using Sharp.Shared.CStrike;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace Sharp.Core.CStrike;

internal abstract class SchemaObject : NativeObject, ISchemaObject
{
    protected string? SchemaClassname;

    protected SchemaObject(nint ptr) : base(ptr)
    {
    }

    public abstract string GetSchemaClassname();

    public bool GetNetVar<T>(string fieldName, ushort extraOffset = 0, bool? _ = null)
        where T : IComparable<bool>
        => SchemaSystem.GetNetVarBool(_this, GetSchemaClassname(), fieldName, extraOffset);

    public byte GetNetVar<T>(string fieldName, ushort extraOffset = 0, byte? _ = null)
        where T : IComparable<byte>
        => SchemaSystem.GetNetVarByte(_this, GetSchemaClassname(), fieldName, extraOffset);

    public short GetNetVar<T>(string fieldName, ushort extraOffset = 0, short? _ = null)
        where T : IComparable<short>
        => SchemaSystem.GetNetVarInt16(_this, GetSchemaClassname(), fieldName, extraOffset);

    public ushort GetNetVar<T>(string fieldName, ushort extraOffset = 0, ushort? _ = null)
        where T : IComparable<ushort>
        => SchemaSystem.GetNetVarUInt16(_this, GetSchemaClassname(), fieldName, extraOffset);

    public int GetNetVar<T>(string fieldName, ushort extraOffset = 0, int? _ = null)
        where T : IComparable<int>
        => SchemaSystem.GetNetVarInt32(_this, GetSchemaClassname(), fieldName, extraOffset);

    public uint GetNetVar<T>(string fieldName, ushort extraOffset = 0, uint? _ = null)
        where T : IComparable<uint>
        => SchemaSystem.GetNetVarUInt32(_this, GetSchemaClassname(), fieldName, extraOffset);

    public long GetNetVar<T>(string fieldName, ushort extraOffset = 0, long? _ = null)
        where T : IComparable<long>
        => SchemaSystem.GetNetVarInt64(_this, GetSchemaClassname(), fieldName, extraOffset);

    public ulong GetNetVar<T>(string fieldName, ushort extraOffset = 0, ulong? _ = null)
        where T : IComparable<ulong>
        => SchemaSystem.GetNetVarUInt64(_this, GetSchemaClassname(), fieldName, extraOffset);

    public float GetNetVar<T>(string fieldName, ushort extraOffset = 0, float? _ = null)
        where T : IComparable<float>
        => SchemaSystem.GetNetVarFloat(_this, GetSchemaClassname(), fieldName, extraOffset);

    public nint GetNetVar<T>(string fieldName, ushort extraOffset = 0, nint? _ = null)
        where T : IComparable<nint>
        => SchemaSystem.GetNetVarPointer(_this, GetSchemaClassname(), fieldName, extraOffset);

    public string GetNetVar<T>(string fieldName, ushort extraOffset = 0, string? _ = null)
        where T : IComparable<string>
        => SchemaSystem.GetNetVarString(_this, GetSchemaClassname(), fieldName, extraOffset);

    public Vector GetNetVar<T>(string fieldName, ushort extraOffset = 0, Vector? _ = null)
        where T : IComparable<Vector>
        => SchemaSystem.GetNetVarVector(_this, GetSchemaClassname(), fieldName, extraOffset);

    public string GetNetVarUtlSymbolLarge(string fieldName, ushort extraOffset = 0)
        => SchemaSystem.GetNetVarUtlSymbolLarge(_this, GetSchemaClassname(), fieldName, extraOffset);

    public ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(string fieldName, ushort extraOffset = 0)
        => ref SchemaSystem.GetNetVarUtlSymbolLargeRef(_this, GetSchemaClassname(), fieldName, extraOffset);

    public string GetNetVarUtlString(string fieldName, ushort extraOffset = 0)
        => SchemaSystem.GetNetVarUtlString(_this, GetSchemaClassname(), fieldName, extraOffset);

    public ref CUtlString GetNetVarUtlStringRef(string fieldName, ushort extraOffset = 0)
        => ref SchemaSystem.GetNetVarUtlStringRef(_this, GetSchemaClassname(), fieldName, extraOffset);

    public ISchemaArray<T> GetSchemaFixedArray<T>(string fieldName, ushort extraOffset = 0) where T : unmanaged
    {
        var field   = SchemaSystem.GetSchemaField(GetSchemaClassname(), fieldName);
        var pointer = nint.Add(_this, field.Offset + extraOffset);

        return SchemaFixedArray<T>.Create(pointer, field, _this) ?? throw new ArgumentNullException(nameof(pointer));
    }

    public ISchemaList<T> GetSchemaList<T>(string fieldName, bool isStruct = false, ushort extraOffset = 0) where T : unmanaged
    {
        var field   = SchemaSystem.GetSchemaField(GetSchemaClassname(), fieldName);
        var pointer = nint.Add(_this, field.Offset + extraOffset);

        return SchemaUnmanagedVector<T>.Create(pointer, field, _this, isStruct)
               ?? throw new ArgumentNullException(nameof(pointer));
    }

    public void SetNetVar(string field, bool value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarBool(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, byte value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarByte(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, short value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarInt16(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, ushort value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarUInt16(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, int value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarInt32(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, uint value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarUInt32(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, long value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarInt64(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, ulong value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarUInt64(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, float value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarFloat(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVar(string field, string value, int maxLen, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarString(_this, GetSchemaClassname(), field, value, maxLen, isStruct, extraOffset);

    public void SetNetVar(string field, Vector value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarVector(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVarUtlSymbolLarge(string field, string value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarUtlSymbolLarge(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public void SetNetVarUtlString(string field, string value, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.SetNetVarUtlString(_this, GetSchemaClassname(), field, value, isStruct, extraOffset);

    public bool FindNetVar(string field)
        => SchemaSystem.FindNetVar(GetSchemaClassname(), field);

    public int GetNetVarOffset(string field)
        => SchemaSystem.GetNetVarOffset(GetSchemaClassname(), field);

    public void NetworkStateChanged(string field, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.NetworkStateChanged(_this, GetSchemaClassname(), field, isStruct, extraOffset);
}
