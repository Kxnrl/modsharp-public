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
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Sharp.Core.Bridges;
using Sharp.Core.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class KeyValues : NativeObject, IKeyValues
{
    private bool _deletable;

    internal void MakeDeletable()
        => _deletable = true;

    public void DeleteThis()
    {
        if (!_deletable)
        {
            throw new InvalidOperationException("KeyValues is not deletable!");
        }

        CoreBridge.KeyValuesHelperInstance.DestroyKeyValues(_this);

        MarkAsDisposed();
    }

    public IKeyValues Clone()
    {
        CheckDisposed();

        var clone = Create(CoreBridge.KeyValuesHelperInstance.Clone(_this))!;
        clone.MakeDeletable();

        return clone;
    }

    public void Clear()
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.Clear(_this);
    }

    public bool LoadFromFile(string filename, string? pathId = null)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.LoadFromFile(_this, filename, pathId);
    }

    public bool SaveToFile(string filename, string? pathId = null, bool allowEmptyString = false)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.SaveToFile(_this, filename, pathId, allowEmptyString);
    }

    public bool LoadFromString(string buffer)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.LoadFromString(_this, buffer);
    }

    public unsafe bool SaveToString(int size,
        out string                      result,
        int                             indent           = 0,
        bool                            sort             = false,
        bool                            allowEmptyString = false)
    {
        CheckDisposed();

        var pool   = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(size);

        try
        {
            fixed (byte* ptr = buffer)
            {
                var success = CoreBridge.KeyValuesHelperInstance.SaveToString(_this, ptr, size, indent, sort, allowEmptyString);
                result = Encoding.UTF8.GetString(ptr, size);

                return success;
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public string GetSectionName()
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetSectionName(_this);
    }

    public void SetSectionName(string name)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetSectionName(_this, name);
    }

    public IKeyValues? GetFirstSubKey()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.GetFirstSubKey(_this));
    }

    public IKeyValues? FindLastSubKey()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.FindLastSubKey(_this));
    }

    public IKeyValues? GetNextKey()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.GetNextKey(_this));
    }

    public IKeyValues? GetFirstTrueSubKey()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.GetFirstTrueSubKey(_this));
    }

    public IKeyValues? GetNextTrueSubKey()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.GetNextTrueSubKey(_this));
    }

    public IKeyValues? FindKey(string name, bool bCreate = false)
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.FindKey(_this, name, bCreate));
    }

    public bool FindAndDeleteSubKey(string name)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.FindAndDeleteSubKey(_this, name);
    }

    public IKeyValues AddKey(string name)
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValuesHelperInstance.AddKey(_this, name))!;
    }

    public KeyValuesDataType GetDataType(string? name = null)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetDataType(_this, name);
    }

    public int GetInt(string? name = null, int defaultValue = 0)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetInt(_this, name, defaultValue);
    }

    public ulong GetUint64(string? name = null, ulong defaultValue = 0)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetUint64(_this, name, defaultValue);
    }

    public float GetFloat(string? name = null, float defaultValue = 0)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetFloat(_this, name, defaultValue);
    }

    public string GetString(string? name = null, string defaultValue = "")
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetString(_this, name, defaultValue);
    }

    public IntPtr GetPtr(string? name = null)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.GetPtr(_this, name, nint.Zero);
    }

    public bool GetBool(string? name = null, bool defaultValue = false)
        => GetInt(name, defaultValue ? 1 : 0) != 0;

    public bool IsEmpty(string? name = null)
    {
        CheckDisposed();

        return CoreBridge.KeyValuesHelperInstance.IsEmpty(_this, name);
    }

    public void SetString(string name, string value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetString(_this, name, value);
    }

    public void SetInt(string name, int value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetInt(_this, name, value);
    }

    public void SetUint64(string name, ulong value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetUint64(_this, name, value);
    }

    public void SetFloat(string name, float value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetFloat(_this, name, value);
    }

    public void SetPtr(string name, IntPtr value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetPtr(_this, name, value);
    }

    public void SetBool(string name, bool value)
    {
        CheckDisposed();

        CoreBridge.KeyValuesHelperInstance.SetInt(_this, name, value ? 1 : 0);
    }
}

