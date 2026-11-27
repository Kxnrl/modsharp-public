# Installing a Shared API

A shared API assembly holds the interfaces and types shared across modules, while the actual implementation lives in a separate provider module. Deploy the provider module under `sharp/modules` and the shared assembly under `sharp/shared`. Adding a NuGet reference while building a consumer does not deploy either assembly to the server.

## Server layout and loading rules

For an assembly named `SharedInterface.Shared`, the server layout looks like this:

```text
{CS2}/game/sharp/
├── shared/
│   └── SharedInterface.Shared/
│       ├── SharedInterface.Shared.dll
│       └── ...                         # Interface assembly's own dependencies, if any
└── modules/
    └── SharedInterface/
        ├── SharedInterface.dll
        ├── SharedInterface.deps.json
        └── ...
```

Shared assemblies load **before** the initial module scan, checking only immediate subdirectories of `sharp/shared`:

- Normally, directory `Name` must contain `Name.dll` directly inside it. Loose DLLs placed straight under `sharp/shared` are ignored.
- If the folder name itself ends with `.dll`, the loader uses that name as is without appending `.dll` again (stick to the standard layout above).
- A `.disabled` file inside the folder or a `.disabled` suffix on the folder name skips it at startup. Disabling a shared assembly does not disable modules that depend on it.
- Unlike modules, shared discovery does not use `*.deps.json`; still, publish and copy the full output if your shared library has extra dependencies.
- Shared assemblies load into non-unloadable contexts with hot reload off, and types resolve by assembly name, so never install duplicate conflicting copies.

> [!IMPORTANT]
> Shared assemblies load once per server process. Adding, updating, or disabling one requires a **server restart**; module hot reload never reloads `sharp/shared`. Missing or mismatched contracts will stop dependent modules from loading. Do not place `Sharp.Shared.dll` here; it already lives in `sharp/core`.

## Build and deploy a minimal shared assembly

With the .NET 10 SDK installed, run:

```shell
dotnet new classlib --name SharedInterface.Shared --framework net10.0
```

Replace `Class1.cs` with:

```csharp
namespace SharedInterface.Shared;

public interface IMySharedModule
{
    string GetMessage();
}
```

Because this interface uses no framework types, no ModSharp package is needed. Set the `.csproj` to:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>SharedInterface.Shared</AssemblyName>
  </PropertyGroup>
</Project>
```

Publish for the server OS (use `--runtime win-x64` for Windows servers):

```shell
dotnet publish SharedInterface.Shared/SharedInterface.Shared.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output ./publish/SharedInterface.Shared
```

Stop the server and copy the publish output:

# [Windows](#tab/windows)

```powershell
$CS2 = 'C:\CS2'
New-Item -ItemType Directory -Force "$CS2/game/sharp/shared/SharedInterface.Shared" | Out-Null
Copy-Item ./publish/SharedInterface.Shared/* "$CS2/game/sharp/shared/SharedInterface.Shared/" -Recurse -Force
```

# [Linux](#tab/linux)

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/shared/SharedInterface.Shared"
cp -a ./publish/SharedInterface.Shared/. "$CS2/game/sharp/shared/SharedInterface.Shared/"
```

---

If your shared types reference ModSharp types, add the matching `ModSharp.Sharp.Shared` package with `ExcludeAssets="runtime"` just as in [Writing a Module](writing-a-module.md), so framework binaries are not bundled into `sharp/shared`.

## Reference the shared assembly from modules

If the shared project sits next to your module project, add this to both the provider and consumer `.csproj` files:

```xml
<ItemGroup>
  <ProjectReference Include="../SharedInterface.Shared/SharedInterface.Shared.csproj"
                    Private="false" ExcludeAssets="runtime" />
</ItemGroup>
```

`Private="false"` stops the contract DLL from copying into the module folder, and `ExcludeAssets="runtime"` excludes its transitive runtime assets too. When referencing a shared NuGet package instead, set `ExcludeAssets="runtime"` on the `PackageReference` and pin the version deployed on the server (`PrivateAssets="all"` alone will not stop DLL copying on publish).

In code, the provider registers its implementation in `PostInit()` via `ISharedSystem.GetSharpModuleManager().RegisterSharpModuleInterface(...)`. Consumers look it up in `OnAllModulesLoaded()` via `GetRequiredSharpModuleInterface<T>` or `GetOptionalSharpModuleInterface<T>`, keep the returned `IModSharpModuleInterface<T>` wrapper, and check `Instance` before each call (never cache the raw implementation object across reloads). Because unloading permanently sets that wrapper's `Instance` to `null` instead of auto-rebinding it, clear your reference in `OnLibraryDisconnect` and acquire a fresh wrapper in `OnLibraryConnected`. Pick a unique interface identity to avoid collisions with other modules.

The shared assembly itself does not implement `IModSharpModule`; only the provider module does. See the [Module Shared API example](../examples/module-api.md) for full code.
