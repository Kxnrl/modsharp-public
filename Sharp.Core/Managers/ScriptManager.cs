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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Extensions.Logging;
using Sharp.Core.Bridges.Natives;
using Sharp.Core.GameEntities;
using Sharp.Core.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.Helpers;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Utilities;
using Forward = Sharp.Core.Bridges.Forwards.Script;
using Native = Sharp.Core.Bridges.Natives.Script;
using ScriptCallback = Sharp.Shared.Managers.IScriptManager.ScriptMethodDelegate;

namespace Sharp.Core.Managers;

internal interface ICoreScriptManager : IScriptManager
{
    void OnModuleUnload(Assembly assembly);

    bool InvokeCallback(uint id, ReadOnlySpan<ScriptArgument> args, out ScriptReturnValue? result);

    void ReleaseCallback(uint id);

    bool IsCallbackAlive(uint id);
}

internal sealed class ScriptManager : ICoreScriptManager
{
    private readonly record struct MethodEntry(ScriptCallback Callback, Assembly Assembly);

    private readonly ILogger<ScriptManager>                                 _logger;
    private readonly Dictionary<int, MethodEntry>                           _registry;
    private readonly Dictionary<(string ClassName, string MethodName), int> _index;
    private readonly int                                                    _gameThreadId;

    private int _nextId;

    public unsafe ScriptManager(ILogger<ScriptManager> logger)
    {
        _logger       = logger;
        _registry     = [];
        _index        = [];
        _gameThreadId = Environment.CurrentManagedThreadId;

        Forward.OnScriptMethodCall += Dispatch;
    }

    public void OnModuleUnload(Assembly assembly)
    {
        // Release callbacks here; otherwise we keep calling the wrong delegate and never free them

        var alc = AssemblyLoadContext.GetLoadContext(assembly);

        var dead = _registry
                   .Where(kv => AssemblyLoadContext.GetLoadContext(kv.Value.Assembly) == alc)
                   .Select(kv => kv.Key)
                   .ToArray();

        foreach (var id in dead)
        {
            _registry.Remove(id);
        }
    }

#region IScriptManager

    public int RegisterMethod(string methodName, ScriptCallback callback)
        => RegisterMethod("Domain", methodName, callback);

    public int RegisterMethod(string className, string methodName, ScriptCallback callback)
    {
        EnsureGameThread();

        var asm = callback.Method.Module.Assembly;
        var key = (className, methodName);

        if (_index.TryGetValue(key, out var id))
        {
            if (_registry.ContainsKey(id))
            {
                throw new InvalidOperationException($"Method '{className}.{methodName}' is already registered");
            }

            // Dead slot => reload / taking over a freed name. Reuse the id.
            _registry[id] = new MethodEntry(callback, asm);

            return id;
        }

        id            = _nextId++;
        _index[key]   = id;
        _registry[id] = new MethodEntry(callback, asm);

        Native.RegisterMethod(className, methodName, id);

        return id;
    }

    public IScriptCallContext CreateCallContext(nint pointer)
    {
        EnsureGameThread();

        var frameBase = Native.HandleFrameBegin();
        var context   = BuildContext(pointer);
        context.EnableHandleFrame(frameBase);

        return context;
    }

#endregion

    public unsafe bool InvokeCallback(uint id, ReadOnlySpan<ScriptArgument> args, out ScriptReturnValue? result)
    {
        result = null;

        EnsureGameThread();

        // Flatten args into one flat pre-order descriptor (nodes + a string blob) and hand it to the
        // engine in a single native call. Two passes so the buffers can come from ArrayPool at exact
        // size: pass 1 counts, pass 2 fills. Rented per invoke — a nested invoke rents its own (the
        // pool never re-hands an un-returned array), so re-entrancy can't clobber ours.
        var nodeCount   = 0;
        var stringBytes = 0;

        foreach (ref readonly var arg in args)
        {
            CountArg(in arg, ref nodeCount, ref stringBytes);
        }

        // Rent >= 1 so an empty descriptor still yields a pinnable array (native reads nodeCount = 0).
        var nodes = ArrayPool<ScriptValue>.Shared.Rent(Math.Max(1, nodeCount));
        var blob  = ArrayPool<byte>.Shared.Rent(Math.Max(1,        stringBytes));

        try
        {
            var ret = default(ScriptValue);

            fixed (byte* blobPtr = blob)
            {
                // Pass 2: emit nodes and encode strings straight into the pinned blob, writing each
                // String node's pointer directly — no separate offset table / fixup step.
                var nodeIndex  = 0;
                var blobOffset = 0;

                foreach (ref readonly var arg in args)
                {
                    WriteArg(in arg, nodes, ref nodeIndex, blobPtr, blob.Length, ref blobOffset);
                }

                fixed (ScriptValue* nodePtr = nodes)
                {
                    var invoked = Native.InvokeCallback(id, nodePtr, nodeCount, args.Length, &ret);

                    if (invoked)
                    {
                        result = ReadReturn(in ret);
                    }

                    return invoked;
                }
            }
        }
        finally
        {
            // No clearArray: only [0, nodeCount) is written and read; stale slots are never touched.
            ArrayPool<ScriptValue>.Shared.Return(nodes);
            ArrayPool<byte>.Shared.Return(blob);
        }
    }

