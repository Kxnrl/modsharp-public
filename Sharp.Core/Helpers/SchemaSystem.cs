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
using System.Text;
using System.Text.Unicode;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Shared;
using Sharp.Shared.CStrike;
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

        var arraySize = 1;

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

    public static int GetChainOffset(string classname)
        => SharedGameObject.SchemaInfo.TryGetValue(classname, out var schemaClass) ? schemaClass.ChainOffset : 0;

    public static int GetSchemaClassSize(string classname)
        => GetSchemaClass(classname).Size;

    public static byte GetSchemaClassAlignOf(string classname)
        => GetSchemaClass(classname).AlignOf;

    public static (SchemaClass, SchemaClassField) ResolveField(string classname, string field)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        return (schemaClass, schemaField);
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
        ushort                            extraOffset = 0,
        ISchemaObject?                    self        = null)
        => SetNetVarByte(ptr, classname, field, (byte) (value ? 1 : 0), isStruct, extraOffset, self);

    public static void SetNetVarByte(nint ptr,
        string                            classname,
        string                            field,
        byte                              value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0,
        ISchemaObject?                    self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarByte(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarInt16(nint ptr,
        string                             classname,
        string                             field,
        short                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarInt16(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarUInt16(nint ptr,
        string                              classname,
        string                              field,
        ushort                              value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarUInt16(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarInt32(nint ptr,
        string                             classname,
        string                             field,
        int                                value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarInt32(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarUInt32(nint ptr,
        string                              classname,
        string                              field,
        uint                                value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarUInt32(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarInt64(nint ptr,
        string                             classname,
        string                             field,
        long                               value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarInt64(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarUInt64(nint ptr,
        string                              classname,
        string                              field,
        ulong                               value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarUInt64(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void SetNetVarFloat(nint ptr,
        string                             classname,
        string                             field,
        float                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarFloat(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static unsafe void SetNetVarString(nint ptr,
        string                                     classname,
        string                                     field,
        string                                     value,
        int                                        length,
        bool                                       isStruct    = false,
        ushort                                     extraOffset = 0,
        ISchemaObject?                             self        = null)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        SetNetVarString(ptr, schemaClass, schemaField, value, length, isStruct, extraOffset, self);
    }

    public static unsafe void SetNetVarString(nint ptr,
        SchemaClass                                schemaClass,
        SchemaClassField                           schemaField,
        string                                     value,
        int                                        length,
        bool                                       isStruct    = false,
        ushort                                     extraOffset = 0,
        ISchemaObject?                             self        = null)
    {
        var offset = schemaField.Offset + extraOffset;
        var dest   = (byte*) (ptr + offset);
        var maxLen = length - 1;
        var newLen = Encoding.UTF8.GetByteCount(value);

        if (newLen > maxLen)
        {
            newLen = maxLen;
        }

        // check current string length first
        var currentLen = 0;

        while (currentLen < length && dest[currentLen] != 0)
        {
            currentLen++;
        }

        if (currentLen == newLen && newLen > 0)
        {
            Span<byte> newBytes = stackalloc byte[newLen];
            Utf8.FromUtf16(value, newBytes, out _, out _);

            if (new ReadOnlySpan<byte>(dest, currentLen).SequenceEqual(newBytes))
            {
                return;
            }
        }

        ptr.WriteStringUtf8(offset, value, length);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static unsafe void SetNetVarUtlSymbolLarge(nint ptr,
        string                                             classname,
        string                                             field,
        string                                             value,
        bool                                               isStruct    = false,
        ushort                                             extraOffset = 0,
        ISchemaObject?                                     self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarUtlSymbolLarge(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static unsafe void SetNetVarUtlString(nint ptr,
        string                                        classname,
        string                                        field,
        string                                        value,
        bool                                          isStruct    = false,
        ushort                                        extraOffset = 0,
        ISchemaObject?                                self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarUtlString(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static unsafe void SetNetVarVector(nint ptr,
        string                                     classname,
        string                                     field,
        Vector                                     value,
        bool                                       isStruct    = false,
        ushort                                     extraOffset = 0,
        ISchemaObject?                             self        = null)
    {
        var (schemaClass, schemaField) = ResolveField(classname, field);
        SetNetVarVector(ptr, schemaClass, schemaField, value, isStruct, extraOffset, self);
    }

    public static void NetVarStateChanged(nint ptr,
        SchemaClass                            schemaClass,
        SchemaClassField                       schemaField,
        ushort                                 extraOffset = 0,
        bool                                   isStruct    = false,
        ISchemaObject?                         self        = null)
    {
        if (!schemaField.Networked)
        {
            return;
        }

        if (self is SchemaObject schemaObject)
        {
            schemaObject.SchemaStateChanged(schemaField.Offset + extraOffset, schemaClass.ChainOffset, isStruct);

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

    public static List<DataMapField> GetDataMapFields(string classname)
    {
        var schemaClass = GetSchemaClass(classname);

        return schemaClass.DataMapFields;
    }

    public static DataMapField? GetDataMapField(string classname, string fieldName)
    {
        var schemaClass = GetSchemaClass(classname);

        return schemaClass.DataMapFields.Find(f => f.Name == fieldName);
    }

    public static nint GetDataMapInputFunc(string classname, string fieldName)
    {
        var schemaClass = GetSchemaClass(classname);

        var field = schemaClass.DataMapFields.Find(f => f.Name == fieldName);

        if (field != null)
        {
            return field.InputFunc;
        }

        foreach (var baseClassName in schemaClass.BaseClasses)
        {
            if (!SharedGameObject.SchemaInfo.TryGetValue(baseClassName, out var baseClass))
            {
                continue;
            }

            var baseField = baseClass.DataMapFields.Find(f => f.Name == fieldName);

            if (baseField != null)
            {
                return baseField.InputFunc;
            }
        }

        return nint.Zero;
    }

    public static void NetworkStateChanged(nint ptr,
        string                                  classname,
        string                                  field,
        bool                                    isStruct    = false,
        ushort                                  extraOffset = 0,
        ISchemaObject?                          self        = null)
    {
        var schemaClass = GetSchemaClass(classname);
        var schemaField = GetSchemaClassField(schemaClass, field);

        // state changed
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

#region Fast-path overloads (pre-resolved SchemaClass + SchemaClassField)

    public static void SetNetVarByte(nint ptr,
        SchemaClass                       schemaClass,
        SchemaClassField                  schemaField,
        byte                              value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0,
        ISchemaObject?                    self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetByte(offset) == value)
        {
            return;
        }

        ptr.WriteByte(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarBool(nint ptr,
        SchemaClass                       schemaClass,
        SchemaClassField                  schemaField,
        bool                              value,
        bool                              isStruct    = false,
        ushort                            extraOffset = 0,
        ISchemaObject?                    self        = null)
        => SetNetVarByte(ptr, schemaClass, schemaField, (byte) (value ? 1 : 0), isStruct, extraOffset, self);

    public static void SetNetVarInt16(nint ptr,
        SchemaClass                        schemaClass,
        SchemaClassField                   schemaField,
        short                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetInt16(offset) == value)
        {
            return;
        }

        ptr.WriteInt16(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarUInt16(nint ptr,
        SchemaClass                         schemaClass,
        SchemaClassField                    schemaField,
        ushort                              value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetUInt16(offset) == value)
        {
            return;
        }

        ptr.WriteUInt16(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarInt32(nint ptr,
        SchemaClass                        schemaClass,
        SchemaClassField                   schemaField,
        int                                value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetInt32(offset) == value)
        {
            return;
        }

        ptr.WriteInt32(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarUInt32(nint ptr,
        SchemaClass                         schemaClass,
        SchemaClassField                    schemaField,
        uint                                value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetUInt32(offset) == value)
        {
            return;
        }

        ptr.WriteUInt32(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarInt64(nint ptr,
        SchemaClass                        schemaClass,
        SchemaClassField                   schemaField,
        long                               value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetInt64(offset) == value)
        {
            return;
        }

        ptr.WriteInt64(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarUInt64(nint ptr,
        SchemaClass                         schemaClass,
        SchemaClassField                    schemaField,
        ulong                               value,
        bool                                isStruct    = false,
        ushort                              extraOffset = 0,
        ISchemaObject?                      self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        if (ptr.GetUInt64(offset) == value)
        {
            return;
        }

        ptr.WriteUInt64(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static void SetNetVarFloat(nint ptr,
        SchemaClass                        schemaClass,
        SchemaClassField                   schemaField,
        float                              value,
        bool                               isStruct    = false,
        ushort                             extraOffset = 0,
        ISchemaObject?                     self        = null)
    {
        var offset = schemaField.Offset + extraOffset;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (ptr.GetFloat(offset) == value)
        {
            return;
        }

        ptr.WriteFloat(offset, value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static unsafe void SetNetVarVector(nint ptr,
        SchemaClass                                schemaClass,
        SchemaClassField                           schemaField,
        Vector                                     value,
        bool                                       isStruct    = false,
        ushort                                     extraOffset = 0,
        ISchemaObject?                             self        = null)
    {
        var offset  = schemaField.Offset + extraOffset;
        var current = *(Vector*) (ptr + offset);

        if (current.X == value.X && current.Y == value.Y && current.Z == value.Z)
        {
            return;
        }

        *(Vector*) (ptr + offset) = value;
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static unsafe void SetNetVarUtlSymbolLarge(nint ptr,
        SchemaClass                                        schemaClass,
        SchemaClassField                                   schemaField,
        string                                             value,
        bool                                               isStruct    = false,
        ushort                                             extraOffset = 0,
        ISchemaObject?                                     self        = null)
    {
        var pointer = (CUtlSymbolLarge*) ((byte*) ptr.ToPointer() + schemaField.Offset + extraOffset);
        var alloc   = new CUtlSymbolLarge(Entity.AllocPooledString(value));

        if (*pointer == alloc)
        {
            return;
        }

        *pointer = alloc;
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

    public static unsafe void SetNetVarUtlString(nint ptr,
        SchemaClass                                   schemaClass,
        SchemaClassField                              schemaField,
        string                                        value,
        bool                                          isStruct    = false,
        ushort                                        extraOffset = 0,
        ISchemaObject?                                self        = null)
    {
        var pointer    = (CUtlString*) (ptr + schemaField.Offset + extraOffset);
        var newLen     = Encoding.UTF8.GetByteCount(value);
        var currentLen = pointer->Length();

        if (newLen == currentLen && currentLen > 0)
        {
            var        currentSpan = pointer->AsReadOnlySpan();
            Span<byte> newBytes    = stackalloc byte[newLen];
            Utf8.FromUtf16(value, newBytes, out _, out _);

            if (currentSpan.SequenceEqual(newBytes))
            {
                return;
            }
        }

        pointer->SetString(value);
        NetVarStateChanged(ptr, schemaClass, schemaField, extraOffset, isStruct, self);
    }

#endregion
}
