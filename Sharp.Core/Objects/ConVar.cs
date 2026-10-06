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
using System.Globalization;
using System.Runtime.CompilerServices;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;

namespace Sharp.Core.Objects;

internal partial class ConVar : NativeObject, IConVar
{
    private string? _name;

    public string Name => _name ??= _this.GetPtrString(0);

    public string DefaultValue => FormatValue(ref GetDefault());

    public string HelpString => _this.GetPtrString(32);

    public ConVarFlags Flags
    {
        get => (ConVarFlags) _this.GetInt64(48);
        set => _this.WriteInt64(48, (long) value);
    }

    public ConVarType Type => (ConVarType) _this.GetInt16(40);

    public bool GetBool()
        => Get().AsBool;

    public short GetInt16()
        => Get().AsInt16;

    public ushort GetUInt16()
        => Get().AsUInt16;

    public int GetInt32()
        => Get().AsInt32;

    public uint GetUInt32()
        => Get().AsUInt32;

    public long GetInt64()
        => Get().AsInt64;

    public ulong GetUInt64()
        => Get().AsUInt64;

    public float GetFloat()
        => Get().AsFloat;

    public double GetDouble()
        => Get().AsDouble;

    public unsafe ref ConVarVariantValue Get()
    {
        ref var target = ref Unsafe.AsRef<ConVarVariantValue>(nint.Add(_this, 0x58).ToPointer());

        return ref target;
    }

    private unsafe ref ConVarVariantValue GetDefault()
    {
        var pDefault = _this.GetObjectPtr(8);

        if (pDefault == nint.Zero)
        {
            throw new InvalidOperationException($"CVar '{Name}' has no default value");
        }

        return ref Unsafe.AsRef<ConVarVariantValue>(pDefault.ToPointer());
    }

    public string GetString()
        => FormatValue(ref Get());

    private string FormatValue(ref ConVarVariantValue value)
        => Type switch
        {
            ConVarType.Bool    => value.AsBool ? "true" : "false",
            ConVarType.Int16   => value.AsInt16.ToString(CultureInfo.InvariantCulture),
            ConVarType.UInt16  => value.AsUInt16.ToString(CultureInfo.InvariantCulture),
            ConVarType.Int32   => value.AsInt32.ToString(CultureInfo.InvariantCulture),
            ConVarType.UInt32  => value.AsUInt32.ToString(CultureInfo.InvariantCulture),
            ConVarType.Int64   => value.AsInt64.ToString(CultureInfo.InvariantCulture),
            ConVarType.UInt64  => value.AsUInt64.ToString(CultureInfo.InvariantCulture),
            ConVarType.Float32 => value.AsFloat.ToString(CultureInfo.InvariantCulture),
            ConVarType.Float64 => value.AsDouble.ToString(CultureInfo.InvariantCulture),
            ConVarType.String  => value.AsString,
            ConVarType.Color   => FormatColor(value.AsColor32),
            _                  => throw new NotSupportedException($"Unsupported type {Type}"),
        };

    private static string FormatColor(Color32 color)
        => color.A == 255
            ? string.Create(CultureInfo.InvariantCulture, $"{color.R} {color.G} {color.B}")
            : string.Create(CultureInfo.InvariantCulture, $"{color.R} {color.G} {color.B} {color.A}");

    private static string? TypeDefaultString(ConVarType type)
        => type switch
        {
            ConVarType.Color                                                => "0 0 0 255",
            ConVarType.Vector2                                              => "0 0",
            ConVarType.Vector3 or ConVarType.QAngle or ConVarType.VectorWS => "0 0 0",
            ConVarType.Vector4                                              => "0 0 0 0",
            _                                                               => null,
        };

    public void ReplicateToClient(IGameClient client, string value)
        => Cvar.ReplicateToClient(_this, value, client.GetAbsPtr());

    public unsafe void Set(int value)
    {
        if (Type == ConVarType.String)
        {
            throw new
                InvalidOperationException(
                    $"Cannot call Set(int) on CVar '{Name}' (type={Type}). Use Set(string) or SetString(string) instead.");
        }

        var x = new ConVarVariantValue(value);
        Cvar.SetValue(_this, &x);
    }