    public void ReleaseCallback(uint id)
    {
        // Release resets a v8::Global (touches the isolate) and mutates the native callback map — both
        // main-thread only, so Dispose must run on the main thread too. Guarded before the native call
        // so an off-thread Dispose throws with the handle still intact (retry on the main thread).
        EnsureGameThread();

        Native.ReleaseCallback(id);
    }

    public bool IsCallbackAlive(uint id)
    {
        // Native IsManagedCallbackAlive touches the (main-thread-only) callback map + entity system, same
        // as Invoke/Release. Guard here too so IScriptCallback.IsAvailable/Owner fail fast off-thread
        // instead of racing the game thread's try_emplace/erase.
        EnsureGameThread();

        return Native.IsCallbackAlive(id);
    }

    private unsafe bool Dispatch(int managedId, nint info, ScriptValue* ret)
    {
        ret->Type = ScriptValueType.Null;
        ScriptCallContext? call = null;

        try
        {
            if (!_registry.TryGetValue(managedId, out var entry))
            {
                return false;
            }

            call = BuildContext(info);

            var result = entry.Callback(call);

            WriteReturn(result, ret);

            return true;
        }
        catch (ScriptThrowException ex)
        {
            ret->Type   = ScriptValueType.Error;
            ret->String = ScriptMarshal.Encode(ex.Message);

            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "cs_script managed method (id={Id}) throw an error.", managedId);

            return false;
        }
        finally
        {
            call?.Dispose();
        }
    }

    private ScriptCallContext BuildContext(nint info)
    {
        var script = BaseEntity.Create(Native.GetScript())
                     ?? throw new InvalidOperationException("cs_script: current-script is null (not inside a script call?)");

        var caller = BaseEntity.Create(Native.GetCaller(info));

        return new ScriptCallContext(info, script, caller, Native.GetArgCount(info), this);
    }

    private void EnsureGameThread()
    {
        if (Environment.CurrentManagedThreadId != _gameThreadId)
        {
            throw new InvalidOperationException("cs_script callbacks must be used on the game thread.");
        }
    }

    private static unsafe ScriptReturnValue? ReadReturn(in ScriptValue value)
        => value.Type switch
        {
            ScriptValueType.Bool   => new ScriptReturnValue(value.Bool),
            ScriptValueType.Number => new ScriptReturnValue(value.Number),
            ScriptValueType.String => value.String is not null
                ? new ScriptReturnValue(NativeString.ReadString(value.String))
                : null,
            ScriptValueType.Vector => new ScriptReturnValue(value.Vector),
            ScriptValueType.Entity => BaseEntity.Create(Entity.FindByEHandle(value.Entity)) is { } entity
                ? new ScriptReturnValue(entity)
                : null,
            _ => null,
        };

    private static unsafe void WriteReturn(ScriptReturnValue? result, ScriptValue* ret)
    {
        // null maps straight to V8::Null; undefined likewise
        if (result is not { } value)
        {
            ret->Type = ScriptValueType.Null;

            return;
        }

        switch (value.Type)
        {
            case ScriptValueType.Bool:
                ret->Type = ScriptValueType.Bool;
                ret->Bool = value.Bool ?? false;

                break;
            case ScriptValueType.Number:
                ret->Type   = ScriptValueType.Number;
                ret->Number = value.Number ?? 0.0;

                break;
            case ScriptValueType.String when value.String is { } s:
                ret->Type   = ScriptValueType.String;
                ret->String = ScriptMarshal.Encode(s);

                break;
            case ScriptValueType.Vector:
                ret->Type   = ScriptValueType.Vector;
                ret->Vector = value.Vector ?? default;

                break;
            case ScriptValueType.Entity when value.Entity is { } entity:
                ret->Type   = ScriptValueType.Entity;
                ret->Entity = entity.Handle.GetValue();

                break;
            case ScriptValueType.Object when value.Handle is { } objectHandle:
                ret->Type   = ScriptValueType.Object;
                ret->Handle = objectHandle;

                break;
            case ScriptValueType.Array when value.Handle is { } arrayHandle:
                ret->Type   = ScriptValueType.Array;
                ret->Handle = arrayHandle;

                break;
            default:
                ret->Type = ScriptValueType.Null;

                break;
        }
    }

