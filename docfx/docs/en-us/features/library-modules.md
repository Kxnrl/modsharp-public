# Native Library Modules

`ILibraryModule` wraps a native game library already loaded in the CS2 server process (such as `server.dll` on Windows or `libserver.so` on Linux). It provides export lookups, pattern/data scanning, RTTI/vtable lookups, and image metadata. It cannot load managed modules or open arbitrary DLL/SO files.

Keep the two meanings of "module" separate:

| Type | Role |
| --- | --- |
| `IModSharpModule` | Your managed module entry point (`Init` / `Shutdown`), managed by the ModSharp loader; see [Writing a Module](../guides/writing-a-module.md). |
| `ILibraryModule` | A host-owned view of a loaded native library. Your code borrows it and any addresses it returns; never free or unload them. |

`OnLibraryConnected` and `OnLibraryDisconnect` track managed ModSharp modules, not native library load events. For cross-platform targets that need ongoing maintenance alongside calls and hooks, prefer [GameData](gamedata.md). See also [Native Call](../examples/native-call.md) and [Native Hook](../examples/native-hook.md).

## Getting the library manager

Call `sharedSystem.GetLibraryModuleManager()` on **`ISharedSystem`** (not `IModSharp`). `ILibraryModuleManager` exposes these properties:

| Property | GameData `library` name |
| --- | --- |
| `Server` | `server` |
| `Engine` | `engine` (engine2) |
| `Tier0` | `tier0` |
| `Schema` | `schemasystem` |
| `Resource` | `resourcesystem` |
| `VScript` | `vscript` |
| `VPhysics` | `vphysics2` |
| `SoundSystem` | `soundsystem` |
| `NetworkSystem` | `networksystem` |
| `WorldRenderer` | `worldrenderer` |
| `MatchMaking` | `matchmaking` |
| `FileSystem` | `filesystem` (filesystem_stdio) |
| `SteamNetworkingSocket` | `steamsockets` (steamnetworkingsockets) |
| `MaterialSystem` | `materialsystem2` |

Use these properties directly. GameData also recognizes `animationsystem`, though this public manager does not expose an `AnimationSystem` property and has no method to load or unload arbitrary libraries.

Each view exposes `nint BaseAddress`, `nuint ImageSize`, and `string ModuleName` (the native module name, not your assembly name). Base and size are handy for address math, though segments inside the image have different read/write/execute permissions and cannot be dereferenced blindly.

## Exports: use `GetExportFunction`, not `FindSymbol`

The public interface has no `FindSymbol` method. Use these instead:

- `nint GetExportFunction(string exportName)`: checks the raw export name first, then demangled/normalized aliases from the native export index. Returns `0` if missing or if a demangled alias is ambiguous. Prefer raw symbols or full signatures for overloaded C++ functions.
- `NativeFunctionInfo[] FindExportFunctions(string exportName)`: returns matching export entries (`RawName`, `Signature`, `Address`) using exact or normalized alias matching (not wildcards or substrings), or an empty array if nothing matches.

`GetFunctionByName` is obsolete; avoid it in new code.

The host itself uses tier0's `LoggingSystem_IsChannelEnabled` export in `log.games.jsonc`, and the example below queries it directly. **Do not add an `@` prefix here**: `@` is GameData signature syntax and is not parsed by `GetExportFunction` or pattern scanners.

Windows queries the loaded PE export table with normalized-name fallback; Linux queries collected ELF exports with the same alias lookup. Because C++ name mangling and ABIs differ across platforms (for example, `CVProfile::OutputReport` uses different raw symbols on Windows and Linux), verify each platform independently.

## Pattern and data searches

| API | Behavior |
| --- | --- |
| `FindPattern(string pattern, nint startAddress = 0)` | Returns the first match in executable segments, or `0`; uniqueness is not required. With a nonzero `startAddress`, the managed wrapper enumerates matches and picks the first one **at or after** `startAddress` (inclusive). |
| `FindPatternMulti(string pattern)` | Returns all matches as a `List<nint>`, or an empty list. |
| `FindPatternExactly(string pattern)` | Returns `0` for no match, the address for a single match, and **throws `InvalidOperationException` on multiple matches**. |
| `FindData(ReadOnlySpan<byte> data, bool readOnly)` | Finds exact bytes in non-executable segments; throws `ArgumentException` on empty input. |
| `FindString(string str)` / `FindStringExact(string str)` | Finds null-terminated strings in non-executable segments. `Exact` also checks the start boundary so a suffix inside a longer string does not match. |
| `FindString(string str, bool readOnly, bool exact)` | Explicit data-section and exact-match flags. |
| `FindPtr(nint ptr)` / `FindPointers(nint ptr)` | Finds storage locations holding `ptr` in non-executable segments without dereferencing it; returns `0` or an empty array when absent. |

`readOnly: true` skips writable segments while `false` includes them; neither `FindData` nor string searches scan executable segments.

