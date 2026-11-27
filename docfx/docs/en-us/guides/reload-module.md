# Reloading a Module

ModSharp swaps updated modules on the **next map change** (right before the next map's `OnServerInit`), or on the next server startup. Copying files does not reload immediately, and there is no `ms reload` command.

## Stage the new build

Keep the entry DLL filename unchanged and publish with the same target framework, server RID, and `--no-self-contained` settings used during [initial deployment](writing-a-module.md), targeting the module's `reload` subfolder.

For `Example`:

# [Windows](#tab/windows)

Run from the parent directory of `Example` (replace `$CS2` with your server path):

```powershell
$CS2 = 'C:\CS2'
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime win-x64 --no-self-contained --output "$CS2/game/sharp/modules/Example/reload"
```

# [Linux](#tab/linux)

Run from the parent directory of `Example` (replace `CS2` with your server path):

```bash
CS2=/srv/cs2
dotnet publish Example/Example.csproj --configuration Release --framework net10.0 --runtime linux-x64 --no-self-contained --output "$CS2/game/sharp/modules/Example/reload"
```

---

The live files remain untouched in the parent folder:

```text
{CS2}/game/sharp/modules/Example/
├── Example.dll                 # Current version
├── Example.deps.json
└── reload/
    ├── Example.dll             # New version
    ├── Example.deps.json
    └── ...                     # Updated top-level dependencies
```

> [!IMPORTANT]
> Wait until all files finish uploading **before** changing maps. Any top-level file in the staging folder triggers a reload; the loader does not check assembly versions or wait for an upload marker. If your server rotates maps automatically, avoid uploading mid-map-change so the loader never reads half-written files.

The loader also checks a same-name staging folder `modules/<dir>/<dir>/` (matching the deployment folder name, not necessarily `AssemblyName`). Avoid it: whenever that folder exists, even if empty, it takes priority over `reload/`, and the updater only deletes `reload/` while leaving the same-name folder behind to permanently shadow future `reload/` updates. Stick to `reload/`.

## Apply on the next map change

Once staged, change maps as usual, for example from the server console:

```text
changelevel de_dust2
```

During the next native `StartupServer` pre-hook, the module manager:

1. Unloads each module with staged top-level files: fires `OnLibraryDisconnect` on other running modules, removes its exported interfaces and natives, then calls its `Shutdown()`.
2. Copies each staged top-level file over the live file, removes the copied files, and deletes `reload/` non-recursively (subdirectories inside `reload/` cause deletion to fail).
3. Instantiates the new module with `hotReload = true`, runs `Init()` and `PostInit()`, and fires `OnLibraryConnected` on other running modules.
4. Calls `OnAllModulesLoaded()` only on the newly replaced modules, leaving untouched modules alone. (If zero modules remain `Running`, the core aborts with a fatal error.)

Check the logs and `ms modules` after the map loads to confirm the new version is `Running`. Consumer modules should reacquire exported interfaces on `OnLibraryConnected` rather than waiting for `OnAllModulesLoaded()`.

## Limitations

- **Top-level files only**: Subdirectories (such as `runtimes/`, satellite assemblies, or nested native/asset folders) are not copied recursively, and leaving subdirectories inside `reload/` breaks non-recursive cleanup (which can abort loading that module on startup). If an update touches subdirectories, stop the server and replace the whole module folder.
- **Stale files stay behind**: Files removed in the new build are not deleted from the live folder; clean them up during a stopped-server deploy.
- **Startup applies staged files too**: Pending files in `reload/` are also applied during initial server startup, with `hotReload = false`. The live folder must already contain a discoverable `.deps.json` and DLL; a `reload/` folder alone does not count as an initial install.
- **No new module discovery on map change**: Adding new module folders or toggling `.disabled` markers requires a server restart.
- **Modules only**: Updating shared API assemblies (`sharp/shared`), the managed core, or the native engine requires restarting the server.
- **Clean up properly in `Shutdown()`**: Unregister callbacks and release external resources yourself. Reloading runs in the same process, so lingering references will keep old instances alive.
