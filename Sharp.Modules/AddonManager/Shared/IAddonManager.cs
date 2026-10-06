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
using Sharp.Shared.Units;

namespace Sharp.Modules.AddonManager.Shared;

/// <summary>
///     Workshop addon management (MultiAddonManager compatible), always delivered through the MultiAddon flow. <br />
///     Server addon changes take effect on the next map change, pass <c>reloadMap</c> or call <see cref="ReloadMap" /> to apply them.
/// </summary>
public interface IAddonManager
{
    const string Identity = nameof(IAddonManager);

    /// <summary>
    ///     Server addons (mounted on the server and delivered to every client)
    /// </summary>
    IReadOnlyList<ulong> GetAddons();

    bool AddAddon(ulong addon, bool reloadMap = false);

    bool RemoveAddon(ulong addon, bool reloadMap = false);

    void ClearAddons(bool reloadMap = false);

    /// <summary>
    ///     Reload the current map (workshop maps are reloaded through the workshop)
    /// </summary>
    void ReloadMap();

    /// <summary>
    ///     Client-only addons, <c>default</c> steamId means every client
    /// </summary>
    IReadOnlyList<ulong> GetClientAddons(SteamID steamId = default);

    /// <summary>
    ///     Add a client-only addon, <c>default</c> steamId means every client. <br />
    ///     <paramref name="refresh" /> sends it to clients already in game (they will reconnect).
    /// </summary>
    bool AddClientAddon(ulong addon, SteamID steamId = default, bool refresh = false);

    bool RemoveClientAddon(ulong addon, SteamID steamId = default);

    void ClearClientAddons(SteamID steamId = default);

    /// <summary>
    ///     Resend every addon to an in-game client that failed to receive them (it will reconnect)
    /// </summary>
    bool RefreshClient(SteamID steamId);

    /// <summary>
    ///     Start a workshop download on the server. <br />
    ///     <paramref name="reloadMap" /> reloads the map once every download started with it finishes.
    /// </summary>
    bool DownloadAddon(ulong addon, bool reloadMap = false, bool force = true);

    /// <summary>
    ///     Whether the server can download addons (Steam API available)
    /// </summary>
    bool HasUGCConnection();
}
