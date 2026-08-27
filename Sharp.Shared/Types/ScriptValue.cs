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

using System.Runtime.InteropServices;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Sharp.Shared.Types;

/// <summary>
///     mirrors the C++ <c>ScriptValue</c> (Engine/src/cstrike/type/ScriptValue.h) <br />
///     Byte layout is fixed and MUST match. Blittable only — <see cref="String" /> is a raw UTF8
///     <c>byte*</c> into a reused native thread-local buffer, never a managed string.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public unsafe struct ScriptValue
{
    [FieldOffset(0)]
    public ScriptValueType Type;

    [FieldOffset(8)]
    public bool Bool;

    [FieldOffset(8)]
    public double Number;

    /// <summary>
    ///     Raw UTF8 pointer into a reused native thread-local buffer. Valid ONLY until the next cs_script
    ///     marshalling call on this thread (the next ArrGet/ObjGet/MarshalArg/InvokeCallback overwrites it) —
    ///     NOT for the whole call. Copy it immediately (e.g. <see cref="Utilities.NativeString.ReadString(byte*)" />);
    ///     the typed <c>GetString</c> accessors already do this and are the safe way to read a string.
    /// </summary>
    [FieldOffset(8)]
    public byte* String;

    [FieldOffset(8)]
    public uint Entity;

    [FieldOffset(8)]
    public uint Handle;

    [FieldOffset(8)]
    public Vector Vector;
}

public record struct ScriptReturnValue
{
    public ScriptValueType Type   { get; }
    public bool?           Bool   { get; }
    public double?         Number { get; }
    public string?         String { get; }
    public IBaseEntity?    Entity { get; }
    public Vector?         Vector { get; }
    public uint?           Handle { get; }

    public ScriptReturnValue(bool value)
    {
        Type = ScriptValueType.Bool;
        Bool = value;
    }

    public ScriptReturnValue(double value)
    {
        Type   = ScriptValueType.Number;
        Number = value;
    }

    public ScriptReturnValue(string value)
    {
        Type   = ScriptValueType.String;
        String = value;
    }

    public ScriptReturnValue(IBaseEntity value)
    {
        Type   = ScriptValueType.Entity;
        Entity = value;
    }

    public ScriptReturnValue(Vector value)
    {
        Type   = ScriptValueType.Vector;
        Vector = value;
    }

    public ScriptReturnValue(ScriptObject value)
    {
        Type   = ScriptValueType.Object;
        Handle = value.Handle;
    }

    public ScriptReturnValue(ScriptArray value)
    {
        Type   = ScriptValueType.Array;
        Handle = value.Handle;
    }
}
