# Custom Target Resolver

Reference `ModSharp.Sharp.Modules.TargetingManager.Shared` as a host-provided contract (`ExcludeAssets="runtime"`) and install `Sharp.Modules.TargetingManager`. Use the [Hello World project settings](hello-world.md).

This example registers **`@example_aim`**, which selects the living player under the caller's crosshair. `@aim` is already built into TargetingManager; attempting to register that name again fails. Console callers and traces without a living player target return an empty result.

`RnQueryShapeAttr.Bullets()` supplies the native trace attributes. `SetEntityToIgnore(pawn, 0)` prevents hitting the caller's own hitbox. The resolver checks that the hit entity is a pawn before converting it.

The Shared contract is a compile-time dependency; the provider is optional at runtime. Resolve its wrapper in `OnAllModulesLoaded` and `OnLibraryConnected`, and clear it on disconnect. Register with your actual assembly name as the owner: TargetingManager removes that owner's resolvers when the module disconnects. When the provider is replaced, the wrapper is resolved again and the resolver is re-registered.

[!code-csharp[TargetingManager.cs](../../codes/targeting-manager.cs)]
