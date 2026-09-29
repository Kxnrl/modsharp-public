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

using Sharp.Shared.Units;

namespace Sharp.Shared.Managers;

public interface IAddonManager
{
#region Dual Addon

    /// <summary>
    ///     Clear dual addon cache
    /// </summary>
    void DualAddonPurgeCheck();

    /// <summary>
    ///     Override cache for a player
    /// </summary>
    void DualAddonOverrideCheck(SteamID steamId, double time);

#endregion

#region Extra Addon

    /// <summary>
    ///     Get the workshop IDs of the current server-side extra addons list (m_ExtraAddons).
    ///     Initially seeded from the <c>-extra_addons</c> command line parameter, but can be modified at runtime
    ///     via <see cref="ExtraAddonAddAddon"/> / <see cref="ExtraAddonRemoveAddon"/>.
    /// </summary>
    ulong[] ExtraAddonGetIds();

    /// <summary>Server-side extra addons (mounted on the server filesystem).</summary>
    string[] ExtraAddonGetServerAddons();

    /// <summary>Workshop IDs that every client is told to download (download-only, not mounted server-side).</summary>
    string[] ExtraAddonGetGlobalClientAddons();

    /// <summary>Addons currently mounted via the filesystem search path.</summary>
    string[] ExtraAddonGetMountedAddons();

    /// <summary>
    ///     Full ordered addon list a particular client should load: workshop map + server-mounted + global client + per-steamid.
    ///     Pass <c>0</c> to omit the per-steamid layer.
    /// </summary>
    string[] ExtraAddonGetClientAddons(SteamID steamId);

    /// <summary>The workshop map ID currently associated with the running map (empty when on a Valve map).</summary>
    string? ExtraAddonGetCurrentWorkshopMap();

    /// <summary>Add a workshop ID to the server-side extra addons list. Optionally remounts and reloads.</summary>
    bool ExtraAddonAddAddon(string addon, bool refresh = false);

    /// <summary>Remove a workshop ID from the server-side extra addons list. Optionally remounts and reloads.</summary>
    bool ExtraAddonRemoveAddon(string addon, bool refresh = false);

    /// <summary>Clear the server-side extra addons list and unmount everything.</summary>
    void ExtraAddonClearAddons();

    /// <summary>Re-mount all server-side extra addons. Optionally reloads the map afterwards.</summary>
    void ExtraAddonRefreshAddons(bool reloadMap = false);

    /// <summary>Reload the current map (using changelevel or host_workshop_map as appropriate).</summary>
    void ExtraAddonReloadMap();

    /// <summary>Mount an addon's VPK on the server filesystem (low-level; usually you want AddAddon).</summary>
    bool ExtraAddonMount(string addon, bool addToTail = false);

    /// <summary>Unmount an addon's VPK from the server filesystem.</summary>
    bool ExtraAddonUnmount(string addon);

    /// <summary>True when the addon is mounted server-side. Pass <c>checkWorkshopMap=true</c> to also count the active workshop map.</summary>
    bool ExtraAddonIsMounted(string addon, bool checkWorkshopMap = false);

    /// <summary>
    ///     Add a workshop ID to the addons delivered to clients.
    ///     Pass <c>steamId=0</c> to add to the global list (every client downloads it),
    ///     otherwise the addon is delivered only to that specific client.
    /// </summary>
    void ExtraAddonAddClientAddon(string addon, SteamID steamId = default, bool refresh = false);

    /// <summary>Remove an addon from the global or per-steamid client list.</summary>
    void ExtraAddonRemoveClientAddon(string addon, SteamID steamId = default);

    /// <summary>Clear the global or per-steamid client addons list.</summary>
    void ExtraAddonClearClientAddons(SteamID steamId = default);

    /// <summary>
    ///     Manually queue a Steam UGC download.
    ///     <paramref name="important"/> = true triggers a map reload after the download completes.
    /// </summary>
    bool ExtraAddonDownload(string addon, bool important = false, bool force = false);

    /// <summary>True when SteamUGC is available (Steam API connected).</summary>
    bool ExtraAddonHasUGCConnection();

#endregion
}
