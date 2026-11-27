# Native Calls

This example uses SDK-generated signature and virtual calls with keys already supplied by ModSharp's `server.games.jsonc`: `CBaseEntity::SetGravityScale` and `CBaseEntity::CollisionRulesChanged`. The old custom `native-call.games.jsonc` and hardcoded vtable indices are no longer needed.

Use the [Hello World project settings](hello-world.md), add `ModSharp.Sharp.Generator.Sdk` as an analyzer package (`PrivateAssets="all"`), and enable `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`. Generated implementations contain unmanaged function pointers even when the handwritten interfaces do not use `unsafe`. Keep the supplied `[assembly: DisableRuntimeMarshalling]` attribute.

`IVirtualCall<T>` obtains a vtable index. `ISignatureCall<T>` resolves a `NetClass::Method` key from `T`'s native `NetClass` name and the C# method name (where `[AddressKey]` overrides only the method part). On non-generic `ISignatureCall` (without `T`), `[AddressKey]` must contain the full key. Register `IGameData` in DI, and resolve the same interface types that you registered.

The current virtual-call generator requires the first parameter to be named `instance`. Keep that name in virtual-call interface declarations; otherwise the generated implementation will not compile.

The client chat command `.native_gravity` (or `ms_native_gravity` in the client console) sets the living caller's gravity scale to `0.5` and invokes the collision-rules virtual function. This changes gameplay; test on a development server. Prefer the SDK's existing high-level entity methods for ordinary plugin code.

[!code-csharp[NativeCall.cs](../../codes/native-call.cs)]

For your own targets, register your custom GameData before constructing generated call wrappers and unregister it in `Shutdown`. Native signatures and indices are platform/build-specific; an incorrect ABI can crash the server. Do not copy offsets from older CS2 builds.

See [GameData](../features/gamedata.md) for custom configuration and lifecycle rules, and [native libraries](../features/library-modules.md) for lower-level symbol and address lookup.
