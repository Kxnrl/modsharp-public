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
using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

/// <summary>
///     Workshop addons delivered alongside the map. <br />
///     Requires the <c>-dual_addon</c> launch parameter (e.g. <c>-dual_addon 123</c> or <c>-dual_addon 123,456</c>). <br />
///     1 addon uses the DualAddon flow, 2+ addons (or an installed <see cref="IAddonListener" />) use the MultiAddon flow.
/// </summary>
public interface IAddonManager
{
    /// <summary>
    ///     Server addons applied on the next map change (seeded from <c>-dual_addon</c>)
    /// </summary>
    IReadOnlyList<ulong> GetAddons();

    /// <summary>
    ///     Replace the server addons. Takes effect on the next map change, changing the map is up to the caller.
    /// </summary>
    /// <returns>false when <c>-dual_addon</c> is not specified</returns>
    bool SetAddons(IReadOnlyList<ulong> addons);

    /// <summary>
    ///     Forget which addons a client already downloaded, <c>default</c> resets every client
    /// </summary>
    void ResetClientCache(SteamID steamId = default);

    /// <summary>
    ///     Resend the addons to an in-game client so it reloads / downloads them again, the client will reconnect. <br />
    ///     Useful when a client failed to receive them. Can not force Steam to verify broken local files.
    /// </summary>
    /// <returns>false when the client is not in game or there is nothing to send</returns>
    bool RefreshClient(SteamID steamId);

    /// <summary>
    ///     Add <see cref="IAddonListener" /> to listen for events
    /// </summary>
    void InstallAddonListener(IAddonListener listener);

    /// <summary>
    ///     Remove <see cref="IAddonListener" />
    /// </summary>
    void RemoveAddonListener(IAddonListener listener);
}
