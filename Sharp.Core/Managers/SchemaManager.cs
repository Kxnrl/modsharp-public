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
using Sharp.Core.CStrike;
using Sharp.Core.Helpers;
using Sharp.Shared;
using Sharp.Shared.CStrike;
using Sharp.Shared.Managers;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;

namespace Sharp.Core.Managers;

internal interface ICoreSchemaManager : ISchemaManager;

internal class SchemaManager : ICoreSchemaManager
{
    public SchemaField GetSchemaField(string classname, string field)
        => SchemaSystem.GetSchemaField(classname, field);

    public int GetNetVarOffset(string classname, string field)
        => SchemaSystem.GetNetVarOffset(classname, field);

    public bool GetNetVar<T>(INativeObject nativeObject,
        string                             classname,
        string                             field,
        ushort                             extraOffset = 0,
        bool?                              _           = null)
        where T : IComparable<bool>
        => SchemaSystem.GetNetVarBool(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public byte GetNetVar<T>(INativeObject nativeObject,
        string                             classname,
        string                             field,
        ushort                             extraOffset = 0,
        byte?                              _           = null)
        where T : IComparable<byte>
        => SchemaSystem.GetNetVarByte(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public short GetNetVar<T>(INativeObject nativeObject,
        string                              classname,
        string                              field,
        ushort                              extraOffset = 0,
        short?                              _           = null)
        where T : IComparable<short>
        => SchemaSystem.GetNetVarInt16(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public ushort GetNetVar<T>(INativeObject nativeObject,
        string                               classname,
        string                               field,
        ushort                               extraOffset = 0,
        ushort?                              _           = null)
        where T : IComparable<ushort>
        => SchemaSystem.GetNetVarUInt16(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public int GetNetVar<T>(INativeObject nativeObject,
        string                            classname,
        string                            field,
        ushort                            extraOffset = 0,
        int?                              _           = null)
        where T : IComparable<int>
        => SchemaSystem.GetNetVarInt32(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public uint GetNetVar<T>(INativeObject nativeObject,
        string                             classname,
        string                             field,
        ushort                             extraOffset = 0,
        uint?                              _           = null)
        where T : IComparable<uint>
        => SchemaSystem.GetNetVarUInt32(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public long GetNetVar<T>(INativeObject nativeObject,
        string                             classname,
        string                             field,
        ushort                             extraOffset = 0,
        long?                              _           = null)
        where T : IComparable<long>
        => SchemaSystem.GetNetVarInt64(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public ulong GetNetVar<T>(INativeObject nativeObject,
        string                              classname,
        string                              field,
        ushort                              extraOffset = 0,
        ulong?                              _           = null)
        where T : IComparable<ulong>
        => SchemaSystem.GetNetVarUInt64(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public float GetNetVar<T>(INativeObject nativeObject,
        string                              classname,
        string                              field,
        ushort                              extraOffset = 0,
        float?                              _           = null)
        where T : IComparable<float>
        => SchemaSystem.GetNetVarFloat(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public nint GetNetVar<T>(INativeObject nativeObject,
        string                             classname,
        string                             field,
        ushort                             extraOffset = 0,
        nint?                              _           = null)
        where T : IComparable<nint>
        => SchemaSystem.GetNetVarPointer(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public string GetNetVar<T>(INativeObject nativeObject,
        string                               classname,
        string                               field,
        ushort                               extraOffset = 0,
        string?                              _           = null)
        where T : IComparable<string>
        => SchemaSystem.GetNetVarString(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public Vector GetNetVar<T>(INativeObject nativeObject,
        string                               classname,
        string                               field,
        ushort                               extraOffset = 0,
        Vector?                              _           = null)
        where T : IComparable<Vector>
        => SchemaSystem.GetNetVarVector(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public string GetNetVarUtlSymbolLarge(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0)
        => SchemaSystem.GetNetVarUtlSymbolLarge(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(INativeObject nativeObject,
        string                                                          classname,
        string                                                          field,
        ushort                                                          extraOffset = 0)
        => ref SchemaSystem.GetNetVarUtlSymbolLargeRef(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public string GetNetVarUtlString(INativeObject nativeObject, string classname, string field, ushort extraOffset = 0)
        => SchemaSystem.GetNetVarUtlString(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public ref CUtlString GetNetVarUtlStringRef(INativeObject nativeObject,
        string                                                classname,
        string                                                field,
        ushort                                                extraOffset = 0)
        => ref SchemaSystem.GetNetVarUtlStringRef(nativeObject.GetAbsPtr(), classname, field, extraOffset);

    public ISchemaArray<T> GetSchemaFixedArray<T>(nint pointer, string classname, string field, ISchemaObject chain)
        where T : unmanaged
        => SchemaFixedArray<T>.Create(pointer, GetSchemaField(classname, field), chain.GetAbsPtr())
           ?? throw new ArgumentNullException(nameof(pointer));

    public ISchemaArray<T> GetSchemaFixedArray<T>(IntPtr pointer, SchemaField field, ISchemaObject chain) where T : unmanaged
        => SchemaFixedArray<T>.Create(pointer, field, chain.GetAbsPtr())
           ?? throw new ArgumentNullException(nameof(pointer));

    public ISchemaList<T> GetSchemaList<T>(IntPtr pointer, string classname, string field, ISchemaObject chain, bool isStruct)
        where T : unmanaged
        => SchemaUnmanagedVector<T>.Create(pointer, GetSchemaField(classname, field), chain.GetAbsPtr(), isStruct)
           ?? throw new ArgumentNullException(nameof(pointer));

    public ISchemaList<T> GetSchemaList<T>(IntPtr pointer, SchemaField field, ISchemaObject chain, bool isStruct)
        where T : unmanaged
        => SchemaUnmanagedVector<T>.Create(pointer, field, chain.GetAbsPtr(), isStruct)
           ?? throw new ArgumentNullException(nameof(pointer));

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        bool                            value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarBool(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        byte                            value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarByte(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        short                           value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarInt16(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        ushort                          value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarUInt16(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        int                             value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarInt32(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        uint                            value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarUInt32(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        long                            value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarInt64(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        ulong                           value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarUInt64(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        float                           value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarFloat(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        string                          value,
        int                             maxlen,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarString(nativeObject.GetAbsPtr(), classname, field, value, maxlen, isStruct, extraOffset);

    public void SetNetVar(INativeObject nativeObject,
        string                          classname,
        string                          field,
        Vector                          value,
        bool                            isStruct    = false,
        ushort                          extraOffset = 0)
        => SchemaSystem.SetNetVarVector(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVarUtlSymbolLarge(INativeObject nativeObject,
        string                                        classname,
        string                                        field,
        string                                        value,
        bool                                          isStruct    = false,
        ushort                                        extraOffset = 0)
        => SchemaSystem.SetNetVarUtlSymbolLarge(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void SetNetVarUtlString(INativeObject nativeObject,
        string                                   classname,
        string                                   field,
        string                                   value,
        bool                                     isStruct    = false,
        ushort                                   extraOffset = 0)
        => SchemaSystem.SetNetVarUtlString(nativeObject.GetAbsPtr(), classname, field, value, isStruct, extraOffset);

    public void NetVarStateChanged(INativeObject nativeObject,
        SchemaClass                              schemaClass,
        SchemaClassField                         schemaField,
        ushort                                   extraOffset = 0,
        bool                                     isStruct    = false)
        => SchemaSystem.NetVarStateChanged(nativeObject.GetAbsPtr(), schemaClass, schemaField, extraOffset, isStruct);

    public nint GetDataMapInputFunc(string classname, string fieldName)
        => SchemaSystem.GetDataMapInputFunc(classname, fieldName);
}
