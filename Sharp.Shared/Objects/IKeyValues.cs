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

using Sharp.Shared.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Sharp.Shared.Objects;

public interface IKeyValues : INativeObject
{
    /// <summary>
    ///     Destroy this instance <br />
    ///     <remarks>
    ///         Only instances created by <see cref="IModSharp.CreateKeyValues" /> or <see cref="IKeyValues.Clone" /> can
    ///         be destroyed
    ///     </remarks>
    /// </summary>
    void DeleteThis();

    /// <summary>
    ///     Clone this KeyValues instance
    /// </summary>
    IKeyValues Clone();

    /// <summary>
    ///     Clear all keys and sub-keys
    /// </summary>
    void Clear();

    /// <summary>
    ///     Load from file
    /// </summary>
    bool LoadFromFile(string filename, string? pathId = null);

    /// <summary>
    ///     Save to file
    /// </summary>
    bool SaveToFile(string filename, string? pathId = null, bool allowEmptyString = false);

    /// <summary>
    ///     Load from string
    /// </summary>
    bool LoadFromString(string buffer);

    /// <summary>
    ///     Export to string
    /// </summary>
    bool SaveToString(int size,
        out string        result,
        int               indent           = 0,
        bool              sort             = false,
        bool              allowEmptyString = false);

    /// <summary>
    ///     Get current section name
    /// </summary>
    string GetSectionName();

    /// <summary>
    ///     Set section name
    /// </summary>
    void SetSectionName(string name);

    /// <summary>
    ///     Get first sub-key (includes both keys and key-value pairs)
    /// </summary>
    IKeyValues? GetFirstSubKey();

    /// <summary>
    ///     Get last sub-key (includes both keys and key-value pairs)
    /// </summary>
    IKeyValues? FindLastSubKey();

    /// <summary>
    ///     Get next key
    /// </summary>
    IKeyValues? GetNextKey();

    /// <summary>
    ///     Get first sub-key (keys only, not key-value pairs)
    /// </summary>
    IKeyValues? GetFirstTrueSubKey();

    /// <summary>
    ///     Get next sub-key (keys only, not key-value pairs)
    /// </summary>
    IKeyValues? GetNextTrueSubKey();

    /// <summary>
    ///     Find key by name, optionally create if not found
    /// </summary>
    /// <param name="name">Key name</param>
    /// <param name="bCreate">Create if key doesn't exist</param>
    IKeyValues? FindKey(string name, bool bCreate = false);

    /// <summary>
    ///     Find and delete sub-key by name
    /// </summary>
    bool FindAndDeleteSubKey(string name);

    /// <summary>
    ///     Add new key
    /// </summary>
    IKeyValues AddKey(string name);

    /// <summary>
    ///     Get data type of value
    /// </summary>
    KeyValuesDataType GetDataType(string? name = null);

    int GetInt(string? name = null, int defaultValue = 0);

    ulong GetUint64(string? name = null, ulong defaultValue = 0);

    float GetFloat(string? name = null, float defaultValue = 0.0f);

    string GetString(string? name = null, string defaultValue = "");

    nint GetPtr(string? name = null);

    bool GetBool(string? name = null, bool defaultValue = false);

    bool IsEmpty(string? name = null);

    void SetString(string name, string value);

    void SetInt(string name, int value);

    void SetUint64(string name, ulong value);

    void SetFloat(string name, float value);

    void SetPtr(string name, nint value);

    void SetBool(string name, bool value);
}

public interface IKeyValues3 : INativeObject
{
    /// <summary>
    ///     Destroy this instance <br />
    ///     <remarks>Only instances created by <see cref="IModSharp.CreateKeyValues3" /> can be destroyed</remarks>
    /// </summary>
    void DeleteThis();

    bool LoadFromFile(string file, string pathId, out string error);

    bool LoadFromCompiledFile(string file, string pathId, out string error);

    bool LoadFromBuffer(byte[] buffer, out string error);

    KeyValues3Type GetKvType();

    bool IsInvalid();

    bool IsNull();

    bool IsBool();

    bool IsInt();

    bool IsUInt();

    /// <summary>
    ///     IsInt || IsUInt
    /// </summary>
    /// <returns></returns>
    bool IsIntegral();

    bool IsDouble();

    bool IsString();

    bool IsBinaryBlob();

    bool IsArray();

    bool IsTable();

    bool IsNullOrInvalid();

    KeyValues3SubType GetSubType();

    int GetArrayElementCount();

    IKeyValues3? GetArrayElement(int index);

    IKeyValues3? AddArrayElementToTail();

    void RemoveArrayElement(int index);

    int GetMemberCount();

    IKeyValues3? GetMember(int index);

    string GetMemberName(int index);

    IKeyValues3? FindMember(string name);

    IKeyValues3? FindOrCreateMember(string name, out bool create);

    bool RemoveMember(string name);

    // getter
    bool GetBool(bool defaultValue = false);

    sbyte GetInt8(sbyte defaultValue = 0);

    short GetInt16(short defaultValue = 0);

    int GetInt32(int defaultValue = 0);

    long GetInt64(long defaultValue = 0);

    byte GetUInt8(byte defaultValue = 0);

    ushort GetUInt16(ushort defaultValue = 0);

    uint GetUInt32(uint defaultValue = 0);

    ulong GetUInt64(ulong defaultValue = 0);

    float GetFloat(float defaultValue = 0);

    double GetDouble(double defaultValue = 0);

    string GetString(string defaultValue = "");

    Color32 GetColor();

    Vector GetVector();

    Vector GetQAngle();

    Matrix3x4 GetMatrix();

    // setter
    void SetBool(bool value);

    void SetInt8(sbyte value);

    void SetInt16(short value);

    void SetInt32(int value);

    void SetInt64(long value);

    void SetUInt8(byte value);

    void SetUInt16(ushort value);

    void SetUInt32(uint value);

    void SetUInt64(ulong value);

    void SetFloat(float value);

    void SetDouble(double value);

    void SetString(string value);

    void SetColor(Color32 value);

    void SetVector(Vector value);

    void SetQAngle(Vector value);

    void SetMatrix(Matrix3x4 value);
}
