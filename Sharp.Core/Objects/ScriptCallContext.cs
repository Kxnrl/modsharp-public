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
using System.Diagnostics.CodeAnalysis;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.CStrike;
using Sharp.Core.GameEntities;
using Sharp.Core.Managers;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Helpers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Native = Sharp.Core.Bridges.Natives.Script;

namespace Sharp.Core.Objects;

internal sealed unsafe class ScriptCallContext : ContextObject, IScriptCallContext
{
    private readonly nint               _info;
    private readonly IBaseEntity        _script;
    private readonly IBaseEntity?       _caller;
    private readonly int                _argc;
    private readonly ICoreScriptManager _manager;

    // Dispose's handle-table handling has two paths:
    //   >= 0: CreateCallContext recorded the entry base index via EnableHandleFrame; Dispose truncates back to it (stack-style frame).
    //   -1 : the Dispatch (managed-method) path, reclaimed by the C++ ManagedTrampoline's ScopedHandleFrame; here we must NEVER touch
    //        the handle table, or we would truncate it before the trampoline resolves the return handle.
    private int _handleFrameBase = -1;

    // This frame's unique id + a per-thread stack of "currently-open CreateCallContext frame" ids, used to enforce LIFO on Dispose (see Dispose).
    private long _handleFrameId;

    [ThreadStatic]
    private static long _nextHandleFrameId;

    [ThreadStatic]
    private static Stack<long>? _openHandleFrames;

    public ScriptCallContext(nint info, IBaseEntity script, IBaseEntity? caller, int argc, ICoreScriptManager manager)
    {
        _info    = info;
        _script  = script;
        _caller  = caller;
        _argc    = argc;
        _manager = manager;
    }

    // Called only on the hook path (CreateCallContext): records the handle-table base index on entry to this call; truncated back on Dispose.
    public void EnableHandleFrame(int baseIndex)
    {
        _handleFrameBase = baseIndex;
        _handleFrameId   = ++_nextHandleFrameId;
        (_openHandleFrames ??= new Stack<long>()).Push(_handleFrameId);
    }

    public IBaseEntity Script
    {
        get
        {
            CheckDisposed();

            return _script;
        }
    }

    public IBaseEntity? Caller
    {
        get
        {
            CheckDisposed();

            return _caller;
        }
    }

    public int ArgCount
    {
        get
        {
            CheckDisposed();

            return _argc;
        }
    }

    public ScriptValueType GetArgType(int index)
        => ReadArg(index).Type;

