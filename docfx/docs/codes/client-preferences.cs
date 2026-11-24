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
    private const string ProviderAssemblyName = "Sharp.Modules.ClientPreferences";
    private const string CookieKey = "ClientPrefsExample.SpawnMessage";

    private readonly IClientManager _clients;
    private readonly IHookManager _hooks;
    private readonly ISharpModuleManager _modules;
    private IModSharpModuleInterface<IClientPreference>? _cachedInterface;
    private IDisposable? _callback;

    public ClientPrefs(ISharedSystem sharedSystem,
        string                       dllPath,
        string                       sharpPath,
        Version                      version,
        IConfiguration               coreConfiguration,
        bool                         hotReload)
    {
        _clients = sharedSystem.GetClientManager();
        _hooks = sharedSystem.GetHookManager();
        _modules = sharedSystem.GetSharpModuleManager();
    }

    public bool Init()
    {
        _hooks.PlayerSpawnPost.InstallForward(OnPlayerSpawned);
        _clients.InstallCommandCallback("greeting", OnGreetingCommand);
        return true;
    }

    public void OnAllModulesLoaded()
        => ResolvePreferences();

    public void OnLibraryConnected(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ResolvePreferences();
        }
    }

    public void OnLibraryDisconnect(string name)
    {
        if (name.Equals(ProviderAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            ReleasePreferences();
        }
    }

    private void ResolvePreferences()
    {
        if (_cachedInterface?.Instance is not null)
        {
            return;
        }

        ReleasePreferences();
        _cachedInterface = _modules.GetOptionalSharpModuleInterface<IClientPreference>(IClientPreference.Identity);
        if (_cachedInterface?.Instance is not { } preferences)
        {
            return;
        }

        _callback = preferences.ListenOnLoad(OnCookieLoad);

        // Hot-reloading this consumer does not replay earlier cookie-load notifications.
        foreach (var client in _clients.GetGameClients(true))
        {
            if (!client.IsFakeClient && !client.IsHltv && client.SteamId.IsValidUserId()
                && preferences.IsLoaded(client.SteamId))
            {
                OnCookieLoad(client);
            }
        }
    }

    private void ReleasePreferences()
    {
        _callback?.Dispose();
        _callback = null;
        _cachedInterface = null;
    }

    private void OnPlayerSpawned(IPlayerSpawnForwardParams param)
    {
        if (!param.Client.IsFakeClient && !param.Client.IsHltv && param.Client.SteamId.IsValidUserId()
            && _cachedInterface?.Instance is { } preferences
            && preferences.IsLoaded(param.Client.SteamId)
            && preferences.GetCookie(param.Client.SteamId, CookieKey) is { } cookie)
        {
            param.Controller.Print(HudPrintChannel.Chat, cookie.GetString());
        }
    }

    // .greeting "Welcome back!" or ms_greeting "Welcome back!" in the client console.
    private ECommandAction OnGreetingCommand(IGameClient client, StringCommand command)
    {
        if (!client.IsFakeClient && !client.IsHltv && client.SteamId.IsValidUserId() && command.ArgCount == 1
            && _cachedInterface?.Instance is { } preferences && preferences.IsLoaded(client.SteamId))
        {
            preferences.SetCookie(client.SteamId, CookieKey, command.GetArg(1));
        }

        return ECommandAction.Stopped;
    }

    private void OnCookieLoad(IGameClient client)
    {
        if (_cachedInterface?.Instance is not { } preferences || !preferences.IsLoaded(client.SteamId))
        {
            return;
        }

        if (preferences.GetCookie(client.SteamId, CookieKey) is null)
        {
            preferences.SetCookie(client.SteamId, CookieKey, "Welcome back!");
        }
    }

    public void Shutdown()
    {
        _hooks.PlayerSpawnPost.RemoveForward(OnPlayerSpawned);
        _clients.RemoveCommandCallback("greeting", OnGreetingCommand);
        ReleasePreferences();
    }

    public string DisplayName   => "ClientPrefs Example";
    public string DisplayAuthor => "ModSharp dev team";
}
