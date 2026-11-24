# Hello World

Create a .NET 10 class library named `HelloWorld` and save the following as `HelloWorld.csproj`. The .NET SDK must support C# 13; ModSharp modules run as x64 libraries, not standalone executables.

Each tutorial module is a separate project; do not combine all entry-point classes in one DLL or load every demonstration together. Shared-API and extension-authoring pages explicitly identify their multi-file groups.

[!code-xml[HelloWorld.csproj](../../codes/hello-world.xml)]

The package is pinned to `2.1.227`, the release matching this documentation's source baseline. Match the SDK package version to your server release rather than using a floating `*` version.

- `AssemblyName` must be stable. This example produces `HelloWorld.dll` and `HelloWorld.deps.json`.
- `EnableDynamicLoading` copies plugin-owned dependencies into the output.
- `ExcludeAssets="runtime"` prevents the host-provided SDK runtime assets from being copied. `PrivateAssets="all"` only prevents the package reference from flowing to consumers; it does **not** by itself prevent copying. See the [.NET plugin dependency guidance](https://learn.microsoft.com/dotnet/core/tutorials/creating-app-with-plugin-support#simple-plugin-with-no-dependencies).
- Do not apply `ExcludeAssets="runtime"` to ordinary third-party libraries that your module must deploy.

Add the module below as `HelloWorld.cs`. Keep its public constructor signature unchanged: ModSharp supplies those six arguments itself and does not use your own DI container to construct the entry point.

[!code-csharp[HelloWorld.cs](../../codes/hello-world.cs)]

Publish with `dotnet publish HelloWorld.csproj -c Release --self-contained false -o publish`. Copy the **contents** of `publish` into `{CS2}/game/sharp/modules/HelloWorld/`, including `HelloWorld.deps.json` and plugin-owned dependencies. Do not copy `Sharp.Shared.dll` or other host-provided shared assemblies. Keep only the entry module's `.deps.json` in that module directory: the loader selects its entry DLL from this file.

`Init` installs callbacks and returns whether loading succeeded. `PostInit` is the first safe place to export a shared interface. `OnAllModulesLoaded` runs after initial loading **and for reloaded modules**, so consumers can resolve existing providers there. `OnLibraryConnected` handles providers loaded later. Release listeners, hooks, commands, subscriptions and your DI container in `Shutdown`; it can also run after a partially failed `Init`.
