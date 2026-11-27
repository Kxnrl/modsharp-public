# cs_script: C# and Map JavaScript

`ISharedSystem.GetScriptManager()` connects ModSharp to CS2's server-side `cs_script` JavaScript runtime, the one maps run through `point_script` entities. It has nothing to do with CS:GO's Squirrel VScript (`.nut`) or client-side Panorama JS, and the public manager exposes no `RunScript`, string eval, or call-JS-by-name API.

This page targets SDK `2.1.227`. Engine updates can shift V8 compatibility or the set of exposed native script classes, so watch server startup and injection warnings alongside managed exceptions.

## Registering C# methods

Register methods on the game thread (typically inside `Init()`) **before the target `point_script` creates its script context**:

| Registration | JavaScript call |
| --- | --- |
| `RegisterMethod("MyModule_Method", callback)` | Import `Instance` from `"cs_script/point_script"`, then call `Instance.MyModule_Method(...)` |
| `RegisterMethod("Entity", "MyModule_Method", callback)` | Call `entity.MyModule_Method(...)` on an entity wrapper |
| `RegisterMethod("CSPlayerController", "MyModule_Method", callback)` | Call the method on a player-controller wrapper |

The class overload attaches a method to an existing engine JS prototype (subclasses inherit it); it does not declare a new JS class. Pass the JS binding name (`CSPlayerController`), not the C++ name (`CCSPlayerController`). If the class does not exist, registration still returns a managed ID, but injection logs a warning and skips it.

A few things to keep in mind:

- Registrations are keyed by the exact `(className, methodName)` pair. Registering a live name again throws `InvalidOperationException`, even from the same module, so prefix your method names with your module or protocol name. Collision checks only cover ModSharp managed registrations, so avoid reusing Valve or native extension method names.
- Ownership follows the assembly that declares the callback method. Callbacks defined in your module assembly are cleaned up when the module unloads; callbacks defined in a shared helper assembly are not automatically tied to the registering module.
- There is no public `UnregisterMethod`, and the returned `int` is just a registration ID. Unloading clears the managed delegate, but native slots and already-injected prototype methods stay behind; calling them while the module is unloaded throws `managed method dispatch failed`.
- Re-registering an unloaded name reuses its ID, so existing contexts pick up the new delegate. Brand-new names registered **after** a context already exists are not injected retroactively; recreate/reload the script context or reload the map.

Inside your delegate, `call.Script` is the current `point_script` and `call.Caller` is the receiver (the script entity for `Instance` methods, or the receiving entity for class methods). Always check `call.Caller` for `null` in case the entity was removed or JS invoked the method with an unexpected `this`.

ModSharp also injects three built-in helpers into every `point_script` context: `Instance.ModSharpVersion()`, `Instance.SpawnEntityFromKeyValues(classname, kvObject?)`, and `CSPlayerController.GetSteamId()`. It also routes `Console.ServerCommand` (and the deprecated `Instance.ServerCommand`) through `IHookManager.PointServerCommand`.

## Arguments, return values, and errors

Check `ArgCount` before indexing; out-of-range indices throw a managed argument exception. Typed getters do not coerce types (strings will not turn into numbers, and numbers will not turn into booleans). JS `null` and `undefined` return C# `null`; any other mismatched non-null type throws `ScriptThrowException`, except `GetFunction`, which returns `null` for non-function arguments.

| JS input | C# access |
| --- | --- |
| Boolean / number / string | `GetBool`, `GetNumber` (`double`), `GetString` |
| Object with numeric `x`, `y`, `z` | `GetVector` (`Sharp.Shared.Types.Vector`) |
| Entity wrapper | `GetEntity` (validates the entity handle serial) |
| Array, including `[1, 2, 3]` | `GetArray` (numeric arrays are not vectors) |
| Other object | `GetObject` |
| JS function | `GetFunction` (`GetArgType` reports `ScriptValueType.Object`; there is no separate `Function` enum tag) |

Objects with numeric `x`, `y`, `z` properties are matched as vectors before plain objects, so use a different shape if you want a `ScriptObject`. Unsupported JS values like symbols and BigInts marshal as `ScriptValueType.Null` with a native warning.

Delegates return `ScriptReturnValue?`, built from bool, double, string, vector, entity, or a `ScriptObject`/`ScriptArray` created via `NewObject()`/`NewArray()` during **that same call**. Returning C# `null` produces JS `null` (never `undefined`). Never return a handle saved from an earlier call.

To raise a JS `Error` visible to the caller, call `call.ThrowError(message)` or throw `new ScriptThrowException(message)` (typed-getter mismatches take the same path). `ThrowError` aborts the delegate, so do not catch and swallow it. Unexpected C# exceptions are logged server-side and surface in JS only as the generic `managed method dispatch failed`. Wrap bridge calls in `try/catch` in JS to handle expected validation errors.

`SetArg` and `SetArgNull` replace argument slots on the current native callback, which is meant for detour wrappers. They do not reassign the JS caller's local variables, though mutating a received object or array does modify the live JS value.