internal unsafe partial class KeyValues3 : NativeObject, IKeyValues3
{
    private bool _deletable;

    internal void MakeDeletable()
        => _deletable = true;

    public void DeleteThis()
    {
        if (!_deletable)
        {
            throw new InvalidOperationException("KeyValues is not deletable!");
        }

        CoreBridge.KeyValues3HelperInstance.DestroyKeyValues3(_this);

        MarkAsDisposed();
    }

    public bool LoadFromFile(string file, string path, out string error)
    {
        CheckDisposed();

        var ptr     = stackalloc byte[256];
        var success = CoreBridge.KeyValues3HelperInstance.LoadFromFile(_this, ptr, file, path);
        error = Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

        return success;
    }

    public bool LoadFromCompiledFile(string file, string path, out string error)
    {
        CheckDisposed();

        if (!file.EndsWith("_c"))
        {
            file += "_c";
        }

        var ptr     = stackalloc byte[256];
        var success = CoreBridge.KeyValues3HelperInstance.LoadFromCompiledFile(_this, ptr, file, path, ResourceBlockType.DATA);
        error = Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

        return success;
    }

    public bool LoadFromCompiledFile(string file, string path, ResourceBlockType block, out string error)
    {
        CheckDisposed();

        if (!file.EndsWith("_c"))
        {
            file += "_c";
        }

        var ptr     = stackalloc byte[256];
        var success = CoreBridge.KeyValues3HelperInstance.LoadFromCompiledFile(_this, ptr, file, path, block);
        error = Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

        return success;
    }

    public bool LoadFromBuffer(byte[] input, out string error)
    {
        CheckDisposed();

        fixed (byte* inputByte = input)
        {
            var ptr     = stackalloc byte[256];
            var success = CoreBridge.KeyValues3HelperInstance.LoadFromBuffer(_this, ptr, inputByte, input.Length, "");
            error = Marshal.PtrToStringUTF8((nint) ptr) ?? string.Empty;

            return success;
        }
    }

