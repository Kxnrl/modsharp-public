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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Core.Helpers;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Utilities;

namespace Sharp.Core.CStrike;

internal abstract class SchemaObject : NativeObject, ISchemaObject
{
    protected string? SchemaClassname;

    private static readonly Dictionary<string, Dictionary<string, (SchemaClass, SchemaClassField)>> ClassFieldCache
        = new (StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Dictionary<string, SchemaField>> SchemaFieldCache
        = new (StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, (SchemaClass, SchemaClassField)>? _resolveMap;
    private Dictionary<string, SchemaField>?                     _fieldMap;

    protected SchemaObject(nint ptr) : base(ptr)
    {
    }

    public abstract string GetSchemaClassname();

#region NetworkStateChanged

    private SchemaObject? _networkParent;
    private int           _networkOffsetInParent;
    private int?          _chainEntityOffset;

    protected virtual bool IsNetworkRoot => false;

    private int ChainEntityOffset => _chainEntityOffset ??= SchemaSystem.GetChainOffset(GetSchemaClassname());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindNetworkParent(SchemaObject parent, int offsetInParent)
    {
        _networkParent         = parent;
        _networkOffsetInParent = offsetInParent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BindEmbeddedPointer(SchemaObject? child)
    {
        if (child is null)
        {
            return;
        }

        var delta = child.GetAbsPtr() - _this;

        if (delta > 0)
        {
            child.BindNetworkParent(this, (int) delta);
        }
    }

    internal void SchemaStateChanged(SchemaField field, bool isStruct, int extraOffset = 0)
    {
        if (!field.Networked)
        {
            return;
        }

        SchemaStateChanged(field.Offset + extraOffset, field.ChainOffset, isStruct);
    }

    internal void SchemaStateChanged(int offset, int chainOffset, bool isStruct)
    {
        if (chainOffset > 0)
        {
            Bridges.Natives.Entity.NetworkStateChanged(_this.Add(chainOffset), (ushort) offset);

            return;
        }

        if (!isStruct)
        {
            Bridges.Natives.Entity.SetStateChanged(_this, (ushort) offset);

            return;
        }

        var node = this;

        while (node._networkParent is { } parent)
        {
            offset += node._networkOffsetInParent;
            node   =  parent;

            if (node.ChainEntityOffset > 0 || node.IsNetworkRoot)
            {
                Bridges.Natives.Entity.SetStructStateChanged(node.GetAbsPtr(),
                                                             node.ChainEntityOffset,
                                                             node.IsNetworkRoot,
                                                             (uint) offset);

                return;
            }
        }
    }

#endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Dictionary<string, (SchemaClass, SchemaClassField)> GetResolveMap()
    {
        if (_resolveMap is not null)
        {
            return _resolveMap;
        }

        var className = GetSchemaClassname();

        if (!ClassFieldCache.TryGetValue(className, out _resolveMap))
        {
            _resolveMap = new Dictionary<string, (SchemaClass, SchemaClassField)>(StringComparer.OrdinalIgnoreCase);
            ClassFieldCache[className] = _resolveMap;
        }

        return _resolveMap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Dictionary<string, SchemaField> GetSchemaFieldMap()
    {
        if (_fieldMap is not null)
        {
            return _fieldMap;
        }

        var className = GetSchemaClassname();

        if (!SchemaFieldCache.TryGetValue(className, out _fieldMap))
        {
            _fieldMap                   = new Dictionary<string, SchemaField>(StringComparer.OrdinalIgnoreCase);
            SchemaFieldCache[className] = _fieldMap;
        }

        return _fieldMap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (SchemaClass schemaClass, SchemaClassField schemaField) CachedResolve(string field)
    {
        var map = GetResolveMap();

        if (!map.TryGetValue(field, out var cached))
        {
            cached     = SchemaSystem.ResolveField(GetSchemaClassname(), field);
            map[field] = cached;
        }

        return cached;
    }

    public bool GetNetVar<T>(string fieldName, ushort extraOffset = 0, bool? _ = null)
        where T : IComparable<bool>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetByte(sf.Offset + extraOffset) != 0;
    }

    public byte GetNetVar<T>(string fieldName, ushort extraOffset = 0, byte? _ = null)
        where T : IComparable<byte>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetByte(sf.Offset + extraOffset);
    }

    public short GetNetVar<T>(string fieldName, ushort extraOffset = 0, short? _ = null)
        where T : IComparable<short>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetInt16(sf.Offset + extraOffset);
    }

    public ushort GetNetVar<T>(string fieldName, ushort extraOffset = 0, ushort? _ = null)
        where T : IComparable<ushort>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetUInt16(sf.Offset + extraOffset);
    }

    public int GetNetVar<T>(string fieldName, ushort extraOffset = 0, int? _ = null)
        where T : IComparable<int>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetInt32(sf.Offset + extraOffset);
    }

