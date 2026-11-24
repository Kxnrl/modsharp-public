# GameData

GameData keeps native memory addresses, byte offsets, and vtable slots out of your C# source. Call `sharedSystem.GetModSharp().GetGameData()` to get the host-wide shared `IGameData` instance. It is a single shared registry across all modules; it does not access schema fields or declare function signatures.

Use built-in keys whenever they cover what you need. Only register a custom file for missing targets, and prefix every custom key with your module name. If you just need a direct export lookup or an exploratory scan, see [Native Library Modules](library-modules.md). Once you have an address, you still need the matching ABI to call or hook it; see [Native Call](../examples/native-call.md) and [Native Hook](../examples/native-hook.md).

## Built-in data and lookup APIs

Before loading managed modules, the host loads five built-in files from `game/sharp/gamedata`:

| File | Examples |
| --- | --- |
| `core.games.jsonc` | `INetChannel::SendNetMessage`, `IScriptVM::CreateVM`, shared interface vtable slots |
| `tier0.games.jsonc` | `CCommand::Tokenize`, `CVProfile::OutputReport` |
| `engine.games.jsonc` | Network server/client addresses, `CServerSideClient::m_SignonState`, engine vtable slots |
| `server.games.jsonc` | Entity functions, `g_pPhysicsQuery`, `CBaseEntity::CollisionRulesChanged` |
| `log.games.jsonc` | `LogDirect`, `LoggingSystem_IsChannelEnabled`, and its C++ overload |

Some native startup resolvers override built-in offsets, indices, or addresses after loading these files. Always query the API instead of hardcoding numbers from the JSONC assets, and never register or unregister those five host-owned files yourself.

Public `IGameData` methods:

| API | Description |
| --- | --- |
| `bool GetOffset(string name, out int offset)` | Look up a byte offset in `Offsets`. |
| `bool GetAddress(string name, out nint address)` | Resolve and cache an address in `Addresses`; returns `false` and zero on failure (native diagnostics may still be logged). |
| `bool GetVFuncIndex(string name, out int index)` | Look up a zero-based vtable slot index in `VFuncs` (not a byte offset). |
| `int GetOffset(string name)` | Return the byte offset, or throw `KeyNotFoundException`. |
| `nint GetAddress(string name)` | Return the resolved address, or throw `KeyNotFoundException`. |
| `int GetVFuncIndex(string name)` | Return the vtable slot index, or throw `KeyNotFoundException`. |
| `GetOffset(classname, name)`, `GetAddress(classname, name)`, `GetVFuncIndex(classname, name)` | Look up `classname::name`, then walk schema base classes. Requires valid schema class metadata rather than plain string concatenation. |
| `void Register(string path)` / `void Unregister(string path)` | Add or remove a custom file's entries in the shared registry. |

Namespaced keys like `GameDataExample::LoggingSystem_IsChannelEnabled` are not schema classes, so query them with the single-string overload.

## The `games.jsonc` format

The root is a JSON object with optional, case-sensitive `Addresses`, `Offsets`, `VFuncs`, and `Patches` sections (no outer `Games`, `Signatures`, or `Library` wrapper). JSON comments are allowed, but trailing commas are not. Unknown property names are not reliably rejected, so double-check your field spelling.

Each section maps keys to entry objects:

| Section | Fields |
| --- | --- |
| `Offsets` | `"windows"` and/or `"linux"` with signed 32-bit JSON integers (unquoted), giving byte offsets in a known struct/object layout. |
| `VFuncs` | Same shape as `Offsets`, but values are zero-based vtable **slot indices**. On x64 the byte offset inside a vtable is `index * 8`; store the slot index, not the multiplied offset. |
| `Addresses` | Common fields like lowercase `"library"`, plus `"windows"`/`"linux"` strings or platform objects (below). |
| `Patches` | `"address"` references an address key; platform objects hold a hex-byte `"patch"` string and optional `"validate"`. Registration patches live memory immediately, and unregistration restores the original bytes. |

Only the current platform is loaded. Integer entries missing the current platform are skipped; reference-based addresses can still resolve without a platform signature. If a file yields no usable entries for the current platform, registration fails.

### Libraries and platform entries

`Addresses[key].library` expects one of these exact lowercase identifiers, not a DLL/SO filename:

```text
engine, server, tier0, schemasystem, resourcesystem, vscript, vphysics2,
soundsystem, networksystem, worldrenderer, matchmaking, filesystem,
steamsockets, materialsystem2, animationsystem
```

These names do not always match the managed library manager properties: for example, GameData uses `schemasystem`, `resourcesystem`, `soundsystem`, and `networksystem`, whereas C# code uses `Schema`, `Resource`, `SoundSystem`, and `NetworkSystem`.

Each platform entry (`"windows"` / `"linux"`) can be either:

- **A string**: an IDA-style byte pattern, or `@` followed by an exported symbol name without spaces (`@` triggers export lookup instead of byte scanning).
- **An object**: `"signature"` for that same string, `"factory"` for address-transform operations, and an optional `"base"` overriding the entry's common base key.

Provide separate platform strings whenever Windows and Linux machine code or mangled C++ names differ. The example below uses the real, unmangled `LoggingSystem_IsChannelEnabled` export on both platforms (copied from baseline `log.games.jsonc` with a renamed key):

[!code-json[GameData configuration](../../codes/gamedata-example.games.jsonc)]

[Download the configuration](../../codes/gamedata-example.games.jsonc). CS2 updates can change exports and signatures at any time, so keep custom GameData files aligned with your server and ModSharp version.

### Patterns, offsets, and pointers are different things

Byte patterns use space-separated hex bytes with `?` or `??` for whole-byte wildcards (nibble wildcards like `A?` are not supported). Trailing wildcards are trimmed automatically. GameData requires **exactly one match** in executable segments; zero or multiple matches fail resolution.

