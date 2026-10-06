/*
 * ModSharp
 * Copyright (C) 2023-2026 Kxnrl. All Rights Reserved.
 *
 * This file is part of ModSharp.
 * ModSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * ModSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ModSharp. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using Sharp.Shared.Listeners;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

/// <summary>
///     Low-level workshop addon delivery, the <c>Sharp.Modules.AddonManager</c> module is built on it. <br />
///     Use the module unless you are writing your own addon manager. <br />
///     The flow is picked on every map change: 1 addon uses the DualAddon flow,
///     2+ addons (or an installed <see cref="IAddonListener" />) use the MultiAddon flow.
/// </summary>
public interface IAddonManager
{
    /// <summary>
    ///     Server addons applied on the next map change (seeded from the deprecated <c>-dual_addon</c> launch parameter)
    /// </summary>
    IReadOnlyList<ulong> GetAddons();

    /// <summary>
    ///     Replace the server addons. Takes effect on the next map change, changing the map is up to the caller.
    /// </summary>
    /// <returns>always true (kept for compatibility)</returns>
    bool SetAddons(IReadOnlyList<ulong> addons);

    /// <summary>
    ///     Forget which addons a client already downloaded, <c>default</c> resets every client
    /// </summary>
    void ResetClientCache(SteamID steamId = default);

    /// <summary>
    ///     Resend the addons to an in-game client, the client will reconnect. <br />
    ///     <paramref name="resetCache" /> resends every addon (e.g. it failed to receive them),
    ///     otherwise only the addons it does not have yet (e.g. just added by an <see cref="IAddonListener" />). <br />
    ///     Can not force Steam to verify broken local files.
    /// </summary>
    /// <returns>false when the client is not in game, still downloading, or there is nothing to send</returns>
    bool RefreshClient(SteamID steamId, bool resetCache = true);

    /// <summary>
    ///     Force a workshop download (update) of an addon on the server. <br />
    ///     An addon mounted by the MultiAddon flow is unmounted during the download and mounted again afterwards
    ///     (Windows locks mounted files). Addons mounted by the engine (workshop map, DualAddon) are not.
    /// </summary>
    bool UpdateAddon(ulong addon);

    /// <summary>
    ///     Reload the current map. A workshop map uses <c>ds_workshop_changelevel</c> when the server already has it
    ///     (no update check), otherwise <c>host_workshop_map</c>.
    /// </summary>
    void ReloadMap();

    /// <summary>
    ///     Tune the MultiAddon flow
    /// </summary>
    void SetOptions(AddonOptions options);

    /// <summary>
    ///     Add <see cref="IAddonListener" /> to listen for events
    /// </summary>
    void InstallAddonListener(IAddonListener listener);

    /// <summary>
    ///     Remove <see cref="IAddonListener" />
    /// </summary>
    void RemoveAddonListener(IAddonListener listener);
}