    public uint GetNetVar<T>(string fieldName, ushort extraOffset = 0, uint? _ = null)
        where T : IComparable<uint>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetUInt32(sf.Offset + extraOffset);
    }

    public long GetNetVar<T>(string fieldName, ushort extraOffset = 0, long? _ = null)
        where T : IComparable<long>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetInt64(sf.Offset + extraOffset);
    }

    public ulong GetNetVar<T>(string fieldName, ushort extraOffset = 0, ulong? _ = null)
        where T : IComparable<ulong>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetUInt64(sf.Offset + extraOffset);
    }

    public float GetNetVar<T>(string fieldName, ushort extraOffset = 0, float? _ = null)
        where T : IComparable<float>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetFloat(sf.Offset + extraOffset);
    }

    public nint GetNetVar<T>(string fieldName, ushort extraOffset = 0, nint? _ = null)
        where T : IComparable<nint>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.GetObjectPtr(sf.Offset + extraOffset);
    }

    public string GetNetVar<T>(string fieldName, ushort extraOffset = 0, string? _ = null)
        where T : IComparable<string>
    {
        var (_, sf) = CachedResolve(fieldName);

        return _this.ReadStringUtf8(sf.Offset + extraOffset);
    }

    public unsafe Vector GetNetVar<T>(string fieldName, ushort extraOffset = 0, Vector? _ = null)
        where T : IComparable<Vector>
    {
        var (_, sf) = CachedResolve(fieldName);

        return *(Vector*) (_this + sf.Offset + extraOffset);
    }

    public unsafe string GetNetVarUtlSymbolLarge(string fieldName, ushort extraOffset = 0)
    {
        var (_, sf) = CachedResolve(fieldName);
        var pointer = (CUtlSymbolLarge*) (_this + sf.Offset + extraOffset);

        return pointer->Get();
    }

    public unsafe ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(string fieldName, ushort extraOffset = 0)
    {
        var (_, sf) = CachedResolve(fieldName);
        var pointer = (CUtlSymbolLarge*) (_this + sf.Offset + extraOffset);

        return ref Unsafe.AsRef<CUtlSymbolLarge>(pointer);
    }

    public unsafe string GetNetVarUtlString(string fieldName, ushort extraOffset = 0)
    {
        var (_, sf) = CachedResolve(fieldName);
        var pointer = (CUtlString*) (_this + sf.Offset + extraOffset);

        return pointer->Get();
    }

    public unsafe ref CUtlString GetNetVarUtlStringRef(string fieldName, ushort extraOffset = 0)
    {
        var (_, sf) = CachedResolve(fieldName);
        var pointer = (CUtlString*) (_this + sf.Offset + extraOffset);

        return ref Unsafe.AsRef<CUtlString>(pointer);
    }

    private SchemaField CachedGetSchemaField(string fieldName)
    {
        var map = GetSchemaFieldMap();

        if (!map.TryGetValue(fieldName, out var cached))
        {
            cached         = SchemaSystem.GetSchemaField(GetSchemaClassname(), fieldName);
            map[fieldName] = cached;
        }

        return cached;
    }

    public ISchemaArray<T> GetSchemaFixedArray<T>(string fieldName, ushort extraOffset = 0, bool isStruct = false)
        where T : unmanaged
    {
        var field   = CachedGetSchemaField(fieldName);
        var pointer = nint.Add(_this, field.Offset + extraOffset);

        return SchemaFixedArray<T>.Create(pointer, field, this, isStruct)
               ?? throw new ArgumentNullException(nameof(pointer));
    }

    public ISchemaList<T> GetSchemaList<T>(string fieldName, bool isStruct = false, ushort extraOffset = 0) where T : unmanaged
    {
        var field   = CachedGetSchemaField(fieldName);
        var pointer = nint.Add(_this, field.Offset + extraOffset);

        return SchemaUnmanagedVector<T>.Create(pointer, field, this, isStruct)
               ?? throw new ArgumentNullException(nameof(pointer));
    }

    public void SetNetVar(string field, bool value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarBool(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, byte value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarByte(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, short value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarInt16(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, ushort value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarUInt16(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, int value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarInt32(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, uint value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarUInt32(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, long value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarInt64(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, ulong value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarUInt64(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, float value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarFloat(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, string value, int maxLen, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarString(_this, sc, sf, value, maxLen, isStruct, extraOffset, this);
    }

    public void SetNetVar(string field, Vector value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarVector(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVarUtlSymbolLarge(string field, string value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarUtlSymbolLarge(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public void SetNetVarUtlString(string field, string value, bool isStruct = false, ushort extraOffset = 0)
    {
        var (sc, sf) = CachedResolve(field);
        SchemaSystem.SetNetVarUtlString(_this, sc, sf, value, isStruct, extraOffset, this);
    }

    public bool FindNetVar(string field)
        => SchemaSystem.FindNetVar(GetSchemaClassname(), field);

    public int GetNetVarOffset(string field)
        => SchemaSystem.GetNetVarOffset(GetSchemaClassname(), field);

    public void NetworkStateChanged(string field, bool isStruct = false, ushort extraOffset = 0)
        => SchemaSystem.NetworkStateChanged(_this, GetSchemaClassname(), field, isStruct, extraOffset, this);
}
