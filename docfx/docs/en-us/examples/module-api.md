# Module Shared API

A shared API needs three projects, not three module entry points in one assembly:

| Project / AssemblyName | Source | References |
|---|---|---|
| `SharedInterface.Shared` | `module-api.cs` | None |
| `SharedInterface` | `module-impl.cs` | `SharedInterface.Shared`, the ModSharp SDK |
| `UseSharedModule` | `module-use-api.cs` | `SharedInterface.Shared`, the ModSharp SDK |

Use the [Hello World project settings](hello-world.md) for the provider and consumer. The contract project is an ordinary `net10.0` class library. Deploy **one** copy of `SharedInterface.Shared.dll` to `{CS2}/game/sharp/shared/`; do not place private copies beside the modules. For project references to this contract, use `Private="false" ExcludeAssets="runtime"` (for packaged contracts, `ExcludeAssets="runtime"`).

Keep the provider's `AssemblyName` as `SharedInterface`: lifecycle callback names are assembly file names without `.dll`, **not** the class name `MySharedModule`.

## Contract

Interface identities are global string keys. Use namespace-qualified constants to avoid collisions.

[!code-csharp[SharedInterface.Shared](../../codes/module-api.cs)]

## Provider

Register interfaces in `PostInit`, after ModSharp has associated the entry-point instance with its owning module. The framework unregisters these interfaces on unload.

[!code-csharp[SharedInterface](../../codes/module-impl.cs)]

## Consumer

Resolve existing providers in `OnAllModulesLoaded`, which also runs for a reloaded consumer. Resolve later providers in `OnLibraryConnected`, and clear references in `OnLibraryDisconnect`. Cache `IModSharpModuleInterface<T>`, not its `Instance`; the wrapper's instance becomes null when its provider is unloaded. Read `.Instance` when calling and do not retain it across asynchronous work or reloads.

[!code-csharp[UseSharedModule](../../codes/module-use-api.cs)]

Shared contracts alone do not load a provider. Install the provider module separately. For code reused privately inside a module rather than a runtime service exported to other modules, use an [extension](writing-sharp-extensions.md).
