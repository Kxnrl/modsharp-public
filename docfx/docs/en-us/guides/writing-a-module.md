# Writing a Module

A ModSharp module is a .NET class library loaded by the server. This guide walks through creating a minimal module from the command line; any IDE with the .NET 10 SDK works just as well.

## Create the project and pin the SDK

With the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed on your dev machine, run:

```shell
dotnet new classlib --name Example --framework net10.0
```

Replace `Example/Example.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>Example</AssemblyName>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModSharp.Sharp.Shared" Version="[2.1.227]"
                      PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

`2.1.227` is the published SDK matching this docs revision. **Pin `ModSharp.Sharp.Shared` to the exact build installed on your server** using brackets (avoid `Version="*"` so you do not pull in APIs your server does not have yet; note that the source tree's default `2.1.1` gets replaced by the build number during releases).

Why these settings matter:

- `net10.0`, C# 13, and x64 match the host runtime.
- `AssemblyName` sets the DLL and `.deps.json` filenames explicitly so deployment stays predictable.
- `EnableDynamicLoading` emits plugin-ready dependency metadata (deploy the full publish output, not just the main DLL).
- `ExcludeAssets="runtime"` keeps `Sharp.Shared.dll` and host dependencies out of your module folder.
- `PrivateAssets="all"` only stops transitive flow to downstream projects; **by itself it will not prevent `dotnet publish` from copying the DLLs**, so pair it with `ExcludeAssets="runtime"`. See [NuGet asset metadata](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets) and [.NET's plugin guide](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support#reference-a-plugin-interface-from-a-nuget-package).

## Implement the entry point

Rename `Class1.cs` to `Example.cs` and replace its contents:

```csharp
using System;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;

namespace Example;

public sealed class Example : IModSharpModule
{
    public Example(ISharedSystem sharedSystem,
        string dllPath,
        string sharpPath,
        Version version,
        IConfiguration configuration,
        bool hotReload)
    {
    }

    public string DisplayName => "Example";
    public string DisplayAuthor => "YourName";

    public bool Init()
    {
        Console.WriteLine("Hello, World!");
        return true;
    }

    public void Shutdown()
    {
        Console.WriteLine("Byebye, World!");
    }
}
```

Put only one non-abstract `IModSharpModule` in the entry assembly, with a **public** six-parameter constructor (`public` class recommended, though `internal` is also picked up). The loader only instantiates the first match; put separate modules in separate folders.

Constructor arguments arrive in this exact order:

| Argument | Description |
| --- | --- |
| `ISharedSystem sharedSystem` | Root entry point for framework services (`GetModSharp()`, `GetSharpModuleManager()`, etc.). |
| `string dllPath` | This module's deployment **directory** (not the DLL path). |
| `string sharpPath` | The server's `game/sharp` root directory. |
| `Version version` | Your module assembly's version. |
| `IConfiguration configuration` | Core configuration loaded from `sharp/configs/core.json`. |
| `bool hotReload` | `false` on initial startup; `true` when replaced during a map-change reload. |

The host watches `configs/core.json`, though modules still need to load and manage any module-specific config files themselves using `dllPath` or `sharpPath`.

## Module lifecycle

On initial startup, each module runs through:

1. Constructor (if it throws, no instance exists so `Shutdown()` is skipped, though leaked registrations are still cleaned up).
2. `Init()`, which must return `true`. Returning `false` or throwing marks the load failed and calls `Shutdown()` on the instance to clean up.
3. State transitions to `Running`, then `PostInit()` runs. Export shared interfaces and dynamic natives here rather than in the constructor or `Init()`.
4. Already-running modules receive `OnLibraryConnected(assemblyName)`.
5. Once the initial module scan finishes, running modules receive `OnAllModulesLoaded()`. Resolve cross-module dependencies here instead of assuming a load order.

`PostInit()`, `OnLibraryConnected()`, `OnLibraryDisconnect()`, and `OnAllModulesLoaded()` have default empty implementations; `Init()`, `Shutdown()`, `DisplayName`, and `DisplayAuthor` are required.

`OnLibraryConnected` does not replay modules loaded earlier, and passes the loaded assembly name without `.dll`. If a provider module reloads, consumers must handle `OnLibraryDisconnect` and reconnecting; see [Module Shared APIs](../examples/module-api.md).

On unload, the core marks the module's load context unloaded (clearing timers and `cs_script` delegates and blocking new listener/hook/timer registrations during `Shutdown()`), sends `OnLibraryDisconnect` to other modules, unregisters its exported interfaces and natives, and calls `Shutdown()`. Release your listeners, hooks, timers, and external resources inside `Shutdown()`. Map callbacks run on a separate lifecycle, so a loaded module does not mean a map or `GameRules` is ready yet; see the [Migration Guide](migration-guide.md).

## Publish and deploy

From the parent folder containing `Example`, run the command matching your **server OS**:

```shell
# Windows x64 server
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime win-x64 --no-self-contained --output ./publish/Example
```

```shell
# Linux x64 server
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output ./publish/Example
```

Always publish framework-dependent (`--no-self-contained`); do not use self-contained, single-file, trimming, or Native AOT. Keep any module-specific dependencies and assets from the publish output.

For an initial install, stop the server and copy the publish folder into `game/sharp/modules/Example/`:

# [Windows](#tab/windows)

```powershell
$CS2 = 'C:\CS2'
New-Item -ItemType Directory -Force "$CS2/game/sharp/modules/Example" | Out-Null
Copy-Item ./publish/Example/* "$CS2/game/sharp/modules/Example/" -Recurse -Force
```

# [Linux](#tab/linux)

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/modules/Example"
cp -a ./publish/Example/. "$CS2/game/sharp/modules/Example/"
```

---

Minimum deployed layout:

```text
{CS2}/game/sharp/modules/Example/
├── Example.dll
├── Example.deps.json
└── ...                    # Module-specific dependencies/assets, if any
```

The loader scans immediate subdirectories of `sharp/modules`, grabs the first top-level `*.deps.json`, and loads the matching DLL. Keep one module per directory. The folder name does not have to match `AssemblyName` (several bundled modules differ), though matching names keep things tidy. Preserve exact filename casing on Linux.

At startup, any directory whose name ends with `.disabled` or that contains a `.disabled` file is skipped (dropping a `.disabled` file at runtime will not unload an already-running module).

Start the server and check the startup logs for `Hello, World!`, then run `ms modules` in the server console to confirm the module's state is `Running`. For future updates, use the [reload flow](reload-module.md) instead of overwriting live files.
