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

using Sharp.Core.CStrike;
using Sharp.Generator;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Core.Bridges.Interfaces;

[NativeVirtualObject(HasDestructors = true)]
internal unsafe partial class KeyValuesHelper : NativeObject
{
    // construct
    public partial nint CreateKeyValues(string name);

    public partial void DestroyKeyValues(nint kv);

    public partial nint Clone(nint kv);

    public partial void Clear(nint kv);

    // file
    public partial bool LoadFromFile(nint kv, string filename, string? pathId);

    public partial bool SaveToFile(nint kv, string filename, string? pathId, bool allowEmptyString);

    // text
    public partial bool LoadFromString(nint kv, string pBuffer);

    public partial bool SaveToString(nint kv,
        byte*                             buffer,
        long                              size,
        int                               indent,
        bool                              sort,
        bool                              allowEmptyString);

    // section
    public partial string GetSectionName(nint kv);

    public partial void SetSectionName(nint kv, string name);

    // iteration
    public partial nint GetFirstSubKey(nint kv);

    public partial nint FindLastSubKey(nint kv);

    public partial nint GetNextKey(nint kv);

    public partial nint GetFirstTrueSubKey(nint kv);

    public partial nint GetNextTrueSubKey(nint kv);

    // key
    public partial nint FindKey(nint kv, string name, bool bCreate);

    public partial bool FindAndDeleteSubKey(nint kv, string name);

    public partial nint AddKey(nint kv, string name);

    // value
    public partial KeyValuesDataType GetDataType(nint kv, string? name);

    public partial bool IsEmpty(nint kv, string? name);

    public partial int GetInt(nint kv, string? name, int defaultValue);

    public partial ulong GetUint64(nint kv, string? name, ulong defaultValue);

    public partial float GetFloat(nint kv, string? name, float defaultValue);

    public partial string GetString(nint kv, string? name, string defaultValue);

    public partial nint GetPtr(nint kv, string? name, nint defaultValue);

    public partial void SetString(nint kv, string name, string value);

    public partial void SetInt(nint kv, string name, int value);

    public partial void SetUint64(nint kv, string name, ulong value);

    public partial void SetFloat(nint kv, string name, float value);

    public partial void SetPtr(nint kv, string name, nint value);
}

[NativeVirtualObject(HasDestructors = true)]
internal unsafe partial class KeyValues3Helper : NativeObject
{
    // construct
    public partial nint CreateKeyValues3(KeyValues3Type type, KeyValues3SubType subType);

    public partial nint DestroyKeyValues3(nint kv);

    // loader
    public partial bool LoadFromFile(nint kv, void* error, string file, string path);

    public partial bool LoadFromCompiledFile(nint kv, void* error, string file, string path, ResourceBlockType type);

    public partial bool LoadFromBuffer(nint kv, void* error, byte* buffer, int length, string kvName);

    // type
    public partial KeyValues3Type GetKvType(nint kv);

    public partial KeyValues3Type GetTypeEx(nint kv);

    public partial KeyValues3SubType GetSubType(nint kv);

    // array
    public partial int GetArrayElementCount(nint kv);

    public partial nint GetArrayElement(nint kv, int index);

    public partial nint AddArrayElementToTail(nint kv);

    public partial void RemoveArrayElement(nint kv, int index);

    // member
    public partial int GetMemberCount(nint kv);

    public partial nint GetMember(nint kv, int index);

    public partial nint GetMemberName(nint kv, int index);

    public partial nint FindMember(nint kv, string name);

    public partial nint FindOrCreateMember(nint kv, string name, bool* create);

    public partial bool RemoveMember(nint kv, string name);

    // getter
    public partial bool GetBool(nint kv, bool def);

    public partial sbyte GetInt8(nint kv, sbyte def);

    public partial short GetInt16(nint kv, short def);

    public partial int GetInt32(nint kv, int def);

    public partial long GetInt64(nint kv, long def);

    public partial byte GetUInt8(nint kv, byte def);

    public partial ushort GetUInt16(nint kv, ushort def);

    public partial uint GetUInt32(nint kv, uint def);

    public partial ulong GetUInt64(nint kv, ulong def);

    public partial float GetFloat(nint kv, float def);

    public partial double GetDouble(nint kv, double def);

    public partial sbyte* GetString(nint kv, string def);

    public partial byte* GetBinaryBlob(nint kv);

    public partial ulong GetBinaryBlobSize(nint kv);

    public partial void GetColor(nint kv, Color32* value);

    public partial void GetVector(nint kv, Vector* value);

    public partial void GetQAngle(nint kv, Vector* value);

    public partial void GetMatrix(nint kv, Matrix3x4* value);

    // setter
    public partial void SetBool(nint kv, bool value);

    public partial void SetInt8(nint kv, sbyte value);

    public partial void SetInt16(nint kv, short value);

    public partial void SetInt32(nint kv, int value);

    public partial void SetInt64(nint kv, long value);

    public partial void SetUInt8(nint kv, byte value);

    public partial void SetUInt16(nint kv, ushort value);

    public partial void SetUInt32(nint kv, uint value);

    public partial void SetUInt64(nint kv, ulong value);

    public partial void SetFloat(nint kv, float value);

    public partial void SetDouble(nint kv, double value);

    public partial void SetString(nint kv, string value);

    public partial void SetColor(nint kv, Color32* value);

    public partial void SetVector(nint kv, Vector* value);

    public partial void SetQAngle(nint kv, Vector* value);

    public partial void SetMatrix(nint kv, Matrix3x4* value);
}