## Calling JavaScript back

To call back into JS, have JS pass a function into a registered C# method. Call `GetFunction(index)` once to get an `IScriptCallback`, then call `Invoke(...)` on the game thread. Invocation is synchronous, allows re-entry into C#, and you can keep the callback for later game-thread calls.

- Every successful `GetFunction` creates a persistent native handle that you must `Dispose`, even if you call it right away, replace it, or never invoke it. Passing a non-function argument returns `null`.
- `Invoke` enters the owning script's scope, with `this` bound to the JS global object rather than the object the function came from. Pass an arrow function, closure, or explicitly bound function if you need receiver state.
- Pass arguments via `ScriptArgument`: bool, int, double, string, vector, `Null`, `Entity(...)`, and nested `Object(...)`/`Array(...)` descriptions. These construct fresh JS values on each call and cannot carry JS functions.
- Callback return values support bool, number, string, vector, and entity. Returning `null`, `undefined`, an object/array, or a Promise gives C# `null` (there is no Promise-awaiting API).
- JS exceptions thrown inside `Invoke` are caught and logged by the native bridge, and `Invoke` returns `null` rather than throwing in C#. Because `null` can mean a thrown error, an empty return, or an object return, return an explicit scalar acknowledgement if you need to tell them apart and check the server log for details.
- `Owner` re-resolves the owning entity on each access. `IsAvailable` turns false after disposal, owner deletion, or map cleanup. Invoking a dead callback throws `InvalidOperationException` and releases it; invoking a disposed wrapper throws `ObjectDisposedException`. Never carry entity indices or callbacks across map changes.

## Thread and ownership rules

All script operations must stay on the game thread: registration, variable access, liveness checks, invocation, and **callback disposal**. Do not use `Task.Run`, hold call contexts across `await`, or dispose V8 handles from background continuations. Disposing off-thread throws before releasing the handle, so dispatch it back to the game thread.

| Value | Lifetime and cleanup |
| --- | --- |
| `IScriptCallContext` from `RegisterMethod` | Borrowed for the synchronous call only. The framework disposes it; do not store or dispose it yourself. |
| `CreateCallContext(pointer)` | For detours holding a live V8 function-callback-info pointer. Manage with `using` and dispose nested contexts innermost first (LIFO) while the pointer and script scope stay valid. |
| `ScriptObject`, `ScriptArray`, nested values/keys, raw handle IDs | Call-frame-scoped views with no individual `Dispose`. Copying the struct does not clone the JS object or extend its lifetime. Never cache them or mix them across contexts. |
| `IScriptCallback` | Independently owned persistent handle. Dispose it on the game thread on module shutdown and before replacing a cached callback. Owner deletion and map cleanup also reclaim native handles, but do not rely on them instead of disposing explicitly. |

`ScriptObject` provides typed getters/setters plus `Has`, `Keys`, `Count`, `Remove`, and `Clear` over own enumerable string properties only (no prototype lookups). `ScriptArray` provides typed reads, `Set*`, `Push*`, `Insert*`, `Remove`, and `Clear` (removals shift trailing items, and writing past the end can grow the array). Both are live views over JS values rather than deserialized collections. Stick to plain mutable objects and arrays: frozen properties, getters/setters, proxies, and overridden array methods can fail or trigger JS code, and not every native failure becomes a C# exception.

Raw `ScriptValue.String` points to reused unmanaged UTF-8 memory and must be copied before the next marshalling call (including during enumeration); `GetString` already makes that copy for you. Likewise, `ScriptMarshal.Encode` and `EncodeString` reuse a thread-local unmanaged buffer on the managed side, so consume the pointer immediately before another encode and never cache or free it. In normal code, stick to typed setters and `ScriptReturnValue(string)`. Entity references still need regular liveness checks after the call.

Structured values returned from C# stay alive as normal JS objects once written to the JS return slot, but their C# handles expire when the delegate returns. A returned object can share an input array directly, as the example below does.

## Reading and writing module variables

`TryReadVariable(script, name, out value)` and `TryWriteVariable(script, name, value)` work on a running `point_script`. Under the hood they inspect scopes reachable from an engine-registered JS callback (skipping the global scope), so **a script that has not registered any callbacks may not be discoverable** even after its file runs. Register a hook like `OnScriptInput` first and wait until top-level initialization finishes. This is scope inspection only: it does not evaluate JS expressions or see private variables inside imported modules.

| Target | Behavior |
| --- | --- |
| Top-level `let`, `var`, `const` | Reads initialized module-scope bindings. Use `let` or `var` for mutable state. |
| Top-level write | Calls V8's debugger scope setter and returns its bool result. Missing or rejected bindings return `false`; the bridge adds no separate `const` check. Do not rely on `const` for write protection or assume consistent behavior across CS2 builds. |
| `checkpointState.total` | Resolves the root binding, then walks JS properties (`const` on an object binding does not freeze its properties). Missing intermediate objects fail; the final write may create a property if JS allows it. |
| Dotted numeric segment | Paths like `state.items.0` access property `"0"`; bracket syntax and expressions are not supported. |
| Final value is an object/array | Returns `true` with `value == null` (no persistent wrapper is returned). Read scalar leaves instead, or pass the object to a registered method. |
| Missing root / unavailable scope | Returns `false`. A valid path whose leaf is `null`/`undefined`, or a missing final property on an existing object, can return `true` with `null`, so check both the bool result and the value. |