    public bool? GetBool(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Bool => sv.Bool,
            ScriptValueType.Null => null,
            _                    => throw TypeMismatch(index, ScriptValueType.Bool, sv.Type),
        };
    }

    public double? GetNumber(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Number => sv.Number,
            ScriptValueType.Null   => null,
            _                      => throw TypeMismatch(index, ScriptValueType.Number, sv.Type),
        };
    }

    public string? GetString(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.String => sv.String is not null ? Shared.Utilities.NativeString.ReadString(sv.String) : null,
            ScriptValueType.Null   => null,
            _                      => throw TypeMismatch(index, ScriptValueType.String, sv.Type),
        };
    }

    public Vector? GetVector(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Vector => sv.Vector,
            ScriptValueType.Null   => null,
            _                      => throw TypeMismatch(index, ScriptValueType.Vector, sv.Type),
        };
    }

    public IBaseEntity? GetEntity(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Entity => BaseEntity.Create(Entity.FindByEHandle(sv.Entity)),
            ScriptValueType.Null   => null,
            _                      => throw TypeMismatch(index, ScriptValueType.Entity, sv.Type),
        };
    }

    public IScriptCallback? GetFunction(int index)
    {
        CheckDisposed();

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _argc);

        // 0 = not a function / owner unresolved.
        var id = Native.CreateCallback(_info, index);

        return id == 0 ? null : new ScriptCallback(_manager, id, _script.Handle.GetValue());
    }

    // ---- Object / Array high-level entry points --------------------------------------------------

    public ScriptObject? GetObject(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Object => new ScriptObject(this, sv.Handle),
            ScriptValueType.Null   => null,
            _                      => throw TypeMismatch(index, ScriptValueType.Object, sv.Type),
        };
    }

    public ScriptArray? GetArray(int index)
    {
        var sv = ReadArg(index);

        return sv.Type switch
        {
            ScriptValueType.Array => new ScriptArray(this, sv.Handle),
            ScriptValueType.Null  => null,
            _                     => throw TypeMismatch(index, ScriptValueType.Array, sv.Type),
        };
    }

    public ScriptObject NewObject()
    {
        CheckDisposed();

        return new ScriptObject(this, Native.NewObject());
    }

    public ScriptArray NewArray()
    {
        CheckDisposed();

        return new ScriptArray(this, Native.NewArray());
    }

    public ScriptValue ObjGet(uint handle, string key)
    {
        CheckDisposed();

        var sv = default(ScriptValue);
        Native.ObjGet(handle, key, &sv);

        return sv;
    }

    public void ObjSet(uint handle, string key, in ScriptValue value)
    {
        CheckDisposed();

        var v = value;
        Native.ObjSet(handle, key, &v);
    }

    public bool ObjHas(uint handle, string key)
    {
        CheckDisposed();

        return Native.ObjHas(handle, key);
    }

    public uint ObjKeys(uint handle)
    {
        CheckDisposed();

        return Native.ObjKeys(handle);
    }

    public bool ObjDelete(uint handle, string key)
    {
        CheckDisposed();

        return Native.ObjDelete(handle, key);
    }

    public int ObjCount(uint handle)
    {
        CheckDisposed();

        return Native.ObjCount(handle);
    }

    public void ObjClear(uint handle)
    {
        CheckDisposed();

        Native.ObjClear(handle);
    }

    public int ArrLength(uint handle)
    {
        CheckDisposed();

        return Native.ArrLength(handle);
    }

    public ScriptValue ArrGet(uint handle, int index)
    {
        CheckDisposed();

        var sv = default(ScriptValue);
        Native.ArrGet(handle, index, &sv);

        return sv;
    }

    public void ArrSet(uint handle, int index, in ScriptValue value)
    {
        CheckDisposed();

        var v = value;
        Native.ArrSet(handle, index, &v);
    }

    public void ArrPush(uint handle, in ScriptValue value)
    {
        CheckDisposed();

        var v = value;
        Native.ArrPush(handle, &v);
    }

    public bool ArrRemove(uint handle, int index)
    {
        CheckDisposed();

        return Native.ArrRemove(handle, index);
    }

    public void ArrClear(uint handle)
    {
        CheckDisposed();

        Native.ArrClear(handle);
    }

    public bool ArrInsert(uint handle, int index, in ScriptValue value)
    {
        CheckDisposed();

        var v = value;

        return Native.ArrInsert(handle, index, &v);
    }

    public void SetArgNull(int index)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Null });

    public void SetArg(int index, bool value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Bool, Bool = value });

    public void SetArg(int index, double value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Number, Number = value });

    public void SetArg(int index, string value)
        => SetArgCore(index, Shared.Utilities.ScriptMarshal.EncodeString(value));

    public void SetArg(int index, Vector value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Vector, Vector = value });

    public void SetArg(int index, IBaseEntity value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Entity, Entity = value.Handle.GetValue() });

    public void SetArg(int index, ScriptObject value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Object, Handle = value.Handle });

    public void SetArg(int index, ScriptArray value)
        => SetArgCore(index, new ScriptValue { Type = ScriptValueType.Array, Handle = value.Handle });

    private void SetArgCore(int index, ScriptValue value)
    {
        CheckDisposed();

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _argc);

        Native.SetArg(_info, index, &value);
    }

    private ScriptValue ReadArg(int index)
    {
        CheckDisposed();

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _argc);

        var sv = default(ScriptValue);

        Native.MarshalArg(_info, index, &sv);

        return sv;
    }

    // Must be a ScriptThrowException: only it is turned by Dispatch into ret.Error and carried back to the script as a JS Error; a plain exception would fall back to the generic failure.
    private static ScriptThrowException TypeMismatch(int index, ScriptValueType expected, ScriptValueType actual)
        => new ($"cs_script arg {index}: expected {expected} or null, got {actual}");

    [DoesNotReturn]
    public void ThrowError(string message)
    {
        CheckDisposed();

        throw new ScriptThrowException(message);
    }

    public IBaseEntity? ResolveEntity(in ScriptValue value)
    {
        CheckDisposed();

        return BaseEntity.Create(Entity.FindByEHandle(value.Entity));
    }

    protected override void OnDisposed()
    {
        base.OnDisposed();

        Dispose();
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        // See _handleFrameBase: >= 0 is the CreateCallContext path — truncate back the handle segment recorded on entry; the Dispatch path
        // is -1, left to the C++ ManagedTrampoline's ScopedHandleFrame to reclaim, so we do not touch the handle table here.
        if (_handleFrameBase >= 0)
        {
            // The handle table is a single thread-local LIFO stack: truncating back to this frame's base is only safe when this frame is on top.
            // Out-of-order release (freeing c1 while c2 is still open) would drop c2's live handles too, so we enforce LIFO by frame id and fail-fast on a violation.
            var frames = _openHandleFrames;
            if (frames is null || frames.Count == 0 || frames.Peek() != _handleFrameId)
            {
                throw new InvalidOperationException(
                    "cs_script CreateCallContext contexts must be disposed in LIFO order (innermost first).");
            }

            frames.Pop();
            Native.HandleFrameEnd(_handleFrameBase);
        }

        MarkAsDisposed();
    }
}
