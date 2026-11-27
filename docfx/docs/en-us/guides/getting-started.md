# Getting Started

This guide covers installing ModSharp on an existing CS2 dedicated server. `{CS2}` below refers to your server root directory (the one containing `game`).

## Download the server package

Grab the archive matching your server OS from [Releases](https://github.com/Kxnrl/modsharp-public/releases):

- Windows x64: `ModSharp-git<build>-windows.zip`
- Linux x64: `ModSharp-git<build>-linux.zip`

The `*-extensions.zip` files are development libraries rather than full server packages. Always keep the native engine, managed core, modules, shared assemblies, configs, and gamedata from the same release together.

## Install the runtime

ModSharp's managed core and modules target **.NET 10 (`net10.0`) x64**. The architecture **must be x64** (x86 and Arm64 builds will not work).

To run a server, you only need the **.NET 10 Runtime (x64)**; there is no need to install the SDK or ASP.NET Core Runtime just to host a server. If the machine already has the x64 **.NET 10 SDK** installed, it includes the runtime and works as is. On your development machine, install the **.NET 10 SDK** to [write modules](writing-a-module.md).

# [Windows](#tab/windows)

Install the [Visual C++ x64 Redistributable](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist), then install .NET 10 in either of two ways:

- **System install**: install the [.NET 10 x64 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (detected under `C:\Program Files\dotnet\host\fxr`).
- **Private runtime**: extract the Windows x64 .NET 10 Runtime zip directly into `{CS2}/game/sharp/runtime`.

# [Linux](#tab/linux)

Run the server inside a CS2-compatible **Steam Runtime 3 (sniper)** environment (including Docker containers). The .NET runtime and its native libraries must be visible inside the actual process environment running CS2, not just on the host machine.

The easiest approach is to download the **Linux x64 .NET 10 Runtime** tarball from the [.NET download page](https://dotnet.microsoft.com/download/dotnet/10.0) and extract it straight into `{CS2}/game/sharp/runtime` (swap in your server path and filename):

```bash
CS2=/srv/cs2
mkdir -p "$CS2/game/sharp/runtime"
tar -xzf ./dotnet-runtime-10.0.<patch>-linux-x64.tar.gz -C "$CS2/game/sharp/runtime"
```

The host also checks `/usr/share/dotnet/host/fxr` and `/usr/lib/dotnet/host/fxr` when those paths and dependencies are visible inside the server environment; if Steam RT3 hides the host's .NET install, use the private runtime above.

If you deploy with Pterodactyl, see [1zc/CS2-Pterodactyl](https://github.com/1zc/CS2-Pterodactyl).

---

A private runtime should have this exact layout:

```text
{CS2}/game/sharp/runtime/
├── dotnet                         # dotnet.exe on Windows
├── host/fxr/10.0.<patch>/
│   └── libhostfxr.so               # hostfxr.dll on Windows
└── shared/Microsoft.NETCore.App/10.0.<patch>/
    └── ...
```

> [!IMPORTANT]
> Do not nest an extra `dotnet` folder between `runtime` and `host`. The native host ignores `PATH` and `DOTNET_ROOT` when locating `hostfxr`; instead, it picks the highest major version >= 10 across its private and system search paths. Also, `sharp/core/Sharp.Core.runtimeconfig.json` targets .NET 10 specifically: if a newer major version (e.g. .NET 11) is visible in a system path, the host loads that system `hostfxr`, which looks for .NET 10 inside that system folder rather than `sharp/runtime`, so .NET 10 must be installed in that same system location as well.

## Extract ModSharp

Stop the server and extract the archive into `{CS2}/game`. The zip already contains a top-level `sharp` directory, so make sure you do not create `game/sharp/sharp`:

```text
{CS2}/game/
├── csgo/
│   └── gameinfo.gi
└── sharp/
    ├── bin/
    │   ├── modsharp.dll                   # Windows native engine
    │   ├── win64/server.dll               # Windows loader
    │   ├── libmodsharp.so                 # Linux native engine
    │   └── linuxsteamrt64/libserver.so     # Linux loader
    ├── core/
    │   ├── Sharp.Core.dll
    │   ├── Sharp.Core.deps.json
    │   ├── Sharp.Core.runtimeconfig.json
    │   ├── Sharp.Shared.dll
    │   └── ...                            # Keep all bundled dependencies
    ├── configs/
    │   └── core.json
    ├── gamedata/
    ├── locales/
    ├── modules/
    ├── shared/
    └── runtime/                           # Private runtime, if used
```

Each archive includes only the native binaries for its OS and omits `runtime`; leave the rest of the files in place. The loader in `sharp/bin/<platform>` loads the engine from `sharp/bin`, so do not move either into `csgo/bin`.

## Edit `gameinfo.gi`

Open `{CS2}/game/csgo/gameinfo.gi` and add `Game sharp` inside `FileSystem` -> `SearchPaths`, **before** `Game csgo`:

```diff
 SearchPaths
 {
     Game_LowViolence    csgo_lv
+    Game                sharp
     Game                csgo
```

Leave the rest of the file untouched. Just like Metamod, this hooks loading via search paths. CS2 updates occasionally overwrite `gameinfo.gi`, so check this line again after updates.

## Start and verify

Start the server and load a map. Look for `Sharp.Core initialization completed` in the logs, and scan above it for any loader, runtime, gamedata, or module errors.

In the **server console**, run:

```text
ms
ms version
ms modules
```

`modsharp` is an alias for `ms`. `ms version` prints the build version, detected hostfxr version (`DotNet Version`), and commit hash. `ms modules` lists every loaded module's name, version, author, and state (`Running` when initialized cleanly).

> [!NOTE]
> At least one module must initialize for startup to succeed. Keep the bundled modules on a fresh install: if no modules exist, or every module is disabled or fails to load, managed startup aborts. `AdminCommands.SQLStorage` ships disabled by default; configure and enable it only if you need SQL storage.

Next steps: [Write a Module](writing-a-module.md), [Install a Shared API](integrating-api.md), or [Reload a Module](reload-module.md).
