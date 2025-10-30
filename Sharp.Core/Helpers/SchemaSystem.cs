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
using System.Runtime.CompilerServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;
using Sharp.Shared.Types.Tier;
using Sharp.Shared.Utilities;

// ReSharper disable InconsistentNaming

namespace Sharp.Core.Helpers;

public static class SchemaSystem
{
    public static SchemaField GetSchemaField(string classname, string field)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        var arraySize     = 1;

        if (schemaField.Category is SchemaTypeCategory.FixedArray)
        {
            var fieldTypeSpan = schemaField.Type.AsSpan();

            var startPos = fieldTypeSpan.IndexOf('[');
            var endPos   = fieldTypeSpan.IndexOf(']');

            if (startPos > -1 && endPos > -1 && endPos > startPos)
            {
                var arraySizeStr = fieldTypeSpan[(startPos + 1)..endPos];
                arraySize = int.Parse(arraySizeStr);
            }
        }

        return new SchemaField
        {
            Networked   = schemaField.Networked,
            ChainOffset = schemaClass.ChainOffset,
            Offset      = schemaField.Offset,
            ArraySize   = arraySize,
        };
    }

    private static SchemaClass GetSchemaClass(string classname)
    {
        if (!SharedGameObject.SchemaInfo.TryGetValue(classname, out var schemaClass))
        {
            throw new ArgumentException($"Invalid class {classname}");
        }

        return schemaClass;
    }

    private static SchemaClassField GetSchemaClassField(SchemaClass schemaClass, string field)
    {
        if (!schemaClass.Fields.TryGetValue(field, out var schemaField))
        {
            throw new ArgumentException($"Invalid NetVar {field} for class {schemaClass.ClassName}");
        }

        return schemaField;
    }

    public static int GetNetVarOffset(string classname, string field)
        => GetSchemaClassField(GetSchemaClass(classname), field).Offset;

    public static int GetNetVarOffset(string classname, string field, ushort extraOffset)
        => (ushort) (GetSchemaClassField(GetSchemaClass(classname), field).Offset + extraOffset);

    public static bool GetNetVarBool(nint ptr, string classname, string field, ushort extraOffset = 0)
        => GetNetVarByte(ptr, classname, field, extraOffset) != 0;

    public static byte GetNetVarByte(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetByte(GetNetVarOffset(classname, field) + extraOffset);

    public static short GetNetVarInt16(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt16(GetNetVarOffset(classname, field) + extraOffset);

    public static ushort GetNetVarUInt16(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt16(GetNetVarOffset(classname, field) + extraOffset);

    public static int GetNetVarInt32(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt32(GetNetVarOffset(classname, field) + extraOffset);

    public static uint GetNetVarUInt32(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt32(GetNetVarOffset(classname, field) + extraOffset);

    public static long GetNetVarInt64(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetInt64(GetNetVarOffset(classname, field) + extraOffset);

    public static ulong GetNetVarUInt64(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetUInt64(GetNetVarOffset(classname, field) + extraOffset);

    public static float GetNetVarFloat(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetFloat(GetNetVarOffset(classname, field) + extraOffset);

    public static nint GetNetVarPointer(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.GetObjectPtr(GetNetVarOffset(classname, field) + extraOffset);

    public static string GetNetVarString(nint ptr, string classname, string field, ushort extraOffset = 0)
        => ptr.ReadStringUtf8(GetNetVarOffset(classname, field) + extraOffset);

    public static unsafe Vector GetNetVarVector(nint ptr, string classname, string field, ushort extraOffset = 0)
        => *(Vector*) ptr.Add(GetNetVarOffset(classname, field) + extraOffset);

    public static unsafe string GetNetVarUtlSymbolLarge(nint ptr,
        string                                               classname,
        string                                               field,
        ushort                                               extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlSymbolLarge*) (ptr + offset);

        return pointer->Get();
    }

    public static unsafe ref CUtlSymbolLarge GetNetVarUtlSymbolLargeRef(nint ptr,
        string                                                               classname,
        string                                                               field,
        ushort                                                               extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlSymbolLarge*) (ptr + offset);

        return ref Unsafe.AsRef<CUtlSymbolLarge>(pointer);
    }

    public static unsafe string GetNetVarUtlString(nint ptr, string classname, string field, ushort extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlString*) (ptr + offset);

        return pointer->Get();
    }

    public static unsafe ref CUtlString GetNetVarUtlStringRef(nint ptr, string classname, string field, ushort extraOffset = 0)
    {
        var offset  = GetNetVarOffset(classname, field) + extraOffset;
        var pointer = (CUtlString*) (ptr + offset);

        return ref Unsafe.AsRef<CUtlString>(pointer);
    }

    public static void SetNetVarBool(nint ptr,
        string                            classname,
        string                            field,
        bool                              value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0)
        => SetNetVarByte(ptr, classname, field, (byte) (value ? 1 : 0), isStruct, extraOffset);

    public static void SetNetVarByte(nint ptr,
        string                            classname,
        string                            field,
        byte                              value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteByte(schemaField.Offset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarInt16(nint ptr,
        string                             classname,
        string                             field,
        short                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteInt16(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarUInt16(nint ptr,
        string                              classname,
        string                              field,
        ushort                              value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteUInt16(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarInt32(nint ptr,
        string                             classname,
        string                             field,
        int                                value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteInt32(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarUInt32(nint ptr,
        string                              classname,
        string                              field,
        uint                                value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteUInt32(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarInt64(nint ptr,
        string                             classname,
        string                             field,
        long                               value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteInt64(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarUInt64(nint ptr,
        string                              classname,
        string                              field,
        ulong                               value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteUInt64(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarFloat(nint ptr,
        string                             classname,
        string                             field,
        float                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteFloat(schemaField.Offset + extraOffset, value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void SetNetVarString(nint ptr,
        string                              classname,
        string                              field,
        string                              value,
        int                                 length,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteStringUtf8(schemaField.Offset + extraOffset, value, length);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static unsafe void SetNetVarUtlSymbolLarge(nint ptr,
        string                                             classname,
        string                                             field,
        string                                             value,
        bool                                               isStruct    = false,
        ushort                                             extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        var offset      = schemaField.Offset;
        var thisPtr     = ptr.ToPointer();
        var pointer     = (CUtlSymbolLarge*) ((byte*) thisPtr + offset + extraOffset);
        var alloc       = new CUtlSymbolLarge(Entity.AllocPooledString(value));
        *pointer = alloc;

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, isStruct: isStruct);
    }

    public static unsafe void SetNetVarUtlString(nint ptr,
        string                                        classname,
        string                                        field,
        string                                        value,
        bool                                          isStruct    = false,
        ushort                                        extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        var pointer     = (CUtlString*) (ptr + schemaField.Offset + extraOffset);
        pointer->SetString(value);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, isStruct: isStruct);
    }

    public static void SetNetVarVector(nint ptr,
        string                              classname,
        string                              field,
        Vector                              value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);
        ptr.WriteFloat(schemaField.Offset + extraOffset + 0, value.X);
        ptr.WriteFloat(schemaField.Offset + extraOffset + 4, value.Y);
        ptr.WriteFloat(schemaField.Offset + extraOffset + 8, value.Z);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }

    public static void NetVarStateChanged(nint ptr,
        SchemaClass                            schemaClass,
        SchemaClassField                       schemaField,
        ushort                                 extraOffset = 0,
        bool                                   isStruct    = false)
    {
        if (!schemaField.Networked)
        {
            return;
        }

        if (schemaClass.ChainOffset > 0)
        {
            Entity.NetworkStateChanged(ptr.Add(schemaClass.ChainOffset),
                                       (ushort) (schemaField.Offset + extraOffset));
        }
        else if (!isStruct)
        {
            Entity.SetStateChanged(ptr, (ushort) (schemaField.Offset + extraOffset));
        }
    }

    public static bool FindNetVar(string classname, string field)
    {
        if (!SharedGameObject.SchemaInfo.TryGetValue(classname, out var schemaClass))
        {
            return false;
        }

        if (!schemaClass.Fields.ContainsKey(field))
        {
            return false;
        }

        return true;
    }

    public static void NetworkStateChanged(nint ptr,
        string                                  classname,
        string                                  field,
        bool                                    isStruct    = false,
        ushort                                  extraOffset = 0)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct);
    }
}
