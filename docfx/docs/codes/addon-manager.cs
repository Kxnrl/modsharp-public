using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace AddonManagerExample;

public sealed class AddonManagerExample : IModSharpModule, IAddonListener
{
    private readonly ISharedSystem _sharedSystem;

    // e.g. loaded from your own config / database beforehand
    private readonly HashSet<SteamID> _vips = [new SteamID(76561198000000000UL)];

    public AddonManagerExample(ISharedSystem sharedSystem,
        string                               dllPath,
        string                               sharpPath,
        Version                              version,
        IConfiguration                       coreConfiguration,
        bool                                 hotReload)
        => _sharedSystem = sharedSystem;

    public bool Init()
    {
        var addons = _sharedSystem.GetAddonManager();

        // replace the server addons, applied on the next map change.
        addons.SetAddons([123123123123, 123123123456]);

        // ask to be queried for per-client addons.
        addons.InstallAddonListener(this);

        // let players re-fetch the addons when they failed to receive them: ms_addon_refresh
        _sharedSystem.GetClientManager().InstallCommandCallback("addon_refresh", OnCommandAddonRefresh);

        return true;
    }

    public void Shutdown()
    {
        // must uninstall the listener in Shutdown
        _sharedSystem.GetAddonManager().RemoveAddonListener(this);
        _sharedSystem.GetClientManager().RemoveCommandCallback("addon_refresh", OnCommandAddonRefresh);
    }

    private ECommandAction OnCommandAddonRefresh(IGameClient client, StringCommand command)
    {
        // the client reconnects and loads / downloads the addons again
        _sharedSystem.GetAddonManager().RefreshClient(client.SteamId);

        return ECommandAction.Handled;
    }

    public string DisplayName   => "Addon Manager Example";
    public string DisplayAuthor => "ModSharp Dev Team";

    public int ListenerVersion  => IAddonListener.ApiVersion;
    public int ListenerPriority => 0;

    public void OnClientQueryAddons(SteamID steamId, List<ulong> addons)
    {
        // called before the client exists, keep it fast and return the same result while it downloads.
        if (_vips.Contains(steamId))
        {
            addons.Add(123123123789);
        }
    }
}