Writes accept `ScriptReturnValue` (scalar, vector, or entity; `default(ScriptReturnValue)` writes JS `null`). You cannot pass a `ScriptArgument` tree or reuse a call-scoped handle to replace a whole object; pass structured data through callbacks or mutate the object inside the call that received it. Dotted paths use regular JS property access and may trigger getters and setters.

## Complete checkpoint-score example

In this example, the map sends positive checkpoint awards, C# validates them, reads the map's score limit, updates a scalar property, calls JS back for acknowledgement, and returns a structured receipt. This is map/server cooperation, not a transactional boundary: the score is updated before the notification callback runs.

Create a separate module project following [Writing a Module](../guides/writing-a-module.md): `net10.0`, C# 13, x64, nullable enabled, implicit usings disabled, `EnableDynamicLoading=true`, and exact `ModSharp.Sharp.Shared` `[2.1.227]` with `PrivateAssets="all" ExcludeAssets="runtime"`. Set `AssemblyName` to `CheckpointBridge`, replace the generated class, and deploy the output to `game/sharp/modules/CheckpointBridge/`. Keep only this single `IModSharpModule` in the project.

[!code-csharp[Checkpoint C# bridge](../../codes/cs-script.cs)]

This module retains no entities, listeners, or callbacks. The `using` block on the callback disposes its persistent handle on every exit path, and the host removes the registered delegate when the module unloads.

### Map resource and loading order

1. Install CS2 Workshop Tools, create or open an addon, and save the script below to `{CS2}/content/csgo_addons/<addon>/scripts/modsharp_docs_checkpoint.js` (an addon source asset, not something under `sharp/modules` or Panorama).
2. In Hammer, place a `point_script`, set `targetname` to `modsharp_docs_checkpoint`, and pick `scripts/modsharp_docs_checkpoint.vjs` in the `cs_script` asset browser (the source file is `.js`, while Hammer lists the compiled `.vjs`).
3. Build and run the map in Workshop Tools. On a dedicated server, package, publish, and mount the compiled map and its script dependency through the normal Workshop flow; dropping only the C# DLL or a loose `.js` file under `sharp` will not work.
4. Make sure ModSharp and `CheckpointBridge` load **before the map creates its script context**. A plain Workshop Tools session without ModSharp can run the JS file, but `Instance.ModSharpDocs_CheckpointAwardV1` will not be present.
5. Wire two test buttons' `OnPressed` outputs to the `point_script` with input `RunScriptInput` and parameters `award` and `invalid_award`. Those strings select registered JS callbacks rather than executing raw code.

[Download the map JavaScript](../../codes/cs-script.js).

[!code-javascript[Checkpoint map script](../../codes/cs-script.js)]

On map start, JS logs `Ready`. Triggering `award` calls C#, updates `checkpointState.total`, calls back into `acknowledgeAward`, and logs a receipt with `awarded=25` and `acknowledged=true`. Triggering `invalid_award` logs a validation error without touching the score or count; awards above the limit are rejected the same way.

### Reloading and caveats

Replacing the module DLL does not reload the map's JS. Following the [module reload guide](../guides/reload-module.md), existing method names reconnect to the new delegate, while newly added method names require recreating the script context. Re-running the map in Workshop Tools picks up JS edits; tool-mode script hot reload runs on the engine's separate `OnScriptReload` lifecycle rather than ModSharp module reload. This sample does not persist state across script re-evaluation, so top-level totals reset. For a clean reset, load the module first and then reload the map. If you cache JS callbacks, dispose and reacquire them around script reinitialization instead of assuming an old callback still points to the latest closure just because the entity survived.

Because Node and browsers do not provide `cs_script/point_script`, test method injection, VJS packaging, variable reads/writes, callbacks, and reloads on a real CS2 server with a map compiled in Workshop Tools.

## References

- Valve: [CS2 scripting overview](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting), [source asset and Hammer setup](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting/Hello_Gordon), [point_script entity/input](https://developer.valvesoftware.com/wiki/Point_script_(Source_2)), and [current scripting API](https://developer.valvesoftware.com/wiki/Counter-Strike_2_Workshop_Tools/Scripting_API) (this example uses `Console.Log` from the API reference instead of `Instance.Msg` from the older tutorial).
- ModSharp source: [ScriptManager contract](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/ScriptManager.cs), [managed dispatch/cleanup](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Managers/ScriptManager.cs), [native injection](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/hook/cs_script.cpp), [argument/object natives](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/bridge/natives/ScriptNatives.cpp), and [V8 callback/variable implementation](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/v8proxy.cpp).
