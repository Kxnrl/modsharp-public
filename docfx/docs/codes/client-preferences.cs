using System;
using Microsoft.Extensions.Configuration;
using Sharp.Modules.ClientPreferences.Shared;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace ClientPrefsExample;

public sealed class ClientPrefs : IModSharpModule
{
    private readonly IClientManager      _clients;
    private readonly IHookManager        _hooks;
    private readonly ISharpModuleManager _modules;

    public ClientPrefs(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               coreConfiguration,
        bool                         hotReload)
    {
        _clients = sharedSystem.GetClientManager();
        _hooks   = sharedSystem.GetHookManager();
        _modules = sharedSystem.GetSharpModuleManager();
    }

    public bool Init()
    {
        // install hook
        _hooks.PlayerSpawnPost.InstallForward(OnPlayerSpawned);

        // install command
        _clients.InstallCommandCallback("skin", OnSkinCommand);

        return true;
    }

    public void Shutdown()
    {
        // must remove the hooks/commands in Shutdown
        // otherwise you will get fucked after reloaded.
        _hooks.PlayerSpawnPost.RemoveForward(OnPlayerSpawned);

        _clients.RemoveCommandCallback("skin", OnSkinCommand);
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (param.Client.IsFakeClient)
        {
            return;
        }

        if (GetInterface() is not { } cp || !cp.IsLoaded(param.Client.SteamId))
        {
            return;
        }

        if (cp.GetCookie(param.Client.SteamId, "PlayerDefaultModel") is not { } cookie)
        {
            return;
        }

        param.Pawn.SetModel(cookie.GetString());
    }

    private ECommandAction OnSkinCommand(IGameClient client, StringCommand command)
    {
        if (command.ArgCount != 1 || GetInterface() is not { } cp || !cp.IsLoaded(client.SteamId))
        {
            return ECommandAction.Stopped;
        }

        var skin = command.GetArg(1);

        cp.SetCookie(client.SteamId, "PlayerDefaultModel", skin);

        return ECommandAction.Stopped;
    }

    private IModSharpModuleInterface<IClientPreference>? _cachedInterface;

    // this may have performance issue if you call it too frequently,
    // the bast way is cache with 'OnAllModulesLoaded'/'OnLibraryConnected' and clear with 'OnLibraryDisconnect' manually
    private IClientPreference? GetInterface()
    {
        if (_cachedInterface?.Instance is null)
        {
            _cachedInterface = _modules.GetOptionalSharpModuleInterface<IClientPreference>(IClientPreference.Identity);
        }

        return _cachedInterface?.Instance;
    }

    public string DisplayName   => "ClientPrefs Example";
    public string DisplayAuthor => "ModSharp dev team";
}