Patterns use space-separated hex bytes with whole-byte `?`/`??` wildcards (no nibble wildcards); see [GameData's pattern and address-transform rules](gamedata.md#patterns-offsets-and-pointers-are-different-things). Unlike GameData, pattern methods do not treat `@symbol` as an export lookup. When stepping through matches, pass a `startAddress` strictly greater than the previous match (e.g. `last + 1`) so you advance past it.

> [!IMPORTANT]
> The native pattern scanner inspects a captured executable-segment image, preferring original file-backed code bytes when available and falling back to the runtime image. It may not reflect detour bytes patched in after capture, though returned addresses are translated to the loaded module's runtime address space.

Run scans during initialization or explicit diagnostic commands, not every frame. A unique byte match only means the sequence appears once; verify the function entry and prototype against the binary yourself.

## Vtables, interfaces, and function references

| API | Usage and failure behavior |
| --- | --- |
| `TryGetVirtualTableByName(string tableName, out nint address, bool decorated = false)` | Preferred for optional vtables; returns `false`/`0` if missing. Accepts plain class names like `CCSPlayerPawn`. |
| `GetVirtualTableByName(string tableName, bool decorated = false)` | Required lookup: missing tables trigger native `FatalError` instead of a catchable managed exception. |
| `GetVirtualFunctions(string tableName)` | Returns a copied array of known virtual-function addresses (empty if none); not an object's vptr. |
| `FindVirtualTablesPartial(string str)` | Searches RTTI/vtable metadata by partial class name. |
| `TryGetTypeInfo(string typeName, out nint typeInfo)` | Looks up RTTI type info; returns `false`/`0` if missing. |
| `IsPointerDerivedFrom(nint ptr, string name)` | Checks RTTI on a **known-valid object instance**; do not use it to test whether an arbitrary pointer is valid. |
| `FindInterface(string interfaceName)` | Calls the library's `CreateInterface` factory for a versioned engine interface. Missing versions may return `0`, and libraries without a factory can trip a native assertion; only query interfaces the library actually exports. |
| `GetReferencesFromPointer(nint ptr)` | Returns instruction addresses referencing the target, not function-entry addresses. |
| `FindFunction(...)` / `FindFunctions(...)` | Finds functions referencing a string, pointer, or span of inputs (multiple inputs intersect to functions referencing **all** of them). Throws if no match is found; singular `FindFunction` also throws on multiple matches. |
| `GetFunctionRange(nint middle, out nint start, out nint end)` | Returns `true` with `[start, end)` if `middle` falls inside an indexed function; otherwise `false`. |
| `GetKnownFunctionRanges()` | Returns a copy of `NativeFunctionRange[]` from the native function index; not guaranteed to cover every function or optimized region. |

`decorated: true` expects the platform's encoded RTTI type name (the native lookup matches RTTI names and selects the primary zero-offset vtable), not arbitrary exported vtable symbol names. Multiple inheritance, thunks, optimizations, and stripped metadata can affect results. Keep vtable addresses, slot indices, virtual-function addresses, and object pointers distinct.

## RVAs, reads, and permissions

`ILibraryModule` exposes no `GetRva`, `Read<T>`, `Write<T>`, or memory-protection APIs:

- To compute an RVA, confirm the address belongs to the image and subtract `BaseAddress`. Adding a verified RVA back to `BaseAddress` works within that **same binary build**; never copy absolute addresses across processes or reuse RVAs across game updates without rechecking.
- `GetSegments()` returns `ModuleSegmentInfo[]` with `Address`, `Size`, and `Access` (`MemoryAccess`: `Read = 1`, `Write = 2`, `Execute = 4`). These reflect properties captured by the native scanner, not a live OS page-permission query, and cannot change permissions.
- For actual memory reads, use .NET's [Marshal.ReadByte](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.readbyte?view=net-10.0) or typed `unsafe` code. Invalid addresses cause access violations that managed `try/catch` cannot intercept.

For multi-byte reads, verify the entire span, alignment, and target lifetime rather than just the starting address. Exports can also theoretically be forwarded outside the expected image; the example below skips the live read if its bounds checks fail.

## Complete inspection example

Create a module project using the [Hello World settings](../examples/hello-world.md): `net10.0`, C# 13, x64, nullable enabled, implicit usings disabled, and `ModSharp.Sharp.Shared` pinned to your host release (`2.1.227` here). No source generator or `unsafe` block is required.

At startup, the sample prints tier0 metadata and registers a **server-console-only** command `ms_library_module_info` to print it again on demand. It resolves the logging export, computes its RVA, lists segments and export aliases, and reads a single live byte only after verifying image and segment bounds, without calling the function or modifying memory. Because existing hooks may alter the first byte, the example makes no assumptions about a fixed byte value or RVA.

[!code-csharp[LibraryModule.cs](../../codes/library-module.cs)]

[Download the C# example](../../codes/library-module.cs) and deploy it per [Writing a Module](../guides/writing-a-module.md). No custom GameData file is needed.

`Shutdown` removes the command before dropping the borrowed library reference; none of the returned native addresses should be freed. Handle partial initialization in your own modules too, since `Shutdown` still runs if `Init` fails. Keep native work on the game thread, and uninstall/dispose any hooks before releasing their owners or unregistering GameData so unmanaged callbacks never point into an unloaded assembly. Test runtime lookups and scans separately on real Windows and Linux servers.

## Source references

- [ILibraryModule](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/ILibraryModule.cs), [ILibraryModuleManager](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/Managers/LibraryModuleManager.cs), and [ISharedSystem](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Shared/ISharedSystem.cs)
- [Managed wrapper](https://github.com/Kxnrl/modsharp-public/blob/master/Sharp.Core/Bridges/Interfaces/LibraryModule.cs) and [native scanner](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module.cpp)
- [Windows implementation](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module_windows.cpp) and [Linux implementation](https://github.com/Kxnrl/modsharp-public/blob/master/Engine/src/module_linux.cpp)