    public unsafe void Set(bool value)
    {
        if (Type == ConVarType.String)
        {
            throw new
                InvalidOperationException(
                    $"Cannot call Set(bool) on CVar '{Name}' (type={Type}). Use Set(string) or SetString(string) instead.");
        }

        var x = new ConVarVariantValue(value);
        Cvar.SetValue(_this, &x);
    }

    public unsafe void Set(float value)
    {
        if (Type != ConVarType.Float32 && Type != ConVarType.Float64)
        {
            throw new
                InvalidOperationException(
                    $"Cannot call Set(bool) on CVar '{Name}' (type={Type}). Use Set(string) or SetString(string) instead.");
        }

        var x = new ConVarVariantValue(value);
        Cvar.SetValue(_this, &x);
    }

    public void Set(string value)
    {
        if (!Cvar.SetValueString(_this, value))
        {
            throw new ArgumentException($"Cannot parse '{value}' as {Type} for CVar '{Name}'", nameof(value));
        }
    }

    public unsafe void Set(ConVarVariantValue value)
    {
        if (Type == ConVarType.Color)
        {
            Set(FormatValue(ref value));

            return;
        }

        if (Type is ConVarType.Float64 or ConVarType.Vector2 or ConVarType.Vector3 or ConVarType.Vector4 or ConVarType.QAngle or ConVarType.VectorWS)
        {
            throw new
                NotSupportedException(
                    $"Cannot call Set(ConVarVariantValue) on CVar '{Name}' (type={Type}). Use Set(string) instead.");
        }

        Cvar.SetValue(_this, &value);
    }

    public unsafe bool SetMinBound(ConVarVariantValue value)
        => Cvar.SetMinBound(_this, &value);

    public unsafe bool SetMaxBound(ConVarVariantValue value)
        => Cvar.SetMaxBound(_this, &value);

    public unsafe void SetString(string value)
    {
        if (Type == ConVarType.String)
        {
            Span<byte> bytes = stackalloc byte[256];

            bytes.WriteStringUtf8(value);

            fixed (byte* p = bytes)
            {
                var convarValue = new ConVarVariantValue(new IntPtr(p));

                Cvar.SetValue(_this, &convarValue);
            }
        }
        else if (TypeDefaultString(Type) is { } fallback)
        {
            if (!Cvar.SetValueString(_this, value))
            {
                Cvar.SetValueString(_this, fallback);
            }
        }
        else
        {
            var convarValue = Type switch
            {
                ConVarType.Bool => new ConVarVariantValue(value is "1"
                                                          || value.Equals("true", StringComparison.OrdinalIgnoreCase)),
                ConVarType.Int16   => new ConVarVariantValue(short.TryParse(value, CultureInfo.InvariantCulture, out var i16Val) ? i16Val : 0),
                ConVarType.UInt16  => new ConVarVariantValue(ushort.TryParse(value, CultureInfo.InvariantCulture, out var u16Val) ? u16Val : 0),
                ConVarType.Int32   => new ConVarVariantValue(int.TryParse(value, CultureInfo.InvariantCulture, out var i32Val) ? i32Val : 0),
                ConVarType.UInt32  => new ConVarVariantValue(uint.TryParse(value, CultureInfo.InvariantCulture, out var u32Val) ? u32Val : 0),
                ConVarType.Int64   => new ConVarVariantValue(long.TryParse(value, CultureInfo.InvariantCulture, out var i64Val) ? i64Val : 0),
                ConVarType.UInt64  => new ConVarVariantValue(ulong.TryParse(value, CultureInfo.InvariantCulture, out var u64Val) ? u64Val : 0),
                ConVarType.Float32 => new ConVarVariantValue(float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fVal) ? fVal : 0),
                ConVarType.Float64 => new ConVarVariantValue(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dVal) ? (float) dVal : 0),
                _                  => throw new NotSupportedException($"Unsupported type {Type}"),
            };

            Cvar.SetValue(_this, &convarValue);
        }
    }

    internal bool MatchConVar(nint pointer)
        => nint.Add(_this,    0)  == nint.Add(pointer, 0)   // name
           && nint.Add(_this, 40) == nint.Add(pointer, 40); // type
}