    public KeyValues3Type GetKvType()
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetKvType(_this);
    }

    public bool IsInvalid()
        => GetKvType() is KeyValues3Type.Invalid;

    public bool IsNull()
        => GetKvType() is KeyValues3Type.Null;

    public bool IsBool()
        => GetKvType() is KeyValues3Type.Bool;

    public bool IsInt()
        => GetKvType() is KeyValues3Type.Int;

    public bool IsUInt()
        => GetKvType() is KeyValues3Type.UInt;

    public bool IsIntegral()
        => IsInt() || IsUInt();

    public bool IsDouble()
        => GetKvType() is KeyValues3Type.Double;

    public bool IsString()
        => GetKvType() is KeyValues3Type.String;

    public bool IsBinaryBlob()
        => GetKvType() is KeyValues3Type.BinaryBlob;

    public bool IsArray()
        => GetKvType() is KeyValues3Type.Array;

    public bool IsTable()
        => GetKvType() is KeyValues3Type.Table;

    public bool IsNullOrInvalid()
        => IsNull() || IsInvalid();

    public KeyValues3SubType GetSubType()
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetSubType(_this);
    }

    public int GetArrayElementCount()
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetArrayElementCount(_this);
    }

    public IKeyValues3? GetArrayElement(int index)
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValues3HelperInstance.GetArrayElement(_this, index));
    }

    public IKeyValues3? AddArrayElementToTail()
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValues3HelperInstance.AddArrayElementToTail(_this));
    }

    public void RemoveArrayElement(int index)
    {
        CheckDisposed();
        CoreBridge.KeyValues3HelperInstance.RemoveArrayElement(_this, index);
    }

    public int GetMemberCount()
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetMemberCount(_this);
    }

    public IKeyValues3? GetMember(int index)
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValues3HelperInstance.GetMember(_this, index));
    }

    public string GetMemberName(int index)
    {
        CheckDisposed();

        return Utils.ReadString((sbyte*) CoreBridge.KeyValues3HelperInstance.GetMemberName(_this, index));
    }

    public IKeyValues3? FindMember(string name)
    {
        CheckDisposed();

        return Create(CoreBridge.KeyValues3HelperInstance.FindMember(_this, name));
    }

    public IKeyValues3? FindOrCreateMember(string name, out bool create)
    {
        CheckDisposed();

        var cc = false;
        var kv = Create(CoreBridge.KeyValues3HelperInstance.FindOrCreateMember(_this, name, &cc));

        create = cc;

        return kv;
    }

    public bool RemoveMember(string name)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.RemoveMember(_this, name);
    }

    public bool GetBool(bool def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetBool(_this, def);
    }

    public sbyte GetInt8(sbyte def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetInt8(_this, def);
    }

    public short GetInt16(short def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetInt16(_this, def);
    }

    public int GetInt32(int def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetInt32(_this, def);
    }

    public long GetInt64(long def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetInt64(_this, def);
    }

    public byte GetUInt8(byte def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetUInt8(_this, def);
    }

    public ushort GetUInt16(ushort def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetUInt16(_this, def);
    }

    public uint GetUInt32(uint def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetUInt32(_this, def);
    }

    public ulong GetUInt64(ulong def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetUInt64(_this, def);
    }

    public float GetFloat(float def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetFloat(_this, def);
    }

    public double GetDouble(double def)
    {
        CheckDisposed();

        return CoreBridge.KeyValues3HelperInstance.GetDouble(_this, def);
    }

    public string GetString(string def)
    {
        CheckDisposed();

        return Utils.ReadString(CoreBridge.KeyValues3HelperInstance.GetString(_this, def));
    }

    public ReadOnlySpan<byte> GetBinaryBlob()
    {
        CheckDisposed();

        var size = CoreBridge.KeyValues3HelperInstance.GetBinaryBlobSize(_this);
        var data = CoreBridge.KeyValues3HelperInstance.GetBinaryBlob(_this);

        if (data is null || size is 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>(data, (int) size).ToArray();
    }

    public Color32 GetColor()
    {
        var c = new Color32();

        CoreBridge.KeyValues3HelperInstance.GetColor(_this, &c);

        return c;
    }

    public Vector GetVector()
    {
        var v = new Vector();

        CoreBridge.KeyValues3HelperInstance.GetVector(_this, &v);

        return v;
    }

    public Vector GetQAngle()
    {
        var v = new Vector();

        CoreBridge.KeyValues3HelperInstance.GetQAngle(_this, &v);

        return v;
    }

    public Matrix3x4 GetMatrix()
    {
        var m = new Matrix3x4();

        CoreBridge.KeyValues3HelperInstance.GetMatrix(_this, &m);

        return m;
    }

    public void SetBool(bool value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetBool(_this, value);
    }

    public void SetInt8(sbyte value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetInt8(_this, value);
    }

    public void SetInt16(short value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetInt16(_this, value);
    }

    public void SetInt32(int value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetInt32(_this, value);
    }

    public void SetInt64(long value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetInt64(_this, value);
    }

    public void SetUInt8(byte value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetUInt8(_this, value);
    }

    public void SetUInt16(ushort value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetUInt16(_this, value);
    }

    public void SetUInt32(uint value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetUInt32(_this, value);
    }

    public void SetUInt64(ulong value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetUInt64(_this, value);
    }

    public void SetFloat(float value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetFloat(_this, value);
    }

    public void SetDouble(double value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetDouble(_this, value);
    }

    public void SetString(string value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetString(_this, value);
    }

    public void SetColor(Color32 value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetColor(_this, &value);
    }

    public void SetVector(Vector value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetVector(_this, &value);
    }

    public void SetQAngle(Vector value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetQAngle(_this, &value);
    }

    public void SetMatrix(Matrix3x4 value)
    {
        CheckDisposed();

        CoreBridge.KeyValues3HelperInstance.SetMatrix(_this, &value);
    }
}