The initial scan points at the first matched instruction byte; it will not follow calls or dereference globals on its own. To derive the final address, add a `factory` string to the platform object; operations run left to right on the current address `A`:

| Operation | Effect on `A` |
| --- | --- |
| `+N` / `-N` | Add or subtract decimal bytes `N` (write `factory` as a string; numeric JSON values are accepted only with a warning). |
| `r` | Read a signed 32-bit relative displacement at `A` and set `A = A + 4 + displacement`. Advance `A` to the displacement field first. |
| `d` | Read the native pointer stored at `A` and use it as the new address (single dereference). |

For a real example, `g_pPhysicsQuery` in baseline `server.games.jsonc` uses `factory: "+3 r d"`: move 3 bytes to the RIP-relative displacement, resolve the global's storage location with `r`, then read the interface pointer stored there with `d` (its Windows pattern starts with `48 8B 0D`; Linux starts with `48 8D 05`). The result is the physics-query interface pointer, so use the built-in `g_pPhysicsQuery` key rather than copying its patterns.

There are no generic `offset` or `pointer` fields in place of `factory`. Factory reads hit raw native memory without pointer validation, so a bad step will crash the server. Likewise, a nonzero result from `GetAddress` only means an address was resolved; you are still responsible for matching the target's real type, lifetime, and calling convention.

### Reference-based and advanced entries

`Addresses` entries also support:

- `"refs"`: locate functions via references (`strings`, `cvars`, `vtables`, each a string or string array). Singular `vtable` restricts candidates to virtual functions of a specific class and differs from `vtables`.
- `"vscript"`: a native VScript binding name, supported only with `library: "server"` (not a `cs_script` filename).
- `"on_demand": true`: defer address resolution until `GetAddress` is called instead of resolving during `Register`.
- `"base"`: start from another address key before running `factory`. Keep base dependencies in the same file, since eager registration resolves against that file's temporary map rather than the global map, and avoid cycles.

Resolution runs in order: VScript, references, then platform signature. A signature can disambiguate multiple reference hits, and debug builds may cross-check both results. Currently the parser still requires a nonempty signature or valid `refs` even when `base` or `vscript` is set, so base-only or VScript-only entries are skipped.

## Deploying and owning a custom file

1. Create a module project using the [Hello World settings](../examples/hello-world.md): `net10.0`, C# 13, x64, nullable enabled, implicit usings disabled, and `ModSharp.Sharp.Shared` matching your server (`2.1.227` here). No source generator or unsafe code is needed.
2. Place `gamedata-example.games.jsonc` at `{CS2}/game/sharp/gamedata/gamedata-example.games.jsonc`, **not** next to the module DLL. Publish the module per [Writing a Module](../guides/writing-a-module.md).
3. Call `Register` in `Init` before constructing anything that depends on your custom keys. Built-in keys can be queried directly.
4. In `Shutdown`, tear down commands, listeners, and hooks that use your custom addresses before calling `Unregister`.

Path handling has one quirk to watch: the managed wrapper appends `.jsonc` whenever the path does not already end with `.jsonc` (it never appends `.games.jsonc`), so `Register("example.games")` loads `example.games.jsonc`, while `Register("example.json")` becomes `example.json.jsonc`. The native side joins the path under `../../sharp/gamedata/` (normalizing the extension to `.jsonc` during `Register`). Pass a simple relative filename and spell it identically in `Register` and `Unregister`; avoid absolute paths or `..`.

[!code-csharp[GameData.cs](../../codes/gamedata.cs)]

[Download the C# example](../../codes/gamedata.cs). On load it logs a few built-in values alongside the custom alias address.

## Common pitfalls and lifecycle limits

- **Global key collisions**: All modules share one registry, and keys must be unique within each section (including against built-in keys). Duplicate keys in another file fail registration instead of overriding. Namespace your custom keys and give each file a single owning module.
- **No reference counting**: Managed path tracking is process-wide and case-insensitive. Calling `Register` again for an already-tracked path returns silently, and the first `Unregister` from any caller removes the shared entries. Do not rely on double-registering to share a file between modules.
- **Tracked paths on failure**: Paths are added to the tracking set *before* native registration runs. If `Register` throws, the path can remain tracked and cause later calls to silently no-op. The sample records its attempt beforehand so `Shutdown` still cleans up its own unique file path after a failed `Init` (never do this for built-in or third-party files).
- **File-level unregistration**: `Unregister` silently returns for untracked paths; otherwise it removes all entries tied to that stored file path. The managed set does not canonicalize paths, and Linux filesystems remain case-sensitive.
- **Diagnostics**: Native registration failures throw `InvalidOperationException`, though the message may omit details; check the native console log as well. `Unregister` can also throw on native failure.
- **Patches are not transactional**: Syntax errors, duplicate keys, and eager address failures abort before merging normal maps. Files with `Patches` merge maps first and then apply patches one by one, so a later patch failure can leave earlier changes active.
- **No auto-reload**: Updating a custom file requires stopping its consumers, calling `Unregister`, deploying the updated file, calling `Register` again, and recreating consumers, or following the standard [module reload flow](../guides/reload-module.md).

Keep registry mutations and native access on the game thread; the API is not thread-safe. Server restarts, game updates, and platform differences invalidate old addresses and layouts, so verify your GameData on real Windows and Linux development servers.

## Source references

- [Managed GameData wrapper](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Bridges/Interfaces/GameData.cs) and [IGameData](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/IGameData.cs)
- [Native parser, resolver, and registration](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/gamedata.cpp) and [startup auto-resolution](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/address.cpp)
- [Five baseline GameData assets](https://github.com/Kxnrl/modsharp-public/tree/master/.asset/gamedata)