#region Callback Helper

    // Pass 1 — size the descriptor: node count + total UTF8 string bytes
    // (include NUL per string, and object keys). Must traverse identically to WriteArg below.
    private static void CountArg(in ScriptArgument arg, ref int nodes, ref int stringBytes)
    {
        switch (arg.Type)
        {
            case ScriptValueType.String:
                nodes++;
                stringBytes += Encoding.UTF8.GetByteCount(arg.AsString ?? string.Empty) + 1;

                break;
            case ScriptValueType.Object:
                nodes++;

                foreach (ref readonly var field in arg.AsObject)
                {
                    nodes++;
                    stringBytes += Encoding.UTF8.GetByteCount(field.Key ?? string.Empty) + 1;
                    CountArg(in field.Value, ref nodes, ref stringBytes);
                }

                break;
            case ScriptValueType.Array:
                nodes++;

                foreach (ref readonly var element in arg.AsArray)
                {
                    CountArg(in element, ref nodes, ref stringBytes);
                }

                break;
            default:
                nodes++;

                break;
        }
    }

    // Pass 2 — emit one arg into the pre-order node stream. Object/Array carry their child count in the
    // uint slot; an object field is a (key-String, value) pair. Strings go straight into the pinned
    // blob with their pointer written into the node. Mirrors BuildScriptArgValue in cs_script.cpp and
    // must traverse identically to CountArg above.
    private static unsafe void WriteArg(in ScriptArgument arg,
        ScriptValue[]                                     nodes,
        ref int                                           index,
        byte*                                             blob,
        int                                               blobLength,
        ref int                                           blobOffset)
    {
        switch (arg.Type)
        {
            case ScriptValueType.Bool:
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Bool, Bool = arg.AsBool };

                break;
            case ScriptValueType.Number:
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Number, Number = arg.AsNumber };

                break;
            case ScriptValueType.Vector:
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Vector, Vector = arg.AsVector };

                break;
            case ScriptValueType.Entity:
                nodes[index++] = new ScriptValue
                {
                    Type = ScriptValueType.Entity, Entity = arg.AsEntity?.Handle.GetValue() ?? uint.MaxValue,
                };

                break;
            case ScriptValueType.String:
                nodes[index++] = new ScriptValue
                {
                    Type = ScriptValueType.String, String = WriteString(arg.AsString, blob, blobLength, ref blobOffset),
                };

                break;
            case ScriptValueType.Object:
            {
                var fields = arg.AsObject;
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Object, Handle = (uint) fields.Length };

                foreach (ref readonly var field in fields)
                {
                    nodes[index++] = new ScriptValue
                    {
                        Type = ScriptValueType.String, String = WriteString(field.Key, blob, blobLength, ref blobOffset),
                    };

                    WriteArg(in field.Value, nodes, ref index, blob, blobLength, ref blobOffset);
                }

                break;
            }
            case ScriptValueType.Array:
            {
                var elements = arg.AsArray;
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Array, Handle = (uint) elements.Length };

                foreach (ref readonly var element in elements)
                {
                    WriteArg(in element, nodes, ref index, blob, blobLength, ref blobOffset);
                }

                break;
            }
            default:
                nodes[index++] = new ScriptValue { Type = ScriptValueType.Null };

                break;
        }
    }

    // Encode one string as NUL-terminated UTF8 into the pinned blob at the running offset; return a
    // pointer to it. The blob was sized by CountArg, so the write always fits.
    private static unsafe byte* WriteString(string? value, byte* blob, int blobLength, ref int blobOffset)
    {
        var start   = blobOffset;
        var written = Encoding.UTF8.GetBytes(value ?? string.Empty, new Span<byte>(blob + blobOffset, blobLength - blobOffset));
        blob[blobOffset + written] =  0;
        blobOffset                 += written + 1;

        return blob + start;
    }

#endregion
}
