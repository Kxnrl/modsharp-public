using System;
using Microsoft.Extensions.Configuration;
using Sharp.Modules.AddonManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Units;

namespace AddonManagerExample;

public sealed class AddonManagerExample : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;

    private IModSharpModuleInterface<IAddonManager>? _addonManager;

    public AddonManagerExample(ISharedSystem sharedSystem,
        string                               dllPath,
        string                               sharpPath,
        Version                              version,
        IConfiguration                       coreConfiguration,
        bool                                 hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
        => true;

    public void OnAllModulesLoaded()
    {
        _addonManager = _sharedSystem.GetSharpModuleManager()
                                     .GetOptionalSharpModuleInterface<IAddonManager>(IAddonManager.Identity);

        if (_addonManager?.Instance is not { } addons)
        {
            return;
        }

        // add a server addon, applied on the next map change (pass reloadMap: true to apply it now).
        addons.AddAddon(123123123123);

        // deliver an extra addon to one client only, refresh sends it right away if the client is in game.
        addons.AddClientAddon(123123123789, new SteamID(76561198000000000UL), refresh: true);
    }

    public void Shutdown()
    {
    }

    public string DisplayName   => "Addon Manager Example";
    public string DisplayAuthor => "ModSharp Dev Team";
}
