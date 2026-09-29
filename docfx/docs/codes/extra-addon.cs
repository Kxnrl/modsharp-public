using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Types;

namespace ExtraAddonExample;

public sealed class ExtraAddon : IModSharpModule
{
    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger<ExtraAddon> _logger;

    public ExtraAddon(ISharedSystem sharedSystem,
        string                      dllPath,
        string                      sharpPath,
        Version                     version,
        IConfiguration              coreConfiguration,
        bool                        hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger       = sharedSystem.GetLoggerFactory().CreateLogger<ExtraAddon>();
    }

    public bool Init()
    {
        var addons = _sharedSystem.GetAddonManager();

        // Add an addon to the server-side mounted list (similar to specifying it via -extra_addons).
        // refresh: true triggers a remount + map reload if needed.
        addons.ExtraAddonAddAddon("123123123123", refresh: true);

        // Push an addon to all connecting clients (download-only, not mounted server-side).
        addons.ExtraAddonAddClientAddon("123123123456");

        // Push an addon to a specific client only.
        addons.ExtraAddonAddClientAddon("123123123789", new SteamID(76561198000000000UL));

        // Inspect what's currently mounted.
        foreach (var addon in addons.ExtraAddonGetMountedAddons())
        {
            _logger.LogInformation("Mounted addon: {Addon}", addon);
        }

        return true;
    }

    public void Shutdown()
    {
    }

    public string DisplayName => "Extra Addon Example";
    public string DisplayAuthor => "ModSharp dev team";
}
